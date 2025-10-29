using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TestCity.Api.Authorization;

public class NoAuthSchemeOptions : AuthenticationSchemeOptions
{
}

public class NoAuthHandler : AuthenticationHandler<NoAuthSchemeOptions>
{
    public NoAuthHandler(IOptionsMonitor<NoAuthSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Создаем фиктивного пользователя для случая, когда авторизация отключена
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Anonymous"),
            new Claim(ClaimTypes.NameIdentifier, "anonymous")
        };
        
        var identity = new ClaimsIdentity(claims, "NoAuth");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "NoAuth");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
