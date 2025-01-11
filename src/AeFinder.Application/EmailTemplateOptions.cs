using System.Collections.Generic;

namespace AeFinder;

public class EmailTemplateOptions
{
    public Dictionary<string, EmailTemplate> Templates { get; set; } = new();
}

public class EmailTemplate
{
    public string From { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsBodyHtml { get; set; }
}