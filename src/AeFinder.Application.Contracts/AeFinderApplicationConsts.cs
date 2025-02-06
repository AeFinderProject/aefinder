using System.Collections.Generic;

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
    public const int AppCurrentVersionCacheHours = 24 * 30;
    public const string RelateAppExtraDataKey = "RelateAppId";
    public const int DefaultAssetExpiration = 100; // 100 years
    public const string RegisterEmailTemplate = "Register";
    public const string AdminRoleName = "admin";
    
    public static readonly HashSet<string> AppInterestedExtraPropertiesKey = new HashSet<string>
        { "RefBlockNumber", "RefBlockPrefix", "ReturnValue", "Error", "TransactionFee", "ResourceFee" };
}