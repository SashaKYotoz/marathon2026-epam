using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class SignInToTheStorefront(Requester who) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        (string email, string password) = who switch
        {
            Requester.Owner => Credentials(actor.Recall<CustomerAccount>(GroupPreOrderPersonas.Owner)),
            Requester.Waiter => Credentials(actor.Recall<LocationWaiter>()),
            _ => throw new InvalidOperationException($"Only the owner and the waiter sign in to the storefront, not {who}."),
        };

        var page = actor.LookingAtThePage();
        await page.GotoAsync("/main");
        await page.EvaluateAsync("() => { localStorage.clear(); sessionStorage.clear(); }");

        await actor.AttemptsToAsync(new SignInThroughTheLoginForm(email, password));
        await page.WaitForFunctionAsync("() => localStorage.getItem('auth') !== null && localStorage.getItem('user') !== null");
    }

    private static (string Email, string Password) Credentials(CustomerAccount account) => (account.Email, account.Password);

    private static (string Email, string Password) Credentials(LocationWaiter waiter) => (waiter.Email, waiter.Password);
}
