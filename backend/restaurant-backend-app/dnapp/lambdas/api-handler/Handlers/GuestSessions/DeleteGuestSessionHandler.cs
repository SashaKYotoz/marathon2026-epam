using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Services.GroupPreOrders;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

public class DeleteGuestSessionHandler(IGroupPreOrderService groupPreOrderService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out var groupReservationId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'groupReservationId' in path parameters.", context.Logger);

        if (!request.TryGetPathValue("guestSessionId", out var guestSessionId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'guestSessionId' in path parameters.", context.Logger);

        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);

        try
        {
            var updatedSnapshot = await groupPreOrderService.RemoveGuestSessionAsync(userId, groupReservationId, guestSessionId);
            return ResponseBuilder.CreateJsonResponse(200, updatedSnapshot);
        }
        catch (Exception exception) when (GroupPreOrderErrorResponses.IsHandled(exception))
        {
            return GroupPreOrderErrorResponses.From(exception, context.Logger);
        }
    }
}
