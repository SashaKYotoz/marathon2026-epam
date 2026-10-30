using System.ComponentModel.DataAnnotations;

namespace APIHandler.Models.GroupPreOrders;

public record OwnerGuestSessionIdResponse(string GuestSessionId, string GroupReservationId);

public record SubmitGroupPreOrderRequest([property: Required] string ReservationId);

public record ConsolidatedOrderItem(string DishId, int Quantity);
