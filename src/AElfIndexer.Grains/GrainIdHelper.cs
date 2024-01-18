namespace AElfIndexer.Grains;

public static class GrainIdHelper
{
    public static string GenerateGrainId(params object[] ids)
    {
        return ids.JoinAsString("-");
    }

    public static string GenerateScanAppGrainId(string scanAppId)
    {
        return GenerateGrainId(scanAppId);
    }
    
    public static string GenerateBlockScanGrainId(string scanAppId, string version, string chainId)
    {
        return GenerateGrainId(scanAppId, version, chainId);
    }
    
    public static int GenerateBlockScanManagerGrainId()
    {
        return 0;
    }
    
    public static string GenerateSubscriptionGrainId(string scanAppId, string version)
    {
        return GenerateGrainId(scanAppId, version);
    }
    
    public static string GenerateAppStateGrainId(string scanAppId, string version, string chainId, string key)
    {
        return GenerateGrainId(scanAppId, version, chainId, key);
    }
    
    public static string GenerateAppBlockStateSetStatusGrainId(string scanAppId, string version, string chainId)
    {
        return GenerateGrainId(scanAppId, version, chainId);
    }
    
    public static string GenerateAppBlockStateSetGrainId(string scanAppId, string version, string chainId, string blockHash)
    {
        return GenerateGrainId(scanAppId, version, chainId, blockHash);
    }
}