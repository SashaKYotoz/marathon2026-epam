using System.Globalization;
using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Ui;

public sealed record ShownParticipant(string Name, bool Confirmed, string Status, IReadOnlyList<DishQuantity> Dishes, decimal Total);

public sealed record ShownGroupPreOrder(IReadOnlyList<ShownParticipant> Participants, decimal Total, bool NobodyJoinedShown);

public sealed record ShownConfirmation(bool Sent, IReadOnlyList<DishQuantity> Dishes, string? Refusal);

public sealed class GroupPreOrderPage(IPage page) : BasePage(page)
{
    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Name = "Group pre-order", Exact = true });
    public ILocator ParticipantCards => Page.GetByTestId("participant-card");
    public ILocator NobodyJoined => Page.GetByTestId("participants-empty");
    public ILocator GroupTotal => Page.GetByTestId("group-total");
    public ILocator ConfirmGroupButton => Page.GetByRole(AriaRole.Button, new() { Name = "Confirm group pre-order", Exact = true });
    public ILocator ConfirmOwnDishesButton => Page.GetByRole(AriaRole.Button, new() { Name = "Confirm my dishes", Exact = true });
    public ILocator OwnDishesConfirmed => Page.GetByTestId("owner-cart-confirmed");
    public ILocator ConfirmRemovalButton => Page.GetByRole(AriaRole.Button, new() { Name = "Confirm removal", Exact = true });
    public ILocator Refusal => Page.GetByTestId("submit-error");
    public ILocator SentSummary => Page.GetByTestId("submitted-summary");
    public ILocator ConfirmedDishes => Page.GetByTestId("consolidated-item");

    private ILocator ContentShown => Page.Locator("[data-testid='participant-card'], [data-testid='participants-empty'], [data-testid='load-error']");

    public Task NavigateAsync(string reservationId) => NavigateToAsync($"/group-pre-order/{reservationId}");

    public ILocator ParticipantCard(string name) =>
        Page.Locator($"[data-testid='participant-card'][data-participant-name='{name}']");

    public ILocator RemoveButton(string name) =>
        Page.GetByRole(AriaRole.Button, new() { Name = $"Remove {name}", Exact = true });

    public ILocator AddDishButton(string dishName) =>
        Page.GetByRole(AriaRole.Button, new() { Name = $"Add {dishName}", Exact = true });

    public ILocator OwnCartLine(string dishId, int quantity) =>
        Page.Locator($"[data-testid='owner-cart-item'][data-dish-id='{dishId}'][data-quantity='{quantity.ToString(CultureInfo.InvariantCulture)}']");

    public async Task WaitUntilLoadedAsync()
    {
        await Heading.WaitForAsync();
        await ContentShown.First.WaitForAsync();
    }

    public async Task<ShownGroupPreOrder> ReadAsync()
    {
        var participants = new List<ShownParticipant>();
        foreach (var card in await ParticipantCards.AllAsync())
        {
            var dishes = new List<DishQuantity>();
            foreach (var item in await card.GetByTestId("participant-item").AllAsync())
            {
                dishes.Add(new DishQuantity(
                    int.Parse(await item.GetAttributeAsync("data-quantity") ?? "0", CultureInfo.InvariantCulture),
                    await item.GetAttributeAsync("data-dish-name") ?? ""));
            }

            participants.Add(new ShownParticipant(
                await card.GetAttributeAsync("data-participant-name") ?? "",
                string.Equals(await card.GetAttributeAsync("data-submitted"), "true", StringComparison.Ordinal),
                (await card.GetByTestId("participant-status").InnerTextAsync()).Trim(),
                dishes,
                Amount(await card.GetByTestId("participant-total").GetAttributeAsync("data-amount"))));
        }

        return new ShownGroupPreOrder(
            participants,
            Amount(await GroupTotal.GetAttributeAsync("data-amount")),
            await NobodyJoined.IsVisibleAsync());
    }

    public async Task<ShownConfirmation> ReadConfirmationAsync()
    {
        var dishes = new List<DishQuantity>();
        var sent = await SentSummary.IsVisibleAsync();
        if (sent)
        {
            foreach (var line in await ConfirmedDishes.AllAsync())
            {
                dishes.Add(new DishQuantity(
                    int.Parse(await line.GetAttributeAsync("data-quantity") ?? "0", CultureInfo.InvariantCulture),
                    (await line.Locator(".dish-name").InnerTextAsync()).Trim()));
            }
        }

        var refusal = await Refusal.IsVisibleAsync() ? (await Refusal.InnerTextAsync()).Trim() : null;
        return new ShownConfirmation(sent, dishes, refusal);
    }

    private static decimal Amount(string? value) =>
        string.IsNullOrWhiteSpace(value) ? 0m : decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
}
