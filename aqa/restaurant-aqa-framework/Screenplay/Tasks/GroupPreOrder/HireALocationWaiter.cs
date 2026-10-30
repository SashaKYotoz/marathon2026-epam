using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Waiters;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class HireALocationWaiter : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var location = actor.Recall<LocationResponse>();
        var auth = new AuthApi(actor.UsingTheApi());
        var waiters = new WaitersApi(actor.UsingTheApi());
        var registration = SampleDataFactory.RegisterRequest();

        await (await auth.RegisterAsync(registration)).EnsureSucceededAsync("Registering the location waiter");

        var hiring = new WaiterListRequest(registration.Email, location.Id);
        await (await waiters.AddWaiterAsync(hiring)).EnsureSucceededAsync("Hiring the waiter for the sample location");
        actor.Recall<CleanupScope>().TrackDelete($"waiter:{registration.Email}", () => waiters.RemoveWaiterAsync(hiring));

        var signIn = await auth.SignInAsync(new SignInRequest(registration.Email, registration.Password));
        await signIn.EnsureSucceededAsync("Signing the location waiter in");
        var tokens = await signIn.ReadAsAsync<SignInResponse>();

        actor.Remember(new LocationWaiter(registration.FirstName, registration.Email, registration.Password, tokens.IdToken, location.Id));
    }
}
