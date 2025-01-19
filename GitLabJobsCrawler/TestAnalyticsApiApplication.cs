using Autofac;
using Autofac.Extensions.DependencyInjection;
using Kontur.TestAnalytics.GitLabJobsCrawler;
using Kontur.TestAnalytics.GitLabJobsCrawler.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Builders;
using Vostok.Applications.AspNetCore.Middlewares;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;

namespace Kontur.TestAnalytics.Api;

[RequiresSecretConfiguration(typeof(GitLabSettings))]
public class TestAnalyticsGitLabCrawlerApplication : VostokAspNetCoreApplication<TestAnalyticsGitLabCrawlerApplicationStartup>
{
	public override void Setup(IVostokAspNetCoreApplicationBuilder builder, IVostokHostingEnvironment environment)
	{
		builder.SetupGenericHost(s => s.UseServiceProviderFactory(new AutofacServiceProviderFactory(c => BuildContainer(c, environment))));
		builder.DisableVostokMiddlewares();
	}

	private void BuildContainer(ContainerBuilder containerBuilder, IVostokHostingEnvironment environment)
	{
		containerBuilder.RegisterInstance(environment.SecretConfigurationProvider.Get<GitLabSettings>()).As<GitLabSettings>();
		containerBuilder.RegisterType<CrawlerInfoController>().As<ControllerBase>().AsSelf().InstancePerLifetimeScope();
		containerBuilder.RegisterType<GitLabCrawlerService>().AsSelf().SingleInstance();
	}

    public override Task WarmupServicesAsync(IVostokHostingEnvironment environment, IServiceProvider serviceProvider)
    {
		serviceProvider.GetRequiredService<GitLabCrawlerService>().Start();
        return base.WarmupServicesAsync(environment, serviceProvider);
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