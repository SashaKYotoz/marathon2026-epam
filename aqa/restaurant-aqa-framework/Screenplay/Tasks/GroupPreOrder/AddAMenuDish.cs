using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class AddAMenuDish(string alias, decimal price) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var location = actor.Recall<LocationResponse>();
        var api = new DishesApi(actor.UsingTheApi());
        var request = SampleDataFactory.CreateDishRequest([location.Id]) with
        {
            Name = SampleDataFactory.Prefix + alias,
            Price = price,
        };

        var response = await api.CreateDishAsync(request);
        await response.EnsureSucceededAsync($"Adding '{alias}' to the menu");
        var dish = await response.ReadAsAsync<DishResponse>();
        actor.Remember(alias, dish);

        var waiter = await actor.AsksForAsync(new TheWaiterSession());
        actor.Recall<CleanupScope>().TrackDelete(
            $"dish:{dish.Id}",
            () => api.DeleteDishAsync(dish.Id, force: true, bearerToken: waiter.IdToken));
    }
}
