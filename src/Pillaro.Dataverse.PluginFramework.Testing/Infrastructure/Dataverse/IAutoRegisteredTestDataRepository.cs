namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

/// <summary>
/// Marker interface for automatic registration of test data repositories.
/// </summary>
/// <remarks>
/// This interface is used to mark repository implementations that should be
/// automatically discovered and registered in the dependency injection container.
///
/// It does not define any behavior and serves only as a convention-based registration marker.
/// </remarks>
public interface IAutoRegisteredTestDataRepository;