using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TestCity.Core.Infrastructure;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/reset")]
public class ResetCacheController(IServiceProvider serviceProvider) : Controller
{
    [HttpPost]
    public IActionResult ResetAllCaches()
    {
        var resetableServices = serviceProvider.GetServices<IResetable>();

        if (resetableServices?.Any() != true)
        {
            return Ok(new { message = "No resetable services found" });
        }

        var resetCount = 0;
        foreach (var service in resetableServices)
        {
            service.Reset();
            resetCount++;
        }

        return Ok(new { message = $"Successfully reset {resetCount} services" });
    }
}
