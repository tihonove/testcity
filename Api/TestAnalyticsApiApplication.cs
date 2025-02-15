using Autofac;
using Autofac.Extensions.DependencyInjection;
using Kontur.TestAnalytics.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Builders;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;

namespace Kontur.TestAnalytics.Api;

[RequiresSecretConfiguration(typeof(GitLabSettings))]
public class TestAnalyticsApiApplication : VostokAspNetCoreApplication<TestAnalyticsApiApplicationStartup>
{
    public override void Setup(IVostokAspNetCoreApplicationBuilder builder, IVostokHostingEnvironment environment)
    {
        builder.SetupGenericHost(s => s.UseServiceProviderFactory(new AutofacServiceProviderFactory(c => BuildContainer(c, environment))));
        builder.DisableVostokMiddlewares();
    }

    private static void BuildContainer(ContainerBuilder containerBuilder, IVostokHostingEnvironment environment)
    {
        containerBuilder.RegisterInstance(environment.SecretConfigurationProvider.Get<GitLabSettings>()).As<GitLabSettings>();
        containerBuilder.RegisterType<StaticFilesController>().As<ControllerBase>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<GitlabController>().As<ControllerBase>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<ClickhouseProxyController>().As<ControllerBase>().AsSelf().InstancePerDependency();
    }
}
#pragma warning restore RCS1102 // Make class static
