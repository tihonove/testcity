using Microsoft.AspNetCore.Authorization;

namespace TestCity.Api.Authorization;

public class ConditionalAuthorizationHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var authorizationEnabled = Environment.GetEnvironmentVariable("AUTHORIZATION_ENABLED")?.ToLowerInvariant() == "true";

        if (!authorizationEnabled)
        {
            // Если авторизация отключена, разрешаем все запросы
            foreach (var requirement in context.Requirements)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
