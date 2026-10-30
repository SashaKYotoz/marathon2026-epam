using APIHandler.Models.GroupPreOrders;
using APIHandler.Models.GuestSessions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.GroupPreOrders;

public interface IGroupPreOrderService
{
    Task<GetAllSessionsResponse> GetGroupSessionsAsync(string userId, string groupReservationId);

    Task<OwnerGuestSessionIdResponse> GetOwnerGuestSessionIdAsync(string userId, string reservationId);

    Task<IReadOnlyList<ConsolidatedOrderItem>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId);

    Task<GetAllSessionsResponse> RemoveGuestSessionAsync(string userId, string groupReservationId, string guestSessionId);
}
