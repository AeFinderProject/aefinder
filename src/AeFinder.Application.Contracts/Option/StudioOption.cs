using System.Collections.Generic;

namespace AeFinder.Option;

public class StudioOption
{
    public List<AdminOption> AdminOptions { get; set; }
}

public class AdminOption
{
    public string AdminId { get; set; }
    public List<string> AppIds { get; set; }
}