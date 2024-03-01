using System.Runtime.Serialization;

namespace AElfIndexer.Client;

public class AppProcessingException : Exception
{
    public AppProcessingException()
    {

    }

    public AppProcessingException(string message)
        : base(message)
    {

    }

    public AppProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    public AppProcessingException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {

    }
}