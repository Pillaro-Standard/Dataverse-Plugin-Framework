using Pillaro.Dataverse.PluginFramework.Tasks;
using System;

namespace Pillaro.Dataverse.PluginFramework.Data;

/// <summary>
/// Single entry point for task code. Exposes DataService per security context.
/// </summary>
public class DataServiceProvider
{
    private readonly Func<DataService> _createUserDataService;
    private readonly Func<DataService> _createAdminDataService;

    private DataService _user;
    private DataService _admin;

    public DataServiceProvider(OrganizationServiceProvider organizationServiceProvider, int multipleRequestBatchSize = 1000, int waitOnAsyncProcessMaxLoop = 40)
    {
        if (organizationServiceProvider == null) throw new ArgumentNullException(nameof(organizationServiceProvider));

        _createUserDataService = () => new DataService(organizationServiceProvider.User, multipleRequestBatchSize, waitOnAsyncProcessMaxLoop);
        _createAdminDataService = () => new DataService(organizationServiceProvider.Admin, multipleRequestBatchSize, waitOnAsyncProcessMaxLoop);
    }

    public DataService User
    {
        get { return _user ??= _createUserDataService(); }
    }

    public DataService Admin
    {
        get { return _admin ??= _createAdminDataService(); }
    }
}
