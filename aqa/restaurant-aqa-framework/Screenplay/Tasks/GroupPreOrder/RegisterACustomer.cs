using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class RegisterACustomer(string persona) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        if (actor.RecallAll<CustomerAccount>().Any(account => account.Persona == persona))
            return;

        var auth = new AuthApi(actor.UsingTheApi());
        var registration = SampleDataFactory.RegisterRequest();

        await (await auth.RegisterAsync(registration)).EnsureSucceededAsync($"Registering the {persona}");

        var signIn = await auth.SignInAsync(new SignInRequest(registration.Email, registration.Password));
        await signIn.EnsureSucceededAsync($"Signing the {persona} in");
        var tokens = await signIn.ReadAsAsync<SignInResponse>();

        actor.Remember(persona, new CustomerAccount(persona, registration.FirstName, registration.Email, registration.Password, tokens.IdToken));
    }
}
