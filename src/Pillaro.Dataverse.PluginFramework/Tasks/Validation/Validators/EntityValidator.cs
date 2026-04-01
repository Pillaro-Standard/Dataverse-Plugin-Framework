using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class EntityValidator : IBasicValidator
{
    private readonly IEnumerable<string> _entityNames;

    public EntityValidator(IEnumerable<string> entityNames)
    {
        _entityNames = entityNames;
    }

    public string GetName => nameof(EntityValidator);

    public bool Validate(TaskContext taskContext)
    {
        return _entityNames.Select(x => x.ToLower()).Contains(taskContext.PrimaryEntityName.ToLower());
    }

    public string GetMessage()
    {
        return $"Primary entity name is not in {String.Join(",", _entityNames)}";
    }
}