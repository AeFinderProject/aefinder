using AElfIndexer.CodeOps.Validators;

namespace AElfIndexer.CodeOps;

public class CodeCheckException : Exception
{
    public CodeCheckException()
    {
    }

    public CodeCheckException(string message) : base(message)
    {
    }

    public CodeCheckException(string message, List<ValidationResult> findings) : base(message)
    {
        Findings = findings;
    }

    public List<ValidationResult> Findings { get; }
}

public class CodeCheckTimeoutException : CodeCheckException
{
    public CodeCheckTimeoutException()
    {
    }

    public CodeCheckTimeoutException(string message) : base(message)
    {
    }
}
