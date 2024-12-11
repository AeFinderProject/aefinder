using Nest;

namespace AeFinder.App.Es;

public class AppInfoImmutableIndex
{
    [Keyword]
    public string AppId { get; set; }
    [Keyword]
    public string AppName { get; set; }
}