using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BorrowBox.Api.Security;

public static class BorrowBoxSecurity
{
    public const string Scheme = "DemoHeader";
    public const string StaffPolicy = "StaffOnly";
}

public sealed class DemoHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Demo-Role", out var values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = values.ToString();
        if (role is not ("Student" or "Staff"))
            return Task.FromResult(AuthenticateResult.Fail("X-Demo-Role must be Student or Staff."));

        var claims = new[] { new Claim(ClaimTypes.Name, "demo-user"), new Claim(ClaimTypes.Role, role) };
        var identity = new ClaimsIdentity(claims, BorrowBoxSecurity.Scheme);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, BorrowBoxSecurity.Scheme)));
    }
}
