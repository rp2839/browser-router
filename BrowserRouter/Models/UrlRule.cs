using System.Collections.Generic;

namespace BrowserRouter.Models;

public enum PatternType
{
    Regex,
    Domain,
    Prefix
}

public class UrlRule
{
    public string Pattern { get; set; } = "";
    public PatternType PatternType { get; set; } = PatternType.Domain;
    public List<string> Profiles { get; set; } = new();
    public string? Comment { get; set; }
}
