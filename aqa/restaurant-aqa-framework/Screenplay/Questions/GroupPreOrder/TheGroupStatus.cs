using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheGroupStatus(string bookingName = GroupPreOrderPersonas.MainBooking) : IQuestion<GroupStatusResponse>
{
    public async Task<GroupStatusResponse> AnsweredByAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);

        var response = await new GroupPreOrdersApi(actor.UsingTheApi()).GetGroupStatusAsync(booking.GroupReservationId);
        await response.EnsureSucceededAsync("Reading the group status");
        return await response.ReadAsAsync<GroupStatusResponse>();
    }
}
