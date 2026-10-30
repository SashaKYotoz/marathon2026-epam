using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheGroupPreOrderOnThePage(Func<ShownGroupPreOrder, bool>? settledWhen = null) : IQuestion<ShownGroupPreOrder>
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan Pause = TimeSpan.FromSeconds(2);

    public async Task<ShownGroupPreOrder> AnsweredByAsync(Actor actor)
    {
        var page = actor.LookingAtThePage();
        var groupPage = new GroupPreOrderPage(page);
        var deadline = DateTime.UtcNow + Patience;

        await groupPage.WaitUntilLoadedAsync();
        while (true)
        {
            var shown = await groupPage.ReadAsync();
            if (settledWhen is null || settledWhen(shown) || DateTime.UtcNow >= deadline)
                return shown;

            await Task.Delay(Pause);
            await page.ReloadAsync();
            await groupPage.WaitUntilLoadedAsync();
        }
    }
}
