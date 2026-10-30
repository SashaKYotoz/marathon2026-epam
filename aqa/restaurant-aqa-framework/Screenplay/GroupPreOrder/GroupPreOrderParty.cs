namespace Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

public sealed record CustomerAccount(string Persona, string FirstName, string Email, string Password, string IdToken);

public sealed record LocationWaiter(string FirstName, string Email, string Password, string IdToken, string LocationId);

public sealed record GroupBooking(
    string Name,
    string ReservationId,
    string GroupReservationId,
    string InviteToken,
    string OwnerPersona,
    string LocationAddress);

public sealed record JoinedGuest(string Persona, string DisplayName, string SessionId, string SessionToken, string BookingName);

public enum Requester
{
    Owner,
    AnotherCustomer,
    Waiter,
    Guest,
    Nobody,
}

public static class GroupPreOrderPersonas
{
    public const string Owner = "owner";
    public const string AnotherCustomer = "another customer";
    public const string MainBooking = "main";
    public const string OtherBooking = "other";
    public const string UnknownId = "AQA-Sample-does-not-exist";

    public static string DisplayNameOf(Actor actor, string persona) =>
        string.Equals(persona, Owner, StringComparison.OrdinalIgnoreCase) || string.Equals(persona, "the owner", StringComparison.OrdinalIgnoreCase)
            ? actor.Recall<CustomerAccount>(Owner).FirstName
            : actor.Recall<JoinedGuest>(persona).DisplayName;
}
