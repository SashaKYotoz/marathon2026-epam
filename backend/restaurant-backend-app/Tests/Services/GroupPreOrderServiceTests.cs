using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Services.GroupPreOrders;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupPreOrderServiceTests
{
    private const string OwnerId = "owner-user";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";

    private readonly Mock<IGroupReservationRepository> _groupRepository = new();
    private readonly Mock<IGuestSessionRepository> _sessionRepository = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IDishRepository> _dishRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GroupPreOrderService _service;

    private readonly GroupReservation _group = new()
    {
        GroupReservationId = GroupId,
        ReservationId = ReservationId,
        ReservationToken = "token",
        Status = GroupReservationStatus.Open,
    };

    private readonly Reservation _reservation = new()
    {
        Id = ReservationId,
        TableId = "table-1",
        WaiterId = "waiter-1",
        CustomerId = OwnerId,
        GuestCount = 4,
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
    };

    private static readonly Dish Caesar = new() { Id = "dish1", Name = "Caesar Salad", Price = 180 };
    private static readonly Dish TomYum = new() { Id = "dish2", Name = "Tom Yum", Price = 220 };

    public GroupPreOrderServiceTests()
    {
        _service = new GroupPreOrderService(
            _groupRepository.Object,
            _sessionRepository.Object,
            _reservationRepository.Object,
            _orderRepository.Object,
            _dishRepository.Object,
            _userRepository.Object);

        _groupRepository.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(_group);
        _groupRepository.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(_group);
        _reservationRepository.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(_reservation);
        _dishRepository
            .Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> ids) => new[] { Caesar, TomYum }.Where(d => ids.Contains(d.Id)));
    }

    private static GuestSession Session(string id, string name, bool submitted, params (string DishId, int Quantity)[] dishes) => new()
    {
        Id = id,
        GuestName = name,
        GroupReservationId = GroupId,
        IsSubmitted = submitted,
        IsOwnerSession = false,
        CreatedAt = DateTime.UtcNow,
        Dishes = dishes.Select(d => new OrderItem { DishId = d.DishId, Quantity = d.Quantity, State = OrderItemState.Pending }).ToList(),
    };

    private void GivenSessions(params GuestSession[] sessions) =>
        _sessionRepository.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync(sessions.ToList());

    [Fact]
    public async Task GetGroupSessionsAsync_ReturnsParticipantsWithTotals()
    {
        var andrii = Session("session1", "Andrii", true, ("dish1", 2));
        var olena = Session("session2", "Olena", false, ("dish2", 1));
        olena.CreatedAt = andrii.CreatedAt!.Value.AddMinutes(1);
        GivenSessions(olena, andrii);

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        var participants = result.Participants.ToList();
        Assert.Equal(2, participants.Count);
        Assert.Equal("session1", participants[0].Id);
        Assert.Equal(ParticipantType.Guest, participants[0].Type);
        Assert.Equal(360, participants[0].Total);
        Assert.True(participants[0].IsSubmitted);
        Assert.Equal(220, participants[1].Total);
        Assert.False(participants[1].IsSubmitted);
        Assert.Equal(580, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_EmptyGroup_ReturnsNoParticipantsAndZeroTotal()
    {
        GivenSessions();

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        Assert.Empty(result.Participants);
        Assert.Equal(0, result.Total);
        _dishRepository.Verify(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_GuestWithoutDishes_IsListedWithZeroTotal()
    {
        GivenSessions(Session("session1", "Andrii", false));

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        var participant = Assert.Single(result.Participants);
        Assert.Empty(participant.Items);
        Assert.Equal(0, participant.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_OwnerWithDishes_IsListedFirstWithName()
    {
        var owner = Session("owner-session", "Owner", false, ("dish2", 1));
        owner.IsOwnerSession = true;
        owner.OwnerUserId = OwnerId;
        GivenSessions(Session("session1", "Andrii", true, ("dish1", 1)), owner);

        var user = new UserType { Attributes = [new AttributeType { Name = "given_name", Value = "Maria" }] };
        _userRepository.Setup(r => r.GetUserByIdAsync(OwnerId)).ReturnsAsync(user);

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        var first = result.Participants.First();
        Assert.Equal(ParticipantType.Owner, first.Type);
        Assert.Equal("Maria", first.Name);
        Assert.Equal(400, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_OwnerWithoutDishes_IsNotListed()
    {
        var owner = Session("owner-session", "Owner", false);
        owner.IsOwnerSession = true;
        owner.OwnerUserId = OwnerId;
        GivenSessions(owner);

        var result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        Assert.Empty(result.Participants);
    }

    [Theory]
    [InlineData("another-customer")]
    [InlineData("waiter-1")]
    public async Task GetGroupSessionsAsync_NotOwner_ThrowsForbidden(string userId)
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetGroupSessionsAsync(userId, GroupId));
    }

    [Fact]
    public async Task GetGroupSessionsAsync_UnknownGroup_ThrowsNotFound()
    {
        _groupRepository.Setup(r => r.GetGroupReservationByIdAsync("missing")).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetGroupSessionsAsync(OwnerId, "missing"));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NoOwnerSession_CreatesOne()
    {
        GivenSessions();
        GuestSession? saved = null;
        _sessionRepository.Setup(r => r.SaveAsync(It.IsAny<GuestSession>())).Callback<GuestSession>(s => saved = s);

        var result = await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        Assert.NotNull(saved);
        Assert.True(saved.IsOwnerSession);
        Assert.Equal(OwnerId, saved.OwnerUserId);
        Assert.False(string.IsNullOrEmpty(saved.SessionTokenHash));
        Assert.Equal(saved.Id, result.GuestSessionId);
        Assert.Equal(GroupId, result.GroupReservationId);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_ExistingOwnerSession_ReturnsIt()
    {
        var owner = Session("owner-session", "Owner", false);
        owner.IsOwnerSession = true;
        owner.OwnerUserId = OwnerId;
        GivenSessions(Session("session1", "Andrii", false), owner);

        var result = await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        Assert.Equal("owner-session", result.GuestSessionId);
        _sessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetOwnerGuestSessionIdAsync("stranger", ReservationId));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_UnknownReservation_ThrowsNotFound()
    {
        _reservationRepository.Setup(r => r.GetReservationByIdAsync("missing")).ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.GetOwnerGuestSessionIdAsync(OwnerId, "missing"));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NoGroup_ThrowsNotFound()
    {
        _groupRepository.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ConsolidatesOnlySubmittedSessions()
    {
        GivenSessions(
            Session("s1", "Andrii", true, ("dish1", 2), ("dish2", 1)),
            Session("s2", "Olena", true, ("dish1", 1), ("dish2", 1)),
            Session("s3", "Ivan", false, ("dish1", 5)));

        var order = new Order
        {
            OrderId = "order-1",
            CustomerId = OwnerId,
            ReservationId = ReservationId,
            ReservationAddress = "address",
            ReservationTime = "time",
            Dishes = [],
            Status = OrderStatus.Created,
        };
        _orderRepository.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);

        var result = await _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId);

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result.Single(i => i.DishId == "dish1").Quantity);
        Assert.Equal(2, result.Single(i => i.DishId == "dish2").Quantity);
        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        Assert.Equal(3, order.Dishes.Single(d => d.DishId == "dish1").Quantity);
        Assert.Equal(GroupReservationStatus.Submitted, _group.Status);
        _orderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);
        _groupRepository.Verify(r => r.SaveGroupReservationAsync(_group), Times.Once);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_AddsToExistingOrderItems()
    {
        GivenSessions(Session("s1", "Andrii", true, ("dish1", 2)));
        var order = new Order
        {
            OrderId = "order-1",
            CustomerId = OwnerId,
            ReservationId = ReservationId,
            ReservationAddress = "address",
            ReservationTime = "time",
            Dishes = [new OrderItem { DishId = "dish1", Quantity = 1, State = OrderItemState.Pending }],
        };
        _orderRepository.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);

        await _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId);

        Assert.Equal(3, order.Dishes.Single().Quantity);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoSubmittedSessions_Throws()
    {
        GivenSessions(Session("s1", "Andrii", false, ("dish1", 2)));

        await Assert.ThrowsAsync<NoSubmittedGuestSessionsException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_SubmittedSessionsWithoutDishes_Throws()
    {
        GivenSessions(Session("s1", "Andrii", true));

        await Assert.ThrowsAsync<SubmittedSessionsHaveNoDishesException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoOrder_Throws()
    {
        GivenSessions(Session("s1", "Andrii", true, ("dish1", 1)));
        _orderRepository.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<PreOrderNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
        _groupRepository.Verify(r => r.SaveGroupReservationAsync(It.IsAny<GroupReservation>()), Times.Never);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_AlreadySubmitted_ThrowsSubmitted()
    {
        _group.Status = GroupReservationStatus.Submitted;

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.SubmitGroupPreOrderAsync("stranger", GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupOfAnotherReservation_ThrowsValidation()
    {
        var otherReservation = new Reservation
        {
            Id = "reservation-2",
            TableId = "table-1",
            WaiterId = "waiter-1",
            CustomerId = OwnerId,
        };
        _reservationRepository.Setup(r => r.GetReservationByIdAsync("reservation-2")).ReturnsAsync(otherReservation);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, "reservation-2"));
        Assert.Equal(typeof(ValidationException), exception.GetType());
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_UnknownGroup_ThrowsNotFound()
    {
        _groupRepository.Setup(r => r.GetGroupReservationByIdAsync("missing")).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, "missing", ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_UnknownReservation_ThrowsNotFound()
    {
        _reservationRepository.Setup(r => r.GetReservationByIdAsync("missing")).ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, "missing"));
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_RemovesAndReturnsRecalculatedSnapshot()
    {
        var andrii = Session("session1", "Andrii", true, ("dish1", 2));
        var olena = Session("session2", "Olena", false, ("dish2", 1));
        GivenSessions(andrii, olena);
        _sessionRepository.Setup(r => r.GetByGuestSessionId("session1")).ReturnsAsync(andrii);

        var result = await _service.RemoveGuestSessionAsync(OwnerId, GroupId, "session1");

        _sessionRepository.Verify(r => r.DeleteAsync(andrii), Times.Once);
        var participant = Assert.Single(result.Participants);
        Assert.Equal("session2", participant.Id);
        Assert.Equal(220, result.Total);
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_LastParticipant_ReturnsEmptySnapshot()
    {
        var andrii = Session("session1", "Andrii", true, ("dish1", 2));
        GivenSessions(andrii);
        _sessionRepository.Setup(r => r.GetByGuestSessionId("session1")).ReturnsAsync(andrii);

        var result = await _service.RemoveGuestSessionAsync(OwnerId, GroupId, "session1");

        Assert.Empty(result.Participants);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_SessionOfAnotherGroup_ThrowsValidation()
    {
        var foreign = Session("foreign", "Taras", false);
        foreign.GroupReservationId = "group-2";
        _sessionRepository.Setup(r => r.GetByGuestSessionId("foreign")).ReturnsAsync(foreign);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RemoveGuestSessionAsync(OwnerId, GroupId, "foreign"));
        Assert.Equal(typeof(ValidationException), exception.GetType());
        _sessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_UnknownSession_ThrowsNotFound()
    {
        _sessionRepository.Setup(r => r.GetByGuestSessionId("missing")).ReturnsAsync((GuestSession?)null);

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() => _service.RemoveGuestSessionAsync(OwnerId, GroupId, "missing"));
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.RemoveGuestSessionAsync("stranger", GroupId, "session1"));
        _sessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_OwnerSession_ThrowsValidation()
    {
        var owner = Session("owner-session", "Owner", false);
        owner.IsOwnerSession = true;
        owner.OwnerUserId = OwnerId;
        _sessionRepository.Setup(r => r.GetByGuestSessionId("owner-session")).ReturnsAsync(owner);

        await Assert.ThrowsAsync<ValidationException>(() => _service.RemoveGuestSessionAsync(OwnerId, GroupId, "owner-session"));
    }

    [Fact]
    public async Task RemoveGuestSessionAsync_SubmittedGroup_ThrowsSubmitted()
    {
        _group.Status = GroupReservationStatus.Submitted;

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.RemoveGuestSessionAsync(OwnerId, GroupId, "session1"));
    }
}
