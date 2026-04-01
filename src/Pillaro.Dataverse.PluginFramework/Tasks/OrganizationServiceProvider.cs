using Microsoft.Xrm.Sdk;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks;

/// <summary>
/// Provides organization services for supported Dataverse identities.
/// </summary>
public class OrganizationServiceProvider
{
    private readonly Func<IOrganizationService> _createUserOrganizationService;
    private readonly Func<IOrganizationService> _createAdminOrganizationService;
    private readonly Func<IOrganizationService> _createInitiatingUserOrganizationService;

    private IOrganizationService _user;
    private IOrganizationService _admin;
    private IOrganizationService _initiatingUser;

    public OrganizationServiceProvider(IOrganizationServiceFactory organizationServiceFactory, Guid? userId, Guid? initiatingUserId)
    {
        if (organizationServiceFactory == null) throw new ArgumentNullException(nameof(organizationServiceFactory));
        if (userId == null) throw new ArgumentNullException(nameof(userId));
        if (initiatingUserId == null) throw new ArgumentNullException(nameof(initiatingUserId));

        _createAdminOrganizationService = () => organizationServiceFactory.CreateOrganizationService(null);
        _createUserOrganizationService = () => organizationServiceFactory.CreateOrganizationService(userId);
        _createInitiatingUserOrganizationService = () => organizationServiceFactory.CreateOrganizationService(initiatingUserId);
    }

    public IOrganizationService User
    {
        get { return _user ??= _createUserOrganizationService(); }
    }

    public IOrganizationService Admin
    {
        get { return _admin ??= _createAdminOrganizationService(); }
    }

    public IOrganizationService InitiatingUser
    {
        get { return _initiatingUser ??= _createInitiatingUserOrganizationService(); }
    }
}