using Autofac;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Settings;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Interfaces;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Testing;

public class FrameworkTestingAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAutoMapper(ThisAssembly);

        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(t => typeof(IAutoRegisteredService).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .AsImplementedInterfaces();

        var configuration = MediatRConfigurationBuilder
            .Create(ThisAssembly)
            .WithAllOpenGenericHandlerTypesRegistered()
            .Build();

        builder.RegisterMediatR(configuration);

        builder.Register(c => new MemoryCache(Options.Create(new MemoryCacheOptions())))
            .As<IMemoryCache>()
            .SingleInstance();

        builder.Register(c => c.Resolve<IDataverseConnectionService>().GetOrganizationService())
            .As<IOrganizationService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SettingsService>().AsSelf();
    }
}