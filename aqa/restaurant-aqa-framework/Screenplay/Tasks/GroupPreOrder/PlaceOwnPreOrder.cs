using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class PlaceOwnPreOrder(IReadOnlyList<DishQuantity> dishes, string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var owner = actor.Recall<CustomerAccount>(booking.OwnerPersona);
        var orders = new OrdersApi(actor.UsingTheApi());
        var items = dishes
            .Select(dish => new OrderItemRequest(actor.Recall<DishResponse>(dish.Dish).Id, dish.Quantity))
            .ToList();

        var response = await orders.CreateOrderAsync(new CreateOrderRequest(booking.ReservationId, items), owner.IdToken);
        await response.EnsureSucceededAsync("Starting the owner's pre-order");
        actor.Recall<CleanupScope>().TrackDelete(
            $"order:{booking.ReservationId}",
            () => orders.DeleteOrderAsync(booking.ReservationId, owner.IdToken));
    }
}
