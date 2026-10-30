using System.Threading.Tasks;

namespace APIHandler.Services.Auth;

public interface IIdTokenValidator
{
    Task<string?> GetUserIdAsync(string idToken);
}
