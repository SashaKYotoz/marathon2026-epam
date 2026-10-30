using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class OpenTheWaiterPreOrder(string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var page = actor.LookingAtThePage();

        await actor.AttemptsToAsync(new SignInToTheStorefront(Requester.Waiter));

        var dashboard = new WaiterDashboardPage(page);
        await dashboard.NavigateAsync();
        await dashboard.ReservationAt(booking.LocationAddress).PreOrderActionButton.ClickAsync();
        await page.WaitForURLAsync("**/waiter-pre-order");
    }
}
