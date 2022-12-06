namespace AElfIndexer;

public class ApiOptions
{
    public int BlockQueryHeightInterval { get; set; } = 1000;
    public int TransactionQueryHeightInterval { get; set; } = 100;
    public int LogEventQueryHeightInterval { get; set; } = 100;
}