using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using APIHandler.Services.Auth;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public class GuestAuthorizationService(IGuestSessionService guestSessionService, IIdTokenValidator idTokenValidator) : IGuestAuthorizationService
{
    public async ValueTask<APIGatewayProxyResponse?> ValidateAsync(APIGatewayProxyRequest request, ILambdaContext context, string guestSessionid)
    {
        if (request.Headers is null || !TryGetAuthorizationHeader(request, out string authorizationValue))
            return CreateErrorResponse(context);

        const string BearerString = "Bearer ";
        if (!authorizationValue.StartsWith(BearerString) || authorizationValue.Length <= BearerString.Length)
            return CreateErrorResponse(context);

        string token = authorizationValue[BearerString.Length..];

        bool authorized = IsJwt(token)
            ? await IsOwnerAsync(token, guestSessionid)
            : await guestSessionService.IsAuthorizedAsync(token, guestSessionid);

        return !authorized ? CreateErrorResponse(context) : null;
    }

    private async Task<bool> IsOwnerAsync(string idToken, string guestSessionId)
    {
        string? userId = await idTokenValidator.GetUserIdAsync(idToken);
        return userId is not null && await guestSessionService.IsOwnerAuthorizedAsync(userId, guestSessionId);
    }

    private static bool IsJwt(string token) => token.Count(c => c == '.') == 2;

    private static bool TryGetAuthorizationHeader(APIGatewayProxyRequest request, out string authorizationValue)
    {
        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, "Authorization", System.StringComparison.OrdinalIgnoreCase) && header.Value is not null)
            {
                authorizationValue = header.Value;
                return true;
            }
        }

        authorizationValue = string.Empty;
        return false;
    }

    private static APIGatewayProxyResponse CreateErrorResponse(ILambdaContext context)
        => ResponseBuilder.CreateErrorResponse(401, "Invalid guest session token", context.Logger);
}
