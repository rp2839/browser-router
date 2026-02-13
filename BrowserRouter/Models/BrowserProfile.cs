using System.Collections.Generic;

namespace BrowserRouter.Models;

public class BrowserProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Executable { get; set; } = "";
    public List<string> Args { get; set; } = new();
    public string? Icon { get; set; }
}
