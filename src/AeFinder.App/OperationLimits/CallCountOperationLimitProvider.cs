namespace AeFinder.App.OperationLimits;

public class CallCountOperationLimitProvider : IOperationLimitProvider
{
    protected int CallCount;

    public void Reset()
    {
        CallCount = 0;
    }
}