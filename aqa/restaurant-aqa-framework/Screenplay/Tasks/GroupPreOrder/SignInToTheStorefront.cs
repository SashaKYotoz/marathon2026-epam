using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class SignInToTheStorefront(Requester who) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var (email, password) = who switch
        {
            Requester.Owner => Credentials(actor.Recall<CustomerAccount>(GroupPreOrderPersonas.Owner)),
            Requester.Waiter => Credentials(actor.Recall<LocationWaiter>()),
            _ => throw new ArgumentOutOfRangeException(nameof(who), who, "Only the owner and the waiter sign in to the storefront."),
        };

        var page = actor.LookingAtThePage();
        await page.GotoAsync("/main");
        await page.EvaluateAsync("() => { localStorage.clear(); sessionStorage.clear(); }");

        await actor.AttemptsToAsync(new SignInThroughTheLoginForm(email, password));
    }

    private static (string Email, string Password) Credentials(CustomerAccount account) => (account.Email, account.Password);

    private static (string Email, string Password) Credentials(LocationWaiter waiter) => (waiter.Email, waiter.Password);
}
