using Restaurant_Aqa_Framework.Support;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheWaiterPreOrderOnThePage : IQuestion<IReadOnlyList<DishQuantity>>
{
    public Task<IReadOnlyList<DishQuantity>> AnsweredByAsync(Actor actor) =>
        new WaiterPreOrderPage(actor.LookingAtThePage()).ReadDishesAsync();
}
