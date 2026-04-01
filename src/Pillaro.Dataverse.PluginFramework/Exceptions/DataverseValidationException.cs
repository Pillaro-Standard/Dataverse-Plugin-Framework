using System;
using System.Runtime.Serialization;

namespace Pillaro.Dataverse.PluginFramework.Exceptions;

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

    protected DataverseValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}