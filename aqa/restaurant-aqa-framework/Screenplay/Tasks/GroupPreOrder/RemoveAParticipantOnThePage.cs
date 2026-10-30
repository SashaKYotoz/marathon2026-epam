using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class RemoveAParticipantOnThePage(string guestPersona) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var name = GroupPreOrderPersonas.DisplayNameOf(actor, guestPersona);
        var groupPage = new GroupPreOrderPage(actor.LookingAtThePage());

        await groupPage.RemoveButton(name).ClickAsync();
        await groupPage.ConfirmRemovalButton.ClickAsync();
        await groupPage.ParticipantCard(name).WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }
}
