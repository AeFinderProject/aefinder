using System.Runtime.Serialization;

namespace AeFinder.App.OperationLimits;

public class OperationLimitException: Exception
{
    public OperationLimitException()
    {

    }

    public OperationLimitException(string message)
        : base(message)
    {

    }
    
    public OperationLimitException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    public OperationLimitException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {

    }
}