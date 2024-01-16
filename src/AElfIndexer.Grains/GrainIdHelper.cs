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
    
    public static string GenerateAppDataGrainId(string scanAppId, string version, string chainId, string entityKey)
    {
        return GenerateGrainId(scanAppId, version, chainId, entityKey);
    }
    
    public static string GenerateAppBlockStateSetGrainId(string scanAppId, string version, string chainId)
    {
        return GenerateGrainId(scanAppId, version, chainId);
    }
}