using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class ImageValidator : IBasicValidator
{
    private readonly string _imageName;
    private readonly bool _isPreimage;
    private bool _isRegistered;

    public ImageValidator(string imageName, bool isPreimage)
    {
        _imageName = imageName;
        _isPreimage = isPreimage;
    }

    public string GetName => nameof(ImageValidator);

    public bool Validate(TaskContext taskContext)
    {
        var images = GetImages(taskContext);

        _isRegistered = images != null && images.ContainsKey(_imageName);

        if (!_isRegistered)
            return false;

        // A task reads the image entity, not the collection entry. An entry without data passes
        // a plain presence check and still leaves the task with a null image.
        return images[_imageName] != null;
    }

    public string GetMessage()
    {
        if (_isRegistered)
            return $"Plugin contains {GetImageTitle()} with name {_imageName}, but it does not contain any data";

        return $"Plugin does not contains {GetImageTitle()} with name {_imageName}";
    }

    private EntityImageCollection GetImages(TaskContext taskContext)
    {
        if (_isPreimage)
            return taskContext?.PluginExecutionContext?.PreEntityImages;

        //post image
        return taskContext?.PluginExecutionContext?.PostEntityImages;
    }

    private string GetImageTitle()
    {
        return _isPreimage ? "preimage" : "postimage";
    }
}
