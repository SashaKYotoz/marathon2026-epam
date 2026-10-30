using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Business-level outcomes shared by several features ("the visitor is told ...", "could not be
/// found"). How an outcome shows up on the wire — which status code, which error body — is decided here,
/// so feature files never mention it.</summary>
[Binding]
public sealed class OutcomeStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Then("the visitor is told {string}")]
    [Then("the owner is told {string}")]
    [Scope(Tag = "api")]
    public async Task ThenVisitorIsTold(string message)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Succeeded).IsFalse().Because($"the request unexpectedly succeeded: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).IsEqualTo(message);
    }

    [Then("sign-in is refused")]
    public async Task ThenSignInIsRefused()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(401).Because($"response body was: {outcome.Body}");
    }

    [Then("the guest is told that the {item} could not be found")]
    public async Task ThenGuestIsToldNotFound(string item)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).Contains("was not found");
    }

    [Then("the location is not created")]
    public async Task ThenLocationIsNotCreated()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsGreaterThanOrEqualTo(400).Because($"the location was created: {outcome.Body}");
    }

    [Then("the information is available")]
    [Then("the menu is shown")]
    public async Task ThenInformationIsAvailable()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"response status {outcome.Status}, body: {outcome.Body}");
    }

    [Then("the request is refused because {refusal}")]
    public async Task ThenTheRequestIsRefused(Refusal refusal)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        var expected = refusal == Refusal.NoAccountSignedIn ? 401 : 403;

        await Assert.That(outcome.Status).IsEqualTo(expected).Because($"response body was: {outcome.Body}");
    }

    [Then("the owner is told the group pre-order could not be found")]
    [Then("the owner is told the participant could not be found")]
    public async Task ThenTheOwnerIsToldItCouldNotBeFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase)).IsTrue()
            .Because($"the error was: {outcome.ErrorMessage}");
    }

    [Then("the owner is told the participant is not part of this group")]
    public async Task ThenTheOwnerIsToldTheParticipantIsNotPartOfTheGroup()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(400).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).IsEqualTo("Guest session does not belong to this group reservation.");
    }

    [Then("the owner is told the group pre-order was already confirmed")]
    public async Task ThenTheOwnerIsToldItWasAlreadyConfirmed()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(409).Because($"response body was: {outcome.Body}");
    }

    [Then("the guest is told their cart is empty")]
    public async Task ThenTheGuestIsToldTheCartIsEmpty()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(400).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.Body.Contains("The cart is empty", StringComparison.Ordinal)).IsTrue()
            .Because($"response body was: {outcome.Body}");
    }
}
