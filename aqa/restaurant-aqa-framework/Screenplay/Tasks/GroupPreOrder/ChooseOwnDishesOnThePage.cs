using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Support;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class ChooseOwnDishesOnThePage(IReadOnlyList<DishQuantity> dishes, bool confirm) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var groupPage = new GroupPreOrderPage(actor.LookingAtThePage());

        foreach (var wanted in dishes)
        {
            var dish = actor.Recall<DishResponse>(wanted.Dish);
            for (var added = 1; added <= wanted.Quantity; added++)
            {
                await groupPage.AddDishButton(dish.Name).ClickAsync();
                await groupPage.OwnCartLine(dish.Id, added).WaitForAsync();
            }
        }

        if (confirm)
        {
            await groupPage.ConfirmOwnDishesButton.ClickAsync();
            await groupPage.OwnDishesConfirmed.WaitForAsync();
        }
    }
}
