using System;
using System.Collections.Generic;
using System.Diagnostics;
using BrowserRouter.Models;

namespace BrowserRouter.Services;

public class BrowserLauncher
{
    public void Launch(BrowserProfile profile, string url)
    {
        var args = new List<string>(profile.Args) { url };
        var argString = BuildArgString(args);

        var psi = new ProcessStartInfo
        {
            FileName = profile.Executable,
            Arguments = argString,
            UseShellExecute = false
        };

        Process.Start(psi);
    }

    private static string BuildArgString(IEnumerable<string> args)
    {
        var parts = new List<string>();
        foreach (var arg in args)
        {
            if (arg.Contains(' ') && !arg.StartsWith('"'))
                parts.Add($"\"{arg}\"");
            else
                parts.Add(arg);
        }
        return string.Join(" ", parts);
    }
}
