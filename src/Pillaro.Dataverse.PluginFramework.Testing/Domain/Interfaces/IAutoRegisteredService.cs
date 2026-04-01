namespace Pillaro.Dataverse.PluginFramework.Testing.Domain.Interfaces;

/// <summary>
/// Marker interface for automatic registration of services in the Autofac container.
/// </summary>
/// <remarks>
/// This interface does not define any behavior.
/// It is used by the Autofac module to discover and register services automatically
/// based on convention, eliminating the need for manual registration.
/// </remarks>
public interface IAutoRegisteredService;