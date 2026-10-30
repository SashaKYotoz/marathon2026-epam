using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheConfirmationOnThePage : IQuestion<ShownConfirmation>
{
    public Task<ShownConfirmation> AnsweredByAsync(Actor actor) =>
        new GroupPreOrderPage(actor.LookingAtThePage()).ReadConfirmationAsync();
}
