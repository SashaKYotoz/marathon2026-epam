using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class ChooseOwnDishesThroughTheService(IReadOnlyList<DishQuantity> dishes, bool confirm, string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var owner = actor.Recall<CustomerAccount>(booking.OwnerPersona);
        var groups = new GroupPreOrdersApi(actor.UsingTheApi());

        var ownSession = await groups.GetOwnerSessionAsync(booking.ReservationId, owner.IdToken);
        await ownSession.EnsureSucceededAsync("Opening the owner's own cart");
        var sessionId = (await ownSession.ReadAsAsync<OwnerSessionResponse>()).GuestSessionId;

        foreach (var dish in dishes)
        {
            var request = new GuestDishRequest(actor.Recall<DishResponse>(dish.Dish).Id, dish.Quantity);
            await (await groups.AddDishAsync(sessionId, request, owner.IdToken)).EnsureSucceededAsync($"The owner adding {dish}");
        }

        if (confirm)
            await (await groups.SubmitCartAsync(sessionId, owner.IdToken)).EnsureSucceededAsync("The owner confirming their own dishes");
    }
}
