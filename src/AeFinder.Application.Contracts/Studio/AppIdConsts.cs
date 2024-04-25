namespace AeFinder.Studio;

public static class AppIdConsts
{
    public static int MaxNameLength { get; set; } = 30;
    private const string QnameCharFmt = "[A-Za-z0-9]";
    private const string QnameExtCharFmt = "[-A-Za-z0-9_.]";
    public const string NameRegex = "(" + QnameCharFmt + QnameExtCharFmt + "*)?" + QnameCharFmt;
}