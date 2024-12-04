namespace AeFinder.ApiKeys;

public class CreateApiKeyInput
{
    public string Name { get; set; }
    public bool IsEnableSpendingLimit { get; set; }
    public decimal SpendingLimitUsdt { get; set; }
}