namespace AeFinder.Grains;

public static class GrainIdHelper
{
    public static string GenerateGrainId(params object[] ids)
    {
        return ids.JoinAsString("-");
    }
}