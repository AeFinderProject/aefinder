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
    
    public static int GenerateProductsGrainId()
    {
        return 0;
    }
    
    public static string GenerateOrdersGrainId(Guid orgId)
    {
        return GenerateGrainId(orgId.ToString("N"));
    }
    
    public static string GenerateRenewalGrainId(Guid orgId)
    {
        return GenerateGrainId(orgId.ToString("N"));
    }
    
    public static string GenerateBillsGrainId(Guid orgId)
    {
        return GenerateGrainId(orgId.ToString("N"));
    }
}