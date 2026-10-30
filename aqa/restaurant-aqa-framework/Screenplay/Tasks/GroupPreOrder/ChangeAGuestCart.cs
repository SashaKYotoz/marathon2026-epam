using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class ChangeAGuestCart(string guestPersona, IReadOnlyList<DishQuantity> dishes, bool removing = false) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<JoinedGuest>(guestPersona);
        var groups = new GroupPreOrdersApi(actor.UsingTheApi());

        foreach (var dish in dishes)
        {
            var request = new GuestDishRequest(actor.Recall<DishResponse>(dish.Dish).Id, dish.Quantity);
            var response = removing
                ? await groups.RemoveDishAsync(guest.SessionId, request, guest.SessionToken)
                : await groups.AddDishAsync(guest.SessionId, request, guest.SessionToken);

            await response.EnsureSucceededAsync($"{guestPersona} {(removing ? "removing" : "adding")} {dish}");
        }
    }
}
