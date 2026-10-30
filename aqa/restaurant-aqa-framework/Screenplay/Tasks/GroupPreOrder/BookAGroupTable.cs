using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class BookAGroupTable(int guests, string bookingName = GroupPreOrderPersonas.MainBooking, string ownerPersona = GroupPreOrderPersonas.Owner) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        await actor.AttemptsToAsync(new RegisterACustomer(ownerPersona), new AddTable(guests));

        var owner = actor.Recall<CustomerAccount>(ownerPersona);
        var table = actor.Recall<TableResponse>();
        var location = actor.Recall<LocationResponse>();
        var reservations = new ReservationsApi(actor.UsingTheApi());
        var groups = new GroupPreOrdersApi(actor.UsingTheApi());

        var startTime = DateTime.UtcNow.Date.AddDays(2).AddHours(18);
        var booking = await reservations.CreateReservationAsync(
            SampleDataFactory.CreateReservationRequest(table.Id, startTime, guests), owner.IdToken);
        await booking.EnsureSucceededAsync($"Booking a table for {guests} guests");
        var reservation = await booking.ReadAsAsync<ReservationResponse>();
        actor.Remember(reservation);
        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}",
            () => reservations.CancelReservationAsync(reservation.Id, owner.IdToken));

        var invite = await groups.GetInviteTokenAsync(reservation.Id, owner.IdToken);
        await invite.EnsureSucceededAsync("Getting the group invite link");
        var inviteToken = (await invite.ReadAsAsync<InviteTokenResponse>()).Token;

        var group = await groups.GetInviteAsync(inviteToken);
        await group.EnsureSucceededAsync("Opening the group invite");
        var groupReservationId = (await group.ReadAsAsync<GroupInviteResponse>()).GroupReservationId;

        actor.Remember(bookingName, new GroupBooking(bookingName, reservation.Id, groupReservationId, inviteToken, ownerPersona, location.Address));
    }
}
