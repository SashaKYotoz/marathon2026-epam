using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class RemoveAParticipantThroughTheService(Requester requester, string? guestPersona, string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var sessionId = guestPersona is null ? GroupPreOrderPersonas.UnknownId : actor.Recall<JoinedGuest>(guestPersona).SessionId;
        var bearer = await Requesters.BearerAsync(actor, requester);

        await actor.TheLastResponse().CaptureAsync(
            await new GroupPreOrdersApi(actor.UsingTheApi()).RemoveSessionAsync(booking.GroupReservationId, sessionId, bearer));
    }
}
