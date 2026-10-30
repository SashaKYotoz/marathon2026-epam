using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrders;

namespace Restaurant_Aqa_Framework.Screenplay.Questions.GroupPreOrder;

public sealed class TheParticipantsReturned : IQuestion<GroupSessionsResponse>
{
    public Task<GroupSessionsResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GroupSessionsResponse>());
}
