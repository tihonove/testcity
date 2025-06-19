using Microsoft.AspNetCore.Mvc;

namespace TestCity.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : Controller
{
    [HttpGet]
    public IActionResult CheckHealth()
    {
        return Ok("Ok");
    }
}
