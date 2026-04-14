using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class ImageValidator : IBasicValidator
{
    private readonly string _imageName;
    private readonly bool _isPreimage;

    public ImageValidator(string imageName, bool isPreimage)
    {
        _imageName = imageName;
        _isPreimage = isPreimage;
    }

    public string GetName => nameof(ImageValidator);

    public bool Validate(TaskContext taskContext)
    {
        if (_isPreimage)
            return taskContext.PluginExecutionContext?.PreEntityImages?.ContainsKey(_imageName) ?? false;
        //post image
        return taskContext.PluginExecutionContext?.PostEntityImages?.ContainsKey(_imageName) ?? false;
    }

    public string GetMessage()
    {
        return $"Plugin does not contains {GetImageTitle()} with name {_imageName}";
    }

    private string GetImageTitle()
    {
        return _isPreimage ? "preimage" : "postimage";
    }
}