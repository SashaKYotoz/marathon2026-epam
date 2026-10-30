using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class LookAtTheGroupParticipants(Requester requester, GroupTarget target = GroupTarget.TheGroup, string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var groupId = target == GroupTarget.AGroupThatDoesNotExist ? GroupPreOrderPersonas.UnknownId : booking.GroupReservationId;
        var bearer = await Requesters.BearerAsync(actor, requester);

        await actor.TheLastResponse().CaptureAsync(
            await new GroupPreOrdersApi(actor.UsingTheApi()).GetSessionsAsync(groupId, bearer));
    }
}
