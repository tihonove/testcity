using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using TestCity.Api.Models;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController() : ControllerBase
{
    [HttpGet("user")]
    public Task<ActionResult<UserInfoDto?>> GetUser()
    {
        if (!HttpContext.User?.Identity?.IsAuthenticated ?? false) {
            return Task.FromResult<ActionResult<UserInfoDto?>>(Ok(null));
        }
        var userName = HttpContext.User?.Identity?.Name
                    ?? HttpContext.User?.FindFirst("preferred_username")?.Value
                    ?? HttpContext.User?.FindFirst("name")?.Value
                    ?? HttpContext.User?.FindFirst("sub")?.Value;

        var userDisplayName = HttpContext.User?.FindFirst("preferred_username")?.Value
                    ?? HttpContext.User?.FindFirst("name")?.Value
                    ?? HttpContext.User?.FindFirst("sub")?.Value;

        var avatarUrl = HttpContext.User?.FindFirst("picture")?.Value;

        var userInfo = new UserInfoDto
        {
            UserName = userName,
            DisplayName = userDisplayName,
            AvatarUrl = avatarUrl
        };

        return Task.FromResult<ActionResult<UserInfoDto?>>(Ok(userInfo));
    }

    [HttpGet("login")]
    public Task<ActionResult> Login(string returnUrl)
    {
        return Task.FromResult<ActionResult>(
            Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = returnUrl ?? "/"
                },
                "oidc"));
    }
}
