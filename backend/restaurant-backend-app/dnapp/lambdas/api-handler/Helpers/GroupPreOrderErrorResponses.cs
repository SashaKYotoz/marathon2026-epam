using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using System;

namespace APIHandler.Helpers;

public static class GroupPreOrderErrorResponses
{
    public static bool IsHandled(Exception exception) => StatusCodeFor(exception) is not null;

    public static APIGatewayProxyResponse From(Exception exception, ILambdaLogger logger) =>
        ResponseBuilder.CreateErrorResponse(StatusCodeFor(exception) ?? 500, exception.Message, logger);

    public static int? StatusCodeFor(Exception exception) => exception switch
    {
        UnauthorizedAccessException => 401,
        ActionForbiddenException => 403,
        GroupReservationNotFoundException => 404,
        ReservationNotFoundException => 404,
        GuestSessionNotFoundException => 404,
        GroupReservationSubmittedException => 409,
        GroupReservationExpiredException => 409,
        ValidationException => 400,
        _ => null,
    };
}
