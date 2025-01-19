using Autofac;
using Autofac.Extensions.DependencyInjection;
using Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Builders;
using Vostok.Applications.AspNetCore.Middlewares;
using Vostok.Hosting.Abstractions;

namespace Kontur.TestAnalytics.Api;

public class TestAnalyticsGitLabCrawlerApplication : VostokAspNetCoreApplication<TestAnalyticsGitLabCrawlerApplicationStartup>
{
	public override void Setup(IVostokAspNetCoreApplicationBuilder builder, IVostokHostingEnvironment environment)
	{
		builder.SetupGenericHost(s => s.UseServiceProviderFactory(new AutofacServiceProviderFactory(c => BuildContainer(c, environment))));
		builder.DisableVostokMiddlewares();
	}

	private void BuildContainer(ContainerBuilder containerBuilder, IVostokHostingEnvironment environment)
	{
		containerBuilder.RegisterType<CrawlerInfoController>().As<ControllerBase>().AsSelf().InstancePerDependency();
		containerBuilder.RegisterType<GitLabCrawlerService>().AsSelf().SingleInstance();
	}
}

public class TestAnalyticsGitLabCrawlerApplicationStartup
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