using Microsoft.Xrm.Sdk.Messages;

namespace Pillaro.Dataverse.PluginFramework.AutoNumbering;

public class AutoNumberingResponse
{
    public string Number { get; set; }

    public UpdateRequest Request { get; set; }
}
