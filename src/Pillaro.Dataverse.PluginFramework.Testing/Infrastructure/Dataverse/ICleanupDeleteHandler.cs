using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

/// <summary>
/// Provides custom cleanup logic for deleting related Dataverse data before an entity is removed.
/// </summary>
/// <remarks>
/// This interface is used by <see cref="TestDataService"/> during test cleanup.
/// It allows implementing custom deletion of dependent or related records
/// that would otherwise block deletion due to relationships or constraints.
///
/// This is a Dataverse-specific infrastructure extension and is not part of the domain layer.
/// </remarks>
public interface ICleanupDeleteHandler
{
    string EntityLogicalName { get; }

    void DeleteReferences(EntityReference entity, ITestDataService testDataService);
}
