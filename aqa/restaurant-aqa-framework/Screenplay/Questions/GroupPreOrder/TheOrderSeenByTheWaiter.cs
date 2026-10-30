using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheOrderSeenByTheWaiter(string bookingName = GroupPreOrderPersonas.MainBooking) : IQuestion<GetOrderResponse>
{
    public async Task<GetOrderResponse> AnsweredByAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var waiter = actor.Recall<LocationWaiter>();

        var response = await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(booking.ReservationId, waiter.IdToken);
        await response.EnsureSucceededAsync("The waiter reading the reservation's order");
        return await response.ReadAsAsync<GetOrderResponse>();
    }
}
