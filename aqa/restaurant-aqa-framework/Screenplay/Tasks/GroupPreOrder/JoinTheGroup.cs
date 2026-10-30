using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class JoinTheGroup(string guestPersona, string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var groups = new GroupPreOrdersApi(actor.UsingTheApi());
        var displayName = SampleDataFactory.Prefix + guestPersona;

        var response = await groups.JoinAsync(new JoinGroupRequest(booking.GroupReservationId, displayName));
        await response.EnsureSucceededAsync($"{guestPersona} joining the group");
        var session = await response.ReadAsAsync<JoinGroupResponse>();

        actor.Remember(guestPersona, new JoinedGuest(guestPersona, displayName, session.SessionId, session.SessionToken, bookingName));

        var owner = actor.Recall<CustomerAccount>(booking.OwnerPersona);
        actor.Recall<CleanupScope>().TrackDelete(
            $"guest-session:{session.SessionId}",
            () => groups.RemoveSessionAsync(booking.GroupReservationId, session.SessionId, owner.IdToken));
    }
}
