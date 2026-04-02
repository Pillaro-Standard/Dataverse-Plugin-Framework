using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Plugin image validation
/// </summary>
public interface IBasicImageValidation : IBasicAttributeValidation
{
    /// <summary>
    /// Check, if preimage with given name is registered
    /// </summary>
    IBasicImageValidation HasPreImage(string imageName = "image");

    /// <summary>
    /// Check, whether predicate conditions is true and then if preimage with given name is registered.
    /// If predicate is not true, validation is skipped.
    /// </summary>
    IBasicImageValidation HasPreImageWhen(Func<TaskContext, bool> predicate, string imageName = "image");
    /// <summary>
    /// Check, if postimage with given name is registered
    /// </summary>
    IBasicImageValidation HasPostImage(string imageName = "image");

    /// <summary>
    /// Check whether predicate conditions is true and then postimage with given name is registered.
    /// If predicate is not true, validation is skipped.
    /// </summary>
    IBasicImageValidation HasPostImageWhen(Func<TaskContext, bool> predicate, string imageName = "image");
}
