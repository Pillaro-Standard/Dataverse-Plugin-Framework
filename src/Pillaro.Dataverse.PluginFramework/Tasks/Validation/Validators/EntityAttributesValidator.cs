using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class EntityAttributesValidator : IBasicValidator
{
    private readonly IEnumerable<string> _attributes;
    private readonly Entity _entity;
    private readonly bool _containsAll;
    private IEnumerable<string> _missingAttributes;

    public EntityAttributesValidator(Entity entity, IEnumerable<string> attributes, bool containsAll)
    {
        _attributes = attributes.Select(x => x.ToLower());
        _entity = entity;
        _containsAll = containsAll;
    }

    public string GetName => nameof(EntityAttributesValidator);

    public bool Validate(TaskContext taskContext)
    {
        List<string> entityAttributes = _entity.Attributes.Select(x => x.Key.ToLower()).ToList();
        _missingAttributes = GetMissingAttributes(entityAttributes);
        return !_missingAttributes.Any();
    }

    private List<string> GetMissingAttributes(List<string> entityAttributes)
    {
        if (_containsAll)
            return _attributes.Where(x => !entityAttributes.Contains(x)).ToList();
        //at least one
        return _attributes.Any(x => entityAttributes.Contains(x)) ? [] : _attributes.ToList();
    }

    public string GetMessage()
    {
        if (_containsAll)
            return $"Entity does not contain all of expected attributes. Missing attributes: {string.Join(", ", _missingAttributes)}";

        return $"Entity does not contain any of the attributes: {string.Join(", ", _attributes)}";
    }
}