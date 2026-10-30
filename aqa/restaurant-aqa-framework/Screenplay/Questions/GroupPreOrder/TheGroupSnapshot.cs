using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheGroupSnapshot(Func<GroupSessionsResponse, bool>? settledWhen = null, string bookingName = GroupPreOrderPersonas.MainBooking)
    : IQuestion<GroupSessionsResponse>
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(500);

    public async Task<GroupSessionsResponse> AnsweredByAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var owner = actor.Recall<CustomerAccount>(booking.OwnerPersona);
        var groups = new GroupPreOrdersApi(actor.UsingTheApi());
        var deadline = DateTime.UtcNow + Patience;

        while (true)
        {
            var response = await groups.GetSessionsAsync(booking.GroupReservationId, owner.IdToken);
            await response.EnsureSucceededAsync("Reading the group participants");
            var snapshot = await response.ReadAsAsync<GroupSessionsResponse>();

            if (settledWhen is null || settledWhen(snapshot) || DateTime.UtcNow >= deadline)
                return snapshot;

            await Task.Delay(Pause);
        }
    }
}
