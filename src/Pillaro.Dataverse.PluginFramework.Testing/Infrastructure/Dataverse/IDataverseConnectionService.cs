using Microsoft.PowerPlatform.Dataverse.Client;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Interfaces;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

public interface IDataverseConnectionService : IAutoRegisteredService
{
    IOrganizationServiceAsync2 GetOrganizationService(string connectionStringName = "Dataverse", bool ignoreCache = false);

    IOrganizationServiceAsync2 GetOrganizationService(Guid callerId, string connectionStringName = "Dataverse", bool ignoreCache = false);
}
