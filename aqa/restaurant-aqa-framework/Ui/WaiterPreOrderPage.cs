using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Ui;

public sealed partial class WaiterPreOrderPage(IPage page) : BasePage(page)
{
    public ILocator Dishes => Page.Locator("app-dish-card.pre-order-dish");

    public async Task<IReadOnlyList<DishQuantity>> ReadDishesAsync()
    {
        await Dishes.First.WaitForAsync();

        var shown = new List<DishQuantity>();
        foreach (var dish in await Dishes.AllAsync())
        {
            var name = (await dish.Locator(".dish-name").InnerTextAsync()).Trim();
            var quantityText = await dish.Locator(".pre-order-quantity").InnerTextAsync();
            var match = QuantityPattern().Match(quantityText);
            shown.Add(new DishQuantity(match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : 0, name));
        }

        return shown;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex QuantityPattern();
}
