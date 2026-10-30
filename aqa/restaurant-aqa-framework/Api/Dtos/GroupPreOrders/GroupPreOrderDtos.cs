namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

public sealed record InviteTokenResponse(string Token);

public sealed record GroupInviteResponse(string GroupReservationId, string LocationId, int MaxGuestCount, string InviterName);

public sealed record JoinGroupRequest(string GroupReservationId, string GuestName);

public sealed record JoinGroupResponse(string SessionId, string SessionToken);

public sealed record GuestDishRequest(string DishId, int Quantity);

public sealed record GroupDishResponse(string Id, string Name, decimal Price);

public sealed record GroupParticipantItemResponse(GroupDishResponse Dish, int Quantity);

public sealed record GroupParticipantResponse(
    string Id,
    string Type,
    string Name,
    IReadOnlyList<GroupParticipantItemResponse> Items,
    decimal Total,
    bool IsSubmitted);

public sealed record GroupSessionsResponse(IReadOnlyList<GroupParticipantResponse> Participants, decimal Total);

public sealed record OwnerSessionResponse(string GuestSessionId, string GroupReservationId);

public sealed record SubmitGroupPreOrderRequest(string ReservationId);

public sealed record ConsolidatedItemResponse(string DishId, int Quantity);

public sealed record GroupStatusResponse(int CurrentParticipants, int MaxCapacity, string Status);
