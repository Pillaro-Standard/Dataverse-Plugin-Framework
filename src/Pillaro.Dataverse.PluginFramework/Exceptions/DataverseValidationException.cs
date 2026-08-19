namespace Pillaro.Dataverse.PluginFramework.Exceptions;

/// <summary>
/// Expected business validation outcome that has to be shown to the user.
/// The task is logged as Success with Info severity, because stopping and informing the user
/// is the intended behaviour, not a failure. Use a technical exception when the task really failed.
/// </summary>
public class DataverseValidationException : Exception
{
    public DataverseValidationException()
    {
    }

    public DataverseValidationException(string message) : base(message)
    {
    }

    public DataverseValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}