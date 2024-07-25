namespace AeFinder.Options;

public class OperationLimitOptions
{
    public int MaxEntityCallCount
    {
        get
        {
            var defaultMaxEntityCallCount = 100;
            if ((int.TryParse(MaxEntityCallCountString, out var count)) && count > 0)
            {
                return count;
            }
            return defaultMaxEntityCallCount;
        }
    }
    public string MaxEntityCallCountString { get; set; }

    public int MaxEntitySize
    {
        get
        {
            var defaultMaxEntitySize = 100000;
            if ((int.TryParse(MaxEntitySizeString, out var size)) && size > 0)
            {
                return size;
            }
            return defaultMaxEntitySize;
        }
    }
    public string MaxEntitySizeString { get; set; }

    public int MaxLogCallCount
    {
        get
        {
            var defaultMaxLogCallCount = 100;
            if ((int.TryParse(MaxLogCallCountString, out var count)) && count > 0)
            {
                return count;
            }
            return defaultMaxLogCallCount;
        }
    }
    public string MaxLogCallCountString { get; set; }

    public int MaxLogSize
    {
        get
        {
            var defaultMaxLogSize = 100000;
            if ((int.TryParse(MaxLogSizeString, out var size)) && size > 0)
            {
                return size;
            }
            return defaultMaxLogSize;
        }
    }
    public string MaxLogSizeString { get; set; }

    public int MaxContractCallCount
    {
        get
        {
            var defaultMaxContractCallCount = 100;
            if ((int.TryParse(MaxContractCallCountString, out var count)) && count > 0)
            {
                return count;
            }
            return defaultMaxContractCallCount;
        }
    }

    public string MaxContractCallCountString { get; set; }
}