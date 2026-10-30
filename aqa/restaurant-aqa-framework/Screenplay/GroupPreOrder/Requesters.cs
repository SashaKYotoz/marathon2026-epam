using Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

public static class Requesters
{
    public static async Task<string?> BearerAsync(Actor actor, Requester requester)
    {
        switch (requester)
        {
            case Requester.Owner:
                return actor.Recall<CustomerAccount>(GroupPreOrderPersonas.Owner).IdToken;
            case Requester.AnotherCustomer:
                await actor.AttemptsToAsync(new RegisterACustomer(GroupPreOrderPersonas.AnotherCustomer));
                return actor.Recall<CustomerAccount>(GroupPreOrderPersonas.AnotherCustomer).IdToken;
            case Requester.Waiter:
                return actor.Recall<LocationWaiter>().IdToken;
            case Requester.Guest:
                return actor.RecallAll<JoinedGuest>()[0].SessionToken;
            case Requester.Nobody:
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(requester), requester, null);
        }
    }
}
