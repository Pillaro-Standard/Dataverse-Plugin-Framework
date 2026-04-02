using Microsoft.Xrm.Sdk;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Entity attributes validations
/// </summary>
public interface IBasicAttributeValidation : ICustomValidation
{
    /// <summary>
    /// Check whether at least one given attributes is present in entity, otherwise false
    /// </summary>
    IBasicAttributeValidation EntityWithAtLeastOneAttribute(Entity entity, params string[] attributeNames);

    /// <summary>
    /// Check whether predicate conditons is true and then at least one given attributes is present in entity
    /// If predicate is not true, validation is skipped
    /// </summary>
    IBasicAttributeValidation EntityWithAtLeastOneAttributeWhen(Func<TaskContext, bool> predicate, Entity entity, params string[] attributeNames);

    /// <summary>
    /// Returns true when all given attributes are present in entity, otherwise false
    /// </summary>
    IBasicAttributeValidation EntityWithAllAttributes(Entity entity, params string[] attributeNames);

    /// <summary>
    /// Returns true when predicate conditons is true and then all given attributes are present in entity
    /// If predicate is not true, validation is skipped
    /// </summary>
    IBasicAttributeValidation EntityWithAllAttributesWhen(Func<TaskContext, bool> predicate, Entity entity, params string[] attributeNames);
}
