using Microsoft.Xrm.Sdk;
using System.Collections.Concurrent;

namespace Pillaro.Dataverse.PluginFramework.Tasks;

public class OrganizationServiceProvider
{
    private readonly IOrganizationServiceFactory _organizationServiceFactory;

    private readonly Guid _userId;
    private readonly Guid _initiatingUserId;

    private IOrganizationService _user;
    private IOrganizationService _admin;
    private IOrganizationService _initiatingUser;

    public OrganizationServiceProvider(IOrganizationServiceFactory organizationServiceFactory, Guid? userId, Guid? initiatingUserId)
    {
        if (organizationServiceFactory == null)
            throw new ArgumentNullException(nameof(organizationServiceFactory));

        if (userId == null)
            throw new ArgumentNullException(nameof(userId));

        if (initiatingUserId == null)
            throw new ArgumentNullException(nameof(initiatingUserId));

        _organizationServiceFactory = organizationServiceFactory;
        _userId = userId.Value;
        _initiatingUserId = initiatingUserId.Value;
    }

    public IOrganizationService User
        => _user ??= _organizationServiceFactory.CreateOrganizationService(_userId);

    public IOrganizationService Admin
        => _admin ??= _organizationServiceFactory.CreateOrganizationService(null);

    public IOrganizationService InitiatingUser
        => _initiatingUser ??= _organizationServiceFactory.CreateOrganizationService(_initiatingUserId);

    public IOrganizationService GetForUser(Guid userId)
    {
        return _organizationServiceFactory.CreateOrganizationService(userId);
    }
}