namespace AeFinder;

public class AeFinderApplicationConsts
{
    public const string MessageStreamName = "AeFinder";
    public const string MessageStreamNamespace = "default";
    public const string PrimaryKeyGrainIdSuffix = "BlockGrainPrimaryKey";
    public const string BlockGrainIdSuffix = "BlockGrain";
    public const string BlockBranchGrainIdSuffix = "BlockBranchGrain";
    public const int AppLogEventId = 1;
    public const string AppCurrentVersionCacheKeyPrefix = "AppCurrentVersionCache_";
    public const int AppCurrentVersionCacheHours = 24;
}