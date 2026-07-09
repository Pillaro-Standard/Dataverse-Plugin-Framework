namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

/// <summary>
/// Common Dataverse message names used for plugin step registration.
/// Custom API and custom action messages can still be registered by using their message name directly.
/// </summary>
public static class DataverseMessages
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Associate = "Associate";
    public const string Disassociate = "Disassociate";
    public const string Assign = "Assign";
    public const string SetState = "SetState";
}
