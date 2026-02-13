using System.Collections.Generic;

namespace BrowserRouter.Models;

public class AppConfig
{
    public string? DefaultProfile { get; set; }
    public List<BrowserProfile> Browsers { get; set; } = new();
    public List<UrlRule> Rules { get; set; } = new();
}
