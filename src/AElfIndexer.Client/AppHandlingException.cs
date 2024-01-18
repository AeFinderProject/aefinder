using System.Runtime.Serialization;

namespace AElfIndexer.Client;

public class AppHandlingException : Exception
{
    public AppHandlingException()
    {

    }

    public AppHandlingException(string message)
        : base(message)
    {

    }

    public AppHandlingException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    public AppHandlingException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {

    }
}