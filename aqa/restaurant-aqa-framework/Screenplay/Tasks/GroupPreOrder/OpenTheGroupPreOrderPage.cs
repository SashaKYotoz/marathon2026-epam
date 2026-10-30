using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;

public sealed class OpenTheGroupPreOrderPage(bool fromMyReservations, string bookingName = GroupPreOrderPersonas.MainBooking) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(bookingName);
        var page = actor.LookingAtThePage();
        var groupPage = new GroupPreOrderPage(page);

        if (fromMyReservations)
        {
            var reservations = new ReservationDashboardPage(page);
            await reservations.NavigateAsync();
            await reservations.ReservationByAddress(booking.LocationAddress).GroupPreOrderButton.ClickAsync();
            await page.WaitForURLAsync($"**/group-pre-order/{booking.ReservationId}");
        }
        else
        {
            await groupPage.NavigateAsync(booking.ReservationId);
        }

        await groupPage.WaitUntilLoadedAsync();
    }
}
