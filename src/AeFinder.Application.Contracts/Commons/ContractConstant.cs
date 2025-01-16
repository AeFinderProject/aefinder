namespace AeFinder.Commons;

public class ContractConstant
{
    public const string USDT = "USDT";
    public const long USDTDecimals = 1000000;
}

public static class TransactionState
{
    public const string Mined = "MINED";
    public const string Pending = "PENDING";
    public const string NotExisted = "NOTEXISTED";
    public const string Failed = "FAILED";
    public const string NodeValidationFailed = "NODEVALIDATIONFAILED";
}