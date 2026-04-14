using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Tasks;

namespace Pillaro.Dataverse.PluginFramework.Data;

public class DataServiceProvider
{
    private readonly OrganizationServiceProvider _organizationServiceProvider;
    private readonly int _multipleRequestBatchSize;
    private readonly int _waitOnAsyncProcessMaxLoop;


    private DataService _user;
    private DataService _admin;
    private DataService _initiatingUser;

    public DataServiceProvider(OrganizationServiceProvider organizationServiceProvider, int multipleRequestBatchSize = 1000, int waitOnAsyncProcessMaxLoop = 40)
    {
        if (organizationServiceProvider == null)
            throw new ArgumentNullException(nameof(organizationServiceProvider));

        _organizationServiceProvider = organizationServiceProvider;
        _multipleRequestBatchSize = multipleRequestBatchSize;
        _waitOnAsyncProcessMaxLoop = waitOnAsyncProcessMaxLoop;
    }

    public DataService User
        => _user ??= Create(_organizationServiceProvider.User);

    public DataService Admin
        => _admin ??= Create(_organizationServiceProvider.Admin);

    public DataService InitiatingUser
        => _initiatingUser ??= Create(_organizationServiceProvider.InitiatingUser);

    public DataService ForUser(Guid userId)
    {
        var orgService = _organizationServiceProvider.GetForUser(userId);
        return Create(orgService);
    }

    private DataService Create(IOrganizationService service)
    {
        return new DataService(service, _multipleRequestBatchSize, _waitOnAsyncProcessMaxLoop);
    }
}