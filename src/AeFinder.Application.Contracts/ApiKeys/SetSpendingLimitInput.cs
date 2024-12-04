namespace AeFinder.ApiKeys;

public class SetSpendingLimitInput
{
    public bool IsEnableSpendingLimit { get; set; }
    public decimal SpendingLimitUsdt { get; set; }
}