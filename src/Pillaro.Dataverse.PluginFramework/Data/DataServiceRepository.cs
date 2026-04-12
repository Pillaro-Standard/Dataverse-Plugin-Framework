namespace Pillaro.Dataverse.PluginFramework.Data;

public class DataServiceRepository
{
    protected readonly DataService DataService;

    public DataServiceRepository(DataService dataService)
    {
        DataService = dataService;
    }
}