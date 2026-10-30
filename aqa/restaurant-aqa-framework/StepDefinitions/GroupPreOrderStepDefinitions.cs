using System.Globalization;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Screenplay.Tasks.GroupPreOrder;
using Restaurant_Aqa_Framework.Support;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class GroupPreOrderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("a sample restaurant that serves:")]
    public async Task GivenASampleRestaurantThatServes(DataTable menu)
    {
        await Actor.AttemptsToAsync(new SetUpLocation());
        foreach (var row in menu.Rows)
            await Actor.AttemptsToAsync(new AddAMenuDish(row["dish"], decimal.Parse(row["price"], CultureInfo.InvariantCulture)));

        await Actor.AttemptsToAsync(new HireALocationWaiter());
    }

    [Given("the owner has booked a table for {int} guests")]
    public Task GivenTheOwnerHasBookedATable(int guests) => Actor.AttemptsToAsync(new BookAGroupTable(guests));

    [Given("{word} has joined the group")]
    [When("{word} joins the group")]
    public Task GivenAGuestHasJoined(string guest) => Actor.AttemptsToAsync(new JoinTheGroup(guest));

    [Given("{word} has joined the group and confirmed {dish_list}")]
    public Task GivenAGuestHasConfirmed(string guest, IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new JoinTheGroup(guest), new ChangeAGuestCart(guest, dishes), new ConfirmAGuestCart(guest));

    [Given("{word} has joined the group with {dish_list} but has not confirmed yet")]
    public Task GivenAGuestHasNotConfirmed(string guest, IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new JoinTheGroup(guest), new ChangeAGuestCart(guest, dishes));

    [Given("{word} has joined another group of the owner")]
    public async Task GivenAGuestHasJoinedAnotherGroup(string guest)
    {
        if (!Actor.RecallAll<GroupBooking>().Any(booking => booking.Name == GroupPreOrderPersonas.OtherBooking))
            await Actor.AttemptsToAsync(new BookAGroupTable(4, GroupPreOrderPersonas.OtherBooking));

        await Actor.AttemptsToAsync(new JoinTheGroup(guest, GroupPreOrderPersonas.OtherBooking));
    }

    [Given("{word} has added {dish_list} to their cart")]
    [When("{word} adds {dish_list} to their cart")]
    public Task WhenAGuestAddsDishes(string guest, IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new ChangeAGuestCart(guest, dishes));

    [When("{word} removes {dish_list} from their cart")]
    public Task WhenAGuestRemovesDishes(string guest, IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new ChangeAGuestCart(guest, dishes, removing: true));

    [When("{word} tries to confirm their empty cart")]
    public Task WhenAGuestTriesToConfirmAnEmptyCart(string guest) =>
        Actor.AttemptsToAsync(new ConfirmAGuestCart(guest, mustSucceed: false));

    [Given("the owner has already pre-ordered {dish_list} for the reservation")]
    public Task GivenTheOwnerHasPreOrdered(IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new PlaceOwnPreOrder(dishes));

    [Given("the owner has confirmed {dish_list} for themselves")]
    public Task GivenTheOwnerHasConfirmedOwnDishes(IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new ChooseOwnDishesThroughTheService(dishes, confirm: true));

    [Given("the owner has confirmed the group pre-order")]
    public async Task GivenTheOwnerHasConfirmedTheGroupPreOrder()
    {
        await Actor.AttemptsToAsync(new ConfirmTheGroupPreOrderThroughTheService(Requester.Owner));
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        if (!outcome.Succeeded)
            throw new InvalidOperationException($"Confirming the group pre-order failed with {outcome.Status}: {outcome.Body}");
    }

    [Given("the owner has removed {word} from the group")]
    public async Task GivenTheOwnerHasRemovedAGuest(string guest)
    {
        await Actor.AttemptsToAsync(new RemoveAParticipantThroughTheService(Requester.Owner, guest));
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        if (!outcome.Succeeded)
            throw new InvalidOperationException($"Removing {guest} failed with {outcome.Status}: {outcome.Body}");
    }

    [Given("the owner has opened the group pre-order")]
    [When("the owner opens the group pre-order")]
    [Scope(Tag = "ui")]
    public Task WhenTheOwnerOpensTheGroupPreOrder() =>
        Actor.AttemptsToAsync(new SignInToTheStorefront(Requester.Owner), new OpenTheGroupPreOrderPage(fromMyReservations: false));

    [When("the owner opens the group pre-order from their reservations")]
    [Scope(Tag = "ui")]
    public Task WhenTheOwnerOpensTheGroupPreOrderFromReservations() =>
        Actor.AttemptsToAsync(new SignInToTheStorefront(Requester.Owner), new OpenTheGroupPreOrderPage(fromMyReservations: true));

    [When("{requester} looks at the group participants")]
    [Scope(Tag = "api")]
    public Task WhenSomeoneLooksAtTheParticipants(Requester requester) =>
        Actor.AttemptsToAsync(new LookAtTheGroupParticipants(requester));

    [When("the owner looks at the participants of a group that does not exist")]
    [Scope(Tag = "api")]
    public Task WhenTheOwnerLooksAtAnUnknownGroup() =>
        Actor.AttemptsToAsync(new LookAtTheGroupParticipants(Requester.Owner, GroupTarget.AGroupThatDoesNotExist));

    [When("{requester} confirms the group pre-order")]
    [Scope(Tag = "api")]
    public Task WhenSomeoneConfirmsTheGroupPreOrder(Requester requester) =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrderThroughTheService(requester));

    [When("the owner confirms the group pre-order")]
    [Scope(Tag = "ui")]
    public Task WhenTheOwnerConfirmsTheGroupPreOrderOnThePage() =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrderOnThePage());

    [When("the owner confirms a group pre-order that does not exist")]
    [Scope(Tag = "api")]
    public Task WhenTheOwnerConfirmsAnUnknownGroup() =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrderThroughTheService(Requester.Owner, GroupTarget.AGroupThatDoesNotExist));

    [When("the owner confirms the group pre-order for a reservation that does not exist")]
    [Scope(Tag = "api")]
    public Task WhenTheOwnerConfirmsForAnUnknownReservation() =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrderThroughTheService(Requester.Owner, GroupTarget.AReservationThatDoesNotExist));

    [When("{requester} removes {word} from the group")]
    [Scope(Tag = "api")]
    public Task WhenSomeoneRemovesAGuest(Requester requester, string guest) =>
        Actor.AttemptsToAsync(new RemoveAParticipantThroughTheService(requester, guest));

    [When("the owner removes {word} from the group")]
    [Scope(Tag = "ui")]
    public Task WhenTheOwnerRemovesAGuestOnThePage(string guest) =>
        Actor.AttemptsToAsync(new RemoveAParticipantOnThePage(guest));

    [When("the owner removes a participant who does not exist")]
    [Scope(Tag = "api")]
    public Task WhenTheOwnerRemovesAnUnknownParticipant() =>
        Actor.AttemptsToAsync(new RemoveAParticipantThroughTheService(Requester.Owner, null));

    [When("{requester} opens their own cart in the group")]
    [Scope(Tag = "api")]
    public Task WhenSomeoneOpensTheirOwnCart(Requester requester) =>
        Actor.AttemptsToAsync(new OpenTheOwnCartThroughTheService(requester));

    [When("the owner chooses {dish_list} for themselves and confirms them")]
    [Scope(Tag = "ui")]
    public Task WhenTheOwnerChoosesOwnDishesOnThePage(IReadOnlyList<DishQuantity> dishes) =>
        Actor.AttemptsToAsync(new ChooseOwnDishesOnThePage(dishes, confirm: true));

    [When("the waiter opens the pre-order of the reservation")]
    [Scope(Tag = "ui")]
    public Task WhenTheWaiterOpensThePreOrder() => Actor.AttemptsToAsync(new OpenTheWaiterPreOrder());

    [Then("the owner sees these participants:")]
    [Scope(Tag = "api")]
    public async Task ThenTheOwnerSeesTheseParticipantsThroughTheService(DataTable participants)
    {
        var expected = ExpectedParticipants(participants);
        var snapshot = await Actor.AsksForAsync(new TheGroupSnapshot(shown => Describe(shown) == expected));

        await Assert.That(Describe(snapshot)).IsEqualTo(expected);
    }

    [Then("the owner sees these participants:")]
    [Scope(Tag = "ui")]
    public async Task ThenTheOwnerSeesTheseParticipantsOnThePage(DataTable participants)
    {
        var expected = ExpectedParticipants(participants);
        var shown = await Actor.AsksForAsync(new TheGroupPreOrderOnThePage(page => Describe(page) == expected));

        await Assert.That(Describe(shown)).IsEqualTo(expected);
    }

    [Then("the owner sees a group total of {int}")]
    [Scope(Tag = "api")]
    public async Task ThenTheOwnerSeesAGroupTotalThroughTheService(int total)
    {
        var snapshot = await Actor.AsksForAsync(new TheGroupSnapshot(shown => shown.Total == total));
        await Assert.That(snapshot.Total).IsEqualTo(total).Because($"participants were: {Describe(snapshot)}");
    }

    [Then("the owner sees a group total of {int}")]
    [Scope(Tag = "ui")]
    public async Task ThenTheOwnerSeesAGroupTotalOnThePage(int total)
    {
        var shown = await Actor.AsksForAsync(new TheGroupPreOrderOnThePage(page => page.Total == total));
        await Assert.That(shown.Total).IsEqualTo(total).Because($"participants were: {Describe(shown)}");
    }

    [Then("the owner sees that nobody has joined yet")]
    [Scope(Tag = "api")]
    public async Task ThenNobodyHasJoinedThroughTheService()
    {
        var snapshot = await Actor.AsksForAsync(new TheParticipantsReturned());

        await Assert.That(snapshot.Participants.Count).IsEqualTo(0).Because($"participants were: {Describe(snapshot)}");
        await Assert.That(snapshot.Total).IsEqualTo(0m);
    }

    [Then("the owner sees that nobody has joined yet")]
    [Scope(Tag = "ui")]
    public async Task ThenNobodyHasJoinedOnThePage()
    {
        var shown = await Actor.AsksForAsync(new TheGroupPreOrderOnThePage());

        await Assert.That(shown.NobodyJoinedShown).IsTrue().Because($"participants were: {Describe(shown)}");
        await Assert.That(shown.Participants.Count).IsEqualTo(0);
        await Assert.That(shown.Total).IsEqualTo(0m);
    }

    [Then("the owner gets back these participants:")]
    public async Task ThenTheOwnerGetsBackTheseParticipants(DataTable participants)
    {
        var snapshot = await Actor.AsksForAsync(new TheParticipantsReturned());
        await Assert.That(Describe(snapshot)).IsEqualTo(ExpectedParticipants(participants));
    }

    [Then("the owner gets back a group total of {int}")]
    public async Task ThenTheOwnerGetsBackAGroupTotal(int total)
    {
        var snapshot = await Actor.AsksForAsync(new TheParticipantsReturned());
        await Assert.That(snapshot.Total).IsEqualTo(total).Because($"participants were: {Describe(snapshot)}");
    }

    [Then("the owner gets back an empty group")]
    public async Task ThenTheOwnerGetsBackAnEmptyGroup()
    {
        var snapshot = await Actor.AsksForAsync(new TheParticipantsReturned());

        await Assert.That(snapshot.Participants.Count).IsEqualTo(0).Because($"participants were: {Describe(snapshot)}");
        await Assert.That(snapshot.Total).IsEqualTo(0m);
    }

    [Then("{word} is no longer in the group")]
    public async Task ThenAGuestIsNoLongerInTheGroup(string guest)
    {
        var name = GroupPreOrderPersonas.DisplayNameOf(Actor, guest);
        var snapshot = await Actor.AsksForAsync(new TheGroupSnapshot(shown => !Contains(shown, name)));

        await Assert.That(Contains(snapshot, name)).IsFalse().Because($"participants were: {Describe(snapshot)}");
    }

    [Then("{word} is still in the group")]
    public async Task ThenAGuestIsStillInTheGroup(string guest)
    {
        var name = GroupPreOrderPersonas.DisplayNameOf(Actor, guest);
        var snapshot = await Actor.AsksForAsync(new TheGroupSnapshot());

        await Assert.That(Contains(snapshot, name)).IsTrue().Because($"participants were: {Describe(snapshot)}");
    }

    [Then("the pre-order is sent to the restaurant with:")]
    [Scope(Tag = "api")]
    public async Task ThenThePreOrderIsSentThroughTheService(DataTable dishes)
    {
        var consolidated = await Actor.AsksForAsync(new TheConsolidatedPreOrderReturned());
        var byId = MenuDishesById();
        var actual = DishQuantities.Describe(consolidated.Select(item =>
            new DishQuantity(item.Quantity, byId.TryGetValue(item.DishId, out var name) ? name : item.DishId)));

        await Assert.That(actual).IsEqualTo(ExpectedDishes(dishes));
    }

    [Then("the pre-order is sent to the restaurant with:")]
    [Scope(Tag = "ui")]
    public async Task ThenThePreOrderIsSentOnThePage(DataTable dishes)
    {
        var confirmation = await Actor.AsksForAsync(new TheConfirmationOnThePage());

        await Assert.That(confirmation.Sent).IsTrue().Because($"the page said: {confirmation.Refusal}");
        await Assert.That(DishQuantities.Describe(confirmation.Dishes)).IsEqualTo(ExpectedDishes(dishes));
    }

    [Then("the owner is told {string}")]
    [Scope(Tag = "ui")]
    public async Task ThenTheOwnerIsToldOnThePage(string message)
    {
        var confirmation = await Actor.AsksForAsync(new TheConfirmationOnThePage());

        await Assert.That(confirmation.Sent).IsFalse();
        await Assert.That(confirmation.Refusal).IsEqualTo(message);
    }

    [Then("the restaurant has received the group pre-order")]
    public async Task ThenTheRestaurantHasReceivedTheGroupPreOrder()
    {
        var status = await Actor.AsksForAsync(new TheGroupStatus());
        var order = await Actor.AsksForAsync(new TheOrderSeenByTheWaiter());

        await Assert.That(status.Status).IsEqualTo("Submitted");
        await Assert.That(order.Status).IsEqualTo(OrderStatus.SubmittedByCustomer);
    }

    [Then("the waiter sees the pre-order:")]
    [Scope(Tag = "api")]
    public async Task ThenTheWaiterSeesThePreOrderThroughTheService(DataTable dishes)
    {
        var order = await Actor.AsksForAsync(new TheOrderSeenByTheWaiter());
        var actual = DishQuantities.Describe(order.OrderItems.Select(item => new DishQuantity(item.Quantity, item.Dish.Name)));

        await Assert.That(actual).IsEqualTo(ExpectedDishes(dishes));
    }

    [Then("the waiter sees the pre-order:")]
    [Scope(Tag = "ui")]
    public async Task ThenTheWaiterSeesThePreOrderOnThePage(DataTable dishes)
    {
        var shown = await Actor.AsksForAsync(new TheWaiterPreOrderOnThePage());
        await Assert.That(DishQuantities.Describe(shown)).IsEqualTo(ExpectedDishes(dishes));
    }

    [Then("the owner gets their own cart in the group")]
    public async Task ThenTheOwnerGetsTheirOwnCart()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"response status {outcome.Status}, body: {outcome.Body}");

        var ownCart = Actor.TheLastResponse().Json<OwnerSessionResponse>();
        await Assert.That(ownCart.GuestSessionId).IsNotNullOrEmpty();
        await Assert.That(ownCart.GroupReservationId).IsEqualTo(Recall<GroupBooking>(GroupPreOrderPersonas.MainBooking).GroupReservationId);
    }

    private string ExpectedParticipants(DataTable participants) =>
        string.Join(Environment.NewLine, participants.Rows
            .Select(row => Line(
                GroupPreOrderPersonas.DisplayNameOf(Actor, row["participant"]),
                DishQuantities.Describe(MenuNames(DishQuantities.Parse(row["dishes"]))),
                decimal.Parse(row["total"], CultureInfo.InvariantCulture),
                string.Equals(row["confirmed"], "yes", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(line => line, StringComparer.Ordinal));

    private string ExpectedDishes(DataTable dishes) =>
        DishQuantities.Describe(MenuNames(dishes.Rows
            .Select(row => new DishQuantity(int.Parse(row["quantity"], CultureInfo.InvariantCulture), row["dish"]))));

    private List<DishQuantity> MenuNames(IEnumerable<DishQuantity> dishes) =>
        dishes.Select(dish => dish with { Dish = Recall<DishResponse>(dish.Dish).Name }).ToList();

    private Dictionary<string, string> MenuDishesById() =>
        Actor.RecallAll<DishResponse>()
            .GroupBy(dish => dish.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.Ordinal);

    private static string Describe(GroupSessionsResponse snapshot) =>
        string.Join(Environment.NewLine, snapshot.Participants
            .Select(participant => Line(
                participant.Name,
                DishQuantities.Describe(participant.Items.Select(item => new DishQuantity(item.Quantity, item.Dish.Name))),
                participant.Total,
                participant.IsSubmitted))
            .OrderBy(line => line, StringComparer.Ordinal));

    private static string Describe(ShownGroupPreOrder shown) =>
        string.Join(Environment.NewLine, shown.Participants
            .Select(participant => Line(participant.Name, DishQuantities.Describe(participant.Dishes), participant.Total, participant.Confirmed))
            .OrderBy(line => line, StringComparer.Ordinal));

    private static bool Contains(GroupSessionsResponse snapshot, string name) =>
        snapshot.Participants.Any(participant => string.Equals(participant.Name, name, StringComparison.Ordinal));

    private static string Line(string name, string dishes, decimal total, bool confirmed) =>
        $"{name} | {dishes} | {total.ToString("0.##", CultureInfo.InvariantCulture)} | {(confirmed ? "yes" : "no")}";
}
