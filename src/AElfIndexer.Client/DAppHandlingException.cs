using System.Runtime.Serialization;

namespace AElfIndexer.Client;

public class DAppHandlingException : Exception
{
    public DAppHandlingException()
    {

    }

    public DAppHandlingException(string message)
        : base(message)
    {

    }

    public DAppHandlingException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    public DAppHandlingException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {

    }
}