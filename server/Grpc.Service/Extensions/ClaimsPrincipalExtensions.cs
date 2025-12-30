using System.Security.Claims;

namespace Grpc.Service.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims
            .FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    public static bool HasClaim(this ClaimsPrincipal principal, string claimType, string claimValue)
    {
        return principal.Claims
            .Any(c => c.Type == claimType && c.Value == claimValue);
    }

    public static IEnumerable<string> GetClaims(this ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims
            .Where(c => c.Type == claimType)
            .Select(c => c.Value);
    }
}

public static class ApiClaimTypes
{
    public const string ApiKey = "ApiKey";
    public const string ApiClientId = "ApiClientId";
    public const string ApiClientGroup = "ApiClientGroup";

}
