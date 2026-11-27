using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AxizGeo.Application.Common;

namespace AxizGeo.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(raw, out var userId))
            throw new UnauthorizedAppException("El token no contiene un identificador de usuario válido.");

        return userId;
    }

    public static string GetUserFullName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Name)
        ?? "Usuario";

    public static Guid? GetOperatorId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("operatorId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
