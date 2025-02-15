using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Middlewares;

namespace Kontur.TestAnalytics.Api;

#pragma warning disable RCS1102 // Make class static
public class TestAnalyticsApiApplicationStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddCors();
        services.AddResponseCompression();
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseVostokHttpContextTweaks();
        app.UseVostokRequestInfo();
        app.UseVostokDistributedContext();
        app.UseVostokTracing();
        app.UseMiddleware<ThrottlingMiddleware>();
        app.UseVostokPingApi();
        app.UseVostokDiagnosticApi();
        app.UseAuthentication();
        app.UseRouting();
        app.UseCors();
        app.UseAuthorization();
        app.UseResponseCompression();

        app.UseEndpoints(
            endpoints => endpoints.MapControllers());
    }
}
#pragma warning restore RCS1102 // Make class static
