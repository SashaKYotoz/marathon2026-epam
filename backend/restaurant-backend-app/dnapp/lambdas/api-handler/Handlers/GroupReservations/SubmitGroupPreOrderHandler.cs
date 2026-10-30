using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Models.GroupPreOrders;
using APIHandler.Services.GroupPreOrders;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

public class SubmitGroupPreOrderHandler(IGroupPreOrderService groupPreOrderService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out string? groupReservationId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'groupReservationId' in path parameters.", context.Logger);

        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);

        if (!RequestBodyHelper.TryParseAndValidate(request, context, out SubmitGroupPreOrderRequest? body, out APIGatewayProxyResponse? errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(body.ReservationId))
            return ResponseBuilder.CreateErrorResponse(400, "ReservationId is required.", context.Logger);

        try
        {
            var consolidated = await groupPreOrderService.SubmitGroupPreOrderAsync(userId, groupReservationId, body.ReservationId);
            return ResponseBuilder.CreateJsonResponse(200, consolidated);
        }
        catch (Exception exception) when (GroupPreOrderErrorResponses.IsHandled(exception))
        {
            return GroupPreOrderErrorResponses.From(exception, context.Logger);
        }
    }
}
