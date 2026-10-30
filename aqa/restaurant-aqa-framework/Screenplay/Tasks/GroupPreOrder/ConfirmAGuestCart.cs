using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class ConfirmAGuestCart(string guestPersona, bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<JoinedGuest>(guestPersona);
        var response = await new GroupPreOrdersApi(actor.UsingTheApi()).SubmitCartAsync(guest.SessionId, guest.SessionToken);

        await actor.TheLastResponse().CaptureAsync(response);
        if (mustSucceed)
            await response.EnsureSucceededAsync($"{guestPersona} confirming their cart");
    }
}
