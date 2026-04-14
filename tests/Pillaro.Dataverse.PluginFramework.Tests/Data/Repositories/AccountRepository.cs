using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Tests.Data.Repositories;

public class AccountRepository : IAutoRegisteredTestDataRepository
{
    public Account GetNewAccount(int counter = 0)
    {
        return new Account
        {
            Name = $"Test - {counter} {DateTime.Now:O}",
            Description = "Test account",
            Telephone1 = "122 123 124"
        };
    }

    public Account GetNewAccount(string? name, string? description = null, string? telephone1 = null)
    {
        return new Account
        {
            Name = name,
            Description = description,
            Telephone1 = telephone1
        };
    }
}
