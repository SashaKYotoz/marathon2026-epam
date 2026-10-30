using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheConsolidatedPreOrderReturned : IQuestion<IReadOnlyList<ConsolidatedItemResponse>>
{
    public Task<IReadOnlyList<ConsolidatedItemResponse>> AnsweredByAsync(Actor actor) =>
        Task.FromResult<IReadOnlyList<ConsolidatedItemResponse>>(actor.TheLastResponse().Json<List<ConsolidatedItemResponse>>());
}
