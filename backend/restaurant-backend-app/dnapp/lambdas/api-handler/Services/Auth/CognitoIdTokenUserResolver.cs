using APIHandler.Helpers;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace APIHandler.Services.Auth;

public class CognitoIdTokenUserResolver : IIdTokenValidator
{
    public async Task<string?> GetUserIdAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        try
        {
            ClaimsIdentity? identity = await CognitoIdTokenValidator.ValidateAsync(idToken);
            if (identity is null)
                return null;

            return identity.FindFirst("sub")?.Value
                ?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
