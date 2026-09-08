using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class ConsoleStatusFormatterTests
{
    [Theory]
    [InlineData("CREATE")]
    [InlineData("UPDATE")]
    [InlineData("DELETE")]
    [InlineData("OK")]
    [InlineData("CHANGE")]
    [InlineData("WARN")]
    [InlineData("ERROR")]
    [InlineData("TYPE-ONLY")]
    [InlineData("SOMETHING-UNKNOWN")]
    public void Label_AlwaysContainsBracketedStatus(string status)
    {
        var label = ConsoleStatusFormatter.Label(status);

        Assert.Contains($"[{status}]", label);
    }
}
