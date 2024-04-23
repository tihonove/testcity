using Autofac;
using Autofac.Extensions.DependencyInjection;
using Kontur.TestAnalytics.Api.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Builders;
using Vostok.Applications.AspNetCore.Middlewares;
using Vostok.Hosting.Abstractions;

public class TestAnalyticsApiApplication : VostokAspNetCoreApplication<TestAnalyticsApiApplicationStartup>
{
    public override void Setup(IVostokAspNetCoreApplicationBuilder builder, IVostokHostingEnvironment environment)
    {
        builder.SetupGenericHost(s => s.UseServiceProviderFactory(new AutofacServiceProviderFactory(c => BuildContainer(c, environment))));
        builder.DisableVostokMiddlewares();
    }

    private void BuildContainer(ContainerBuilder containerBuilder, IVostokHostingEnvironment environment)
    {
        containerBuilder.RegisterType<StaticFilesController>().As<ControllerBase>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<ClickhouseProxyController>().As<ControllerBase>().AsSelf().InstancePerDependency();
    }
}

public class TestAnalyticsApiApplicationStartup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers();
		services.AddCors();
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

		app.UseEndpoints(
			endpoints => { endpoints.MapControllers(); });
	}
}
