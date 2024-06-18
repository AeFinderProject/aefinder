using System.Collections.Generic;

namespace AeFinder.Apps;

public class FileBeatLogHostInfo
{
    public string Id { get; set; }
    public bool Containerized { get; set; }
    public List<string> Ip { get; set; }
    public List<string> Mac { get; set; }
    public string Name { get; set; }
    public string Hostname { get; set; }
    public string Architecture { get; set; }
    public FileBeatLogHostOS Os { get; set; }
}

public class FileBeatLogHostOS
{
    public string Name { get; set; }
    public string Kernel { get; set; }
    public string Codename { get; set; }
    public string Type { get; set; }
    public string Platform { get; set; }
    public string Version { get; set; }
    public string Family { get; set; }
}