namespace Pillaro.Dataverse.PluginFramework.Tasks;

/// <summary>
/// The user a write is performed as. The values match the services of
/// <see cref="OrganizationServiceProvider"/>.
/// </summary>
public enum ServiceUser
{
    /// <summary>
    /// The user the step runs as. This is the default, so the audit shows who really changed the record.
    /// </summary>
    User = 0,

    /// <summary>
    /// The system user, for writes the calling user is not allowed to perform.
    /// The audit then shows the system user instead of the person who triggered the operation.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// The user who initiated the operation, which differs from <see cref="User"/> when the step
    /// runs in the context of another user.
    /// </summary>
    InitiatingUser = 2,
}
