using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui.Components;

namespace Restaurant_Aqa_Framework.Ui;

public sealed class WaiterDashboardPage(IPage page) : BasePage(page)
{
    private ILocator ReservationCards => Page.Locator("ul.reservation-list > li.card");

    public Task NavigateAsync() => NavigateToAsync("/waiter-reservations");

    public ReservationCardComponent ReservationAt(string address) =>
        new(ReservationCards.Filter(new() { HasText = address }));
}
