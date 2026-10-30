using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Mappers;
using APIHandler.Models.GroupPreOrders;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.Orders;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace APIHandler.Services.GroupPreOrders;

public class GroupPreOrderService(
    IGroupReservationRepository groupReservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IReservationRepository reservationRepository,
    IOrderRepository orderRepository,
    IDishRepository dishRepository,
    IUserRepository userRepository) : IGroupPreOrderService
{
    public const string DefaultOwnerName = "Owner";

    public async Task<GetAllSessionsResponse> GetGroupSessionsAsync(string userId, string groupReservationId)
    {
        var (group, reservation) = await LoadOwnedGroupAsync(userId, groupReservationId);
        return await BuildSnapshotAsync(group, reservation, null);
    }

    public async Task<OwnerGuestSessionIdResponse> GetOwnerGuestSessionIdAsync(string userId, string reservationId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureOwner(reservation, userId);

        var group = await groupReservationRepository.GetByReservationIdAsync(reservationId)
            ?? throw new GroupReservationNotFoundException();

        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(group.GroupReservationId) ?? new List<GuestSession>();
        var ownerSession = sessions.FirstOrDefault(s => s.IsOwnerSession && s.OwnerUserId == userId);

        if (ownerSession is null)
        {
            EnsureGroupIsOpen(group);
            ownerSession = CreateOwnerSession(userId, group.GroupReservationId);
            await guestSessionRepository.SaveAsync(ownerSession);
        }

        return new OwnerGuestSessionIdResponse(ownerSession.Id, group.GroupReservationId);
    }

    public async Task<IReadOnlyList<ConsolidatedOrderItem>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException(reservationId);

        if (!string.Equals(group.ReservationId, reservation.Id, StringComparison.Ordinal))
            throw new ValidationException("Group reservation does not belong to the specified reservation.");

        EnsureOwner(reservation, userId);
        EnsureGroupIsOpen(group);

        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId) ?? new List<GuestSession>();
        var submittedSessions = sessions.Where(s => s.IsSubmitted).ToList();

        if (submittedSessions.Count == 0)
            throw new NoSubmittedGuestSessionsException();

        var consolidated = Consolidate(submittedSessions);

        if (consolidated.Count == 0)
            throw new SubmittedSessionsHaveNoDishesException();

        var order = await orderRepository.GetOrderByReservationIdAsync(reservationId)
            ?? throw new PreOrderNotFoundException();

        MergeIntoOrder(order, consolidated);
        order.Status = OrderStatus.SubmittedByCustomer;
        await orderRepository.SaveOrderAsync(order);

        group.Status = GroupReservationStatus.Submitted;
        await groupReservationRepository.SaveGroupReservationAsync(group);

        return consolidated;
    }

    public async Task<GetAllSessionsResponse> RemoveGuestSessionAsync(string userId, string groupReservationId, string guestSessionId)
    {
        var (group, reservation) = await LoadOwnedGroupAsync(userId, groupReservationId);

        EnsureGroupIsOpen(group);

        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session with id {guestSessionId} was not found.");

        if (!string.Equals(session.GroupReservationId, groupReservationId, StringComparison.Ordinal))
            throw new ValidationException("Guest session does not belong to this group reservation.");

        if (session.IsOwnerSession)
            throw new ValidationException("The owner's own session cannot be removed from the group.");

        await guestSessionRepository.DeleteAsync(session);

        return await BuildSnapshotAsync(group, reservation, guestSessionId);
    }

    public static List<ConsolidatedOrderItem> Consolidate(IEnumerable<GuestSession> sessions) =>
        sessions
            .SelectMany(s => s.Dishes ?? new List<OrderItem>())
            .Where(d => !string.IsNullOrWhiteSpace(d.DishId) && d.Quantity > 0)
            .GroupBy(d => d.DishId)
            .Select(g => new ConsolidatedOrderItem(g.Key, g.Sum(i => i.Quantity)))
            .ToList();

    private static void MergeIntoOrder(Order order, IEnumerable<ConsolidatedOrderItem> items)
    {
        order.Dishes ??= new List<OrderItem>();

        foreach (var item in items)
        {
            var existing = order.Dishes.FirstOrDefault(d => d.DishId == item.DishId);
            if (existing is not null)
            {
                existing.Quantity += item.Quantity;
                existing.State = OrderItemState.Pending;
                continue;
            }

            order.Dishes.Add(new OrderItem
            {
                DishId = item.DishId,
                Quantity = item.Quantity,
                State = OrderItemState.Pending,
            });
        }
    }

    private async Task<(GroupReservation Group, Reservation Reservation)> LoadOwnedGroupAsync(string userId, string groupReservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new ReservationNotFoundException(group.ReservationId);

        EnsureOwner(reservation, userId);

        return (group, reservation);
    }

    private async Task<GetAllSessionsResponse> BuildSnapshotAsync(GroupReservation group, Reservation reservation, string? excludedSessionId)
    {
        var sessions = (await guestSessionRepository.GetAllByGroupReservationIdAsync(group.GroupReservationId) ?? new List<GuestSession>())
            .Where(s => !string.Equals(s.Id, excludedSessionId, StringComparison.Ordinal))
            .ToList();

        var dishIds = sessions
            .SelectMany(s => s.Dishes ?? new List<OrderItem>())
            .Select(d => d.DishId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();

        IEnumerable<Dish> dishes = dishIds.Length == 0
            ? Array.Empty<Dish>()
            : await dishRepository.GetDishesByIdsAsync(dishIds) ?? Array.Empty<Dish>();

        var dishesById = dishes
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var participants = new List<Participant>();

        var ownerSession = sessions.FirstOrDefault(s => s.IsOwnerSession && (s.Dishes?.Count ?? 0) > 0);
        if (ownerSession is not null)
        {
            var ownerName = await ResolveOwnerNameAsync(reservation.CustomerId);
            participants.Add(ToParticipant(ownerSession, ParticipantType.Owner, ownerName, dishesById));
        }

        var guestSessions = sessions
            .Where(s => !s.IsOwnerSession)
            .OrderBy(s => s.CreatedAt ?? DateTime.MaxValue)
            .ThenBy(s => s.GuestName, StringComparer.OrdinalIgnoreCase);

        foreach (var guestSession in guestSessions)
            participants.Add(ToParticipant(guestSession, ParticipantType.Guest, guestSession.GuestName, dishesById));

        return new GetAllSessionsResponse(participants, participants.Sum(p => p.Total));
    }

    private static Participant ToParticipant(
        GuestSession session,
        ParticipantType type,
        string name,
        IReadOnlyDictionary<string, Dish> dishesById)
    {
        var items = (session.Dishes ?? new List<OrderItem>())
            .Where(d => d.Quantity > 0 && dishesById.ContainsKey(d.DishId))
            .Select(d => new OrderItemResponse
            {
                Dish = DishMapper.ToDishResponse(dishesById[d.DishId]),
                Quantity = d.Quantity,
            })
            .ToArray();

        decimal total = items.Sum(i => i.Quantity * i.Dish.Price);

        return new Participant(session.Id, type, name, items, total, session.IsSubmitted);
    }

    private async Task<string> ResolveOwnerNameAsync(string? customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return DefaultOwnerName;

        try
        {
            var user = await userRepository.GetUserByIdAsync(customerId);
            var givenName = user?.Attributes?.FirstOrDefault(a => a.Name == "given_name")?.Value;
            return string.IsNullOrWhiteSpace(givenName) ? DefaultOwnerName : givenName;
        }
        catch (Exception)
        {
            return DefaultOwnerName;
        }
    }

    private static GuestSession CreateOwnerSession(string userId, string groupReservationId) => new()
    {
        Id = Guid.NewGuid().ToString(),
        GroupReservationId = groupReservationId,
        GuestName = DefaultOwnerName,
        Dishes = new List<OrderItem>(),
        SessionTokenHash = Convert.ToBase64String(SHA256.HashData(RandomNumberGenerator.GetBytes(32))),
        IsOwnerSession = true,
        OwnerUserId = userId,
        CreatedAt = DateTime.UtcNow,
    };

    private static void EnsureOwner(Reservation reservation, string userId)
    {
        if (string.IsNullOrWhiteSpace(reservation.CustomerId) ||
            !string.Equals(reservation.CustomerId, userId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");
        }
    }

    private static void EnsureGroupIsOpen(GroupReservation group)
    {
        if (group.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();

        if (group.Status == GroupReservationStatus.Closed)
            throw new GroupReservationExpiredException();
    }
}
