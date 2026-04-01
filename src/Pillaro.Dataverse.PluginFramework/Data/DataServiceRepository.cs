using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Data;

public class DataServiceRepository
{
    protected readonly DataService DataService;
    protected readonly IOrganizationService OrganizationService;

    public DataServiceRepository(DataService dataService)
    {
        DataService = dataService;
        OrganizationService = dataService.OrganizationService;
    }
}