using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;

public class AccountRepository : IAutoRegisteredTestDataRepository
{
    public Account GetNew(string name = "Testrecord Account")
    {
        return new Account
        {
            Name = name
        };
    }
}
