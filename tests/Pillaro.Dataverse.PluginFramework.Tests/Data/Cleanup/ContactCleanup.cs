using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;
using Pillaro.Dataverse.PluginFramework.Extensions;
namespace Pillaro.Dataverse.PluginFramework.Tests.Data.Cleanup;

public class ContactCleanup : ICleanupDeleteHandler
{
    string ICleanupDeleteHandler.EntityLogicalName { get => pl_AutoNumbering.EntityLogicalName; }

    public void DeleteReferences(EntityReference entity, ITestDataService testDataService, IOrganizationService organizationService)
    {
        var parConfs = testDataService.Query<pl_AutoNumbering>()
            .Where(a => a.pl_ParentAutoNumberingId.Id == entity.Id)
            .Select(o => new pl_AutoNumbering() { pl_AutoNumberingId = o.pl_AutoNumberingId })
            .ToList();

        organizationService.Delete(parConfs);
    }
}