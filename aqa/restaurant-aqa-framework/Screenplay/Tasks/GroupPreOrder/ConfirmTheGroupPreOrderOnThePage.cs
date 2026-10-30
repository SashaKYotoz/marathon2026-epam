using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class ConfirmTheGroupPreOrderOnThePage : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var groupPage = new GroupPreOrderPage(actor.LookingAtThePage());

        await groupPage.ConfirmGroupButton.ClickAsync();
        await groupPage.SentSummary.Or(groupPage.Refusal).First.WaitForAsync();
    }
}
