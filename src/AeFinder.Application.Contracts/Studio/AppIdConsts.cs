namespace AeFinder.Studio;

public static class AppIdConsts
{
    //service resource name must be no more than 63 characters,so appid can only less than 20 characters
    public static int MaxNameLength { get; set; } = 20;
    public const string NameRegex = "[a-z0-9]([-a-z0-9]*[a-z0-9])?(\\.[a-z0-9]([-a-z0-9]*[a-z0-9])?)*";
}