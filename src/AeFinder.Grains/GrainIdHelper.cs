using AeFinder.ApiKeys;

namespace AeFinder.Grains;

public static class GrainIdHelper
{
    private static string BlockPushCheckGrainId => "BlockPushCheck";

    public static string GenerateGrainId(params object[] ids)
    {
        return ids.JoinAsString("-");
    }

    public static string GenerateAppSubscriptionGrainId(string appId)
    {
        return GenerateGrainId(appId);
    }

    public static string GenerateAeFinderNameGrainId(string appName)
    {
        const string namePrefix = "AeFinderApp";
        return GenerateGrainId(namePrefix, appName);
    }

    public static string GenerateAeFinderAppGrainId(string adminId)
    {
        const string namePrefix = "AeFinderApp";
        return GenerateGrainId(namePrefix, adminId);
    }

    public static string GenerateBlockPusherGrainId(string appId, string version, string chainId)
    {
        return GenerateGrainId(appId, version, chainId);
    }

    public static int GenerateBlockPusherManagerGrainId()
    {
        return 0;
    }

    public static string GenerateGetAppCodeGrainId(string appId, string version)
    {
        return GenerateGrainId(appId, version);
    }

    public static string GenerateAppStateGrainId(string appId, string version, string chainId, string key)
    {
        return GenerateGrainId(appId, version, chainId, key);
    }

    public static string GenerateAppBlockStateSetStatusGrainId(string appId, string version, string chainId)
    {
        return GenerateGrainId(appId, version, chainId);
    }

    public static string GenerateAppBlockStateSetGrainId(string appId, string version, string chainId, string blockHash)
    {
        return GenerateGrainId(appId, version, chainId, blockHash);
    }

    public static string GenerateBlockPushCheckGrainId()
    {
        return GenerateGrainId(BlockPushCheckGrainId);
    }

    public static string GenerateUserAppsGrainId(string userId)
    {
        const string userAppPrefix = "UserApps";
        return GenerateGrainId(userAppPrefix, userId);
    }
    
    public static string GenerateAppGrainId(string appId)
    {
        return GenerateGrainId(appId);
    }
    
    public static string GenerateOrganizationAppGrainId(string organizationId)
    {
        return GenerateGrainId(organizationId);
    }
    
    public static string GenerateOrganizationAppGrainId(Guid orgId)
    {
        return GenerateGrainId(orgId.ToString("N"));
    }
    
    public static string GenerateAppBlockStateChangeGrainId(string appId, string version, string chainId, long blockHeight)
    {
        return GenerateGrainId(appId, version, chainId, blockHeight);
    }
    
    public static int GenerateMessageStreamNamespaceManagerGrainId()
    {
        return 0;
    }
    
    public static string GenerateAppResourceLimitGrainId(string appId)
    {
        return GenerateGrainId(appId);
    }

    public static string GenerateAppIndexManagerGrainId(string appId, string version)
    {
        return GenerateGrainId(appId, version);
    }
    
    public static int GenerateAppDataClearManagerGrainId()
    {
        return 0;
    }
    
    public static string GenerateAppAttachmentGrainId(string appId, string version)
    {
        return GenerateGrainId(appId, version);
    }

    public static string GenerateAppSubscriptionProcessingStatusGrainId(string appId, string version)
    {
        return GenerateGrainId(appId, version);
    }
    
    public static string GenerateApiTrafficGrainId(string key, DateTime dateTime)
    {
        return GenerateGrainId(key,dateTime.ToString("yyyyMM"));
    }
    
    public static string GenerateAppPodOperationSnapshotGrainId(string appId, string version)
    {
        return GenerateGrainId(appId, version);
    }
    
    public static string GenerateApiKeySummaryGrainId(Guid organizationId)
    {
        return GenerateGrainId(organizationId.ToString("N"));
    }
    
    public static string GenerateApiKeySummaryMonthlySnapshotGrainId(Guid organizationId, DateTime dateTime)
    {
        return GenerateGrainId(organizationId.ToString("N"), dateTime.ToString("yyyyMM"));
    }
    
    public static string GenerateApiKeySummaryDailySnapshotGrainId(Guid organizationId, DateTime dateTime)
    {
        return GenerateGrainId(organizationId.ToString("N"), dateTime.ToString("yyyyMMdd"));
    }
    
    public static string GenerateApiKeyMonthlySnapshotGrainId(Guid apiKeyId, DateTime dateTime)
    {
        return GenerateGrainId(apiKeyId.ToString("N"), dateTime.ToString("yyyyMM"));
    }
    
    public static string GenerateApiKeyDailySnapshotGrainId(Guid apiKeyId, DateTime dateTime)
    {
        return GenerateGrainId(apiKeyId.ToString("N"), dateTime.ToString("yyyyMMdd"));
    }
    
    public static string GenerateApiKeyQueryAeIndexerGrainId(Guid apiKeyId, string appId)
    {
        return GenerateGrainId(apiKeyId.ToString("N"),appId);
    }
    
    public static string GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(Guid apiKeyId, string appId, DateTime dateTime)
    {
        return GenerateGrainId(apiKeyId.ToString("N"), appId, dateTime.ToString("yyyyMM"));
    }
    
    public static string GenerateApiKeyQueryAeIndexerDailySnapshotGrainId(Guid apiKeyId, string appId, DateTime dateTime)
    {
        return GenerateGrainId(apiKeyId.ToString("N"), appId, dateTime.ToString("yyyyMMdd"));
    }
    
    public static string GenerateApiKeyQueryBasicDataGrainId(Guid apiKeyId, BasicDataApiType basicDataApiType)
    {
        return GenerateGrainId(apiKeyId.ToString("N"),basicDataApiType);
    }
    
    public static string GenerateApiKeyQueryBasicDataMonthlySnapshotGrainId(Guid apiKeyId, BasicDataApiType basicDataApiType, DateTime dateTime)
    {
        return GenerateGrainId(apiKeyId.ToString("N"), basicDataApiType, dateTime.ToString("yyyyMM"));
    }
    
    public static string GenerateApiKeyQueryBasicDataDailySnapshotGrainId(Guid apiKeyId, BasicDataApiType basicDataApiType, DateTime dateTime)
    {
        return GenerateGrainId(apiKeyId.ToString("N"), basicDataApiType, dateTime.ToString("yyyyMMdd"));
    }
}