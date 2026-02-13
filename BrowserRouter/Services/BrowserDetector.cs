using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BrowserRouter.Models;

namespace BrowserRouter.Services;

public class DetectedProfile
{
    public string Name { get; set; } = "";
    public string Executable { get; set; } = "";
    public List<string> Args { get; set; } = new();
}

public class BrowserDetector
{
    public List<DetectedProfile> DetectAll()
    {
        var results = new List<DetectedProfile>();
        results.AddRange(DetectChromiumBrowser("Google Chrome", GetChromeExe()));
        results.AddRange(DetectChromiumBrowser("Microsoft Edge", GetEdgeExe()));
        results.AddRange(DetectChromiumBrowser("Vivaldi", GetVivaldiExe()));
        results.AddRange(DetectChromiumBrowser("Brave", GetBraveExe()));
        results.AddRange(DetectFirefoxProfiles());
        return results;
    }

    private static List<DetectedProfile> DetectChromiumBrowser(string browserName, string? exe)
    {
        var results = new List<DetectedProfile>();
        if (exe == null || !File.Exists(exe)) return results;

        var userDataDir = GetChromiumUserDataDir(browserName);
        if (userDataDir == null || !Directory.Exists(userDataDir)) return results;

        var localStatePath = Path.Combine(userDataDir, "Local State");
        if (!File.Exists(localStatePath)) return results;

        try
        {
            var json = File.ReadAllText(localStatePath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("profile", out var profileProp)) return results;
            if (!profileProp.TryGetProperty("info_cache", out var infoCache)) return results;

            foreach (var entry in infoCache.EnumerateObject())
            {
                var folderName = entry.Name;
                var displayName = entry.Value.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString() ?? folderName
                    : folderName;

                results.Add(new DetectedProfile
                {
                    Name = $"{browserName} - {displayName}",
                    Executable = exe,
                    Args = new List<string> { $"--profile-directory={folderName}" }
                });
            }
        }
        catch { }

        return results;
    }

    private static List<DetectedProfile> DetectFirefoxProfiles()
    {
        var results = new List<DetectedProfile>();
        var exe = GetFirefoxExe();
        if (exe == null || !File.Exists(exe)) return results;

        var profilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mozilla", "Firefox");
        var iniPath = Path.Combine(profilesDir, "profiles.ini");
        if (!File.Exists(iniPath)) return results;

        try
        {
            var lines = File.ReadAllLines(iniPath);
            string? profileName = null;
            string? profilePath = null;
            bool isRelative = true;

            foreach (var line in lines)
            {
                if (line.StartsWith("[Profile", StringComparison.OrdinalIgnoreCase))
                {
                    if (profileName != null && profilePath != null)
                        AddFirefoxProfile(results, exe, profileName, profilePath, isRelative, profilesDir);
                    profileName = null;
                    profilePath = null;
                    isRelative = true;
                }
                else if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
                    profileName = line[5..];
                else if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                    profilePath = line[5..];
                else if (line.StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase))
                    isRelative = line[11..].Trim() == "1";
            }

            if (profileName != null && profilePath != null)
                AddFirefoxProfile(results, exe, profileName, profilePath, isRelative, profilesDir);
        }
        catch { }

        return results;
    }

    private static void AddFirefoxProfile(List<DetectedProfile> results, string exe,
        string name, string path, bool isRelative, string profilesDir)
    {
        var fullPath = isRelative ? Path.Combine(profilesDir, path.Replace('/', Path.DirectorySeparatorChar)) : path;
        results.Add(new DetectedProfile
        {
            Name = $"Firefox - {name}",
            Executable = exe,
            Args = new List<string> { "--profile", fullPath, "-no-remote" }
        });
    }

    private static string? GetChromiumUserDataDir(string browserName) => browserName switch
    {
        "Google Chrome" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data"),
        "Microsoft Edge" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data"),
        "Vivaldi" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vivaldi", "User Data"),
        "Brave" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data"),
        _ => null
    };

    private static string? GetChromeExe() => FindExe(
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe");

    private static string? GetEdgeExe() => FindExe(
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe");

    private static string? GetVivaldiExe()
    {
        var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var vivaldiBase = Path.Combine(localApp, "Vivaldi", "Application");
        if (Directory.Exists(vivaldiBase))
        {
            var exes = Directory.GetFiles(vivaldiBase, "vivaldi.exe", SearchOption.TopDirectoryOnly);
            if (exes.Length > 0) return exes[0];
        }
        return FindExe(
            @"C:\Program Files\Vivaldi\Application\vivaldi.exe",
            @"C:\Program Files (x86)\Vivaldi\Application\vivaldi.exe");
    }

    private static string? GetBraveExe() => FindExe(
        @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
        @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe");

    private static string? GetFirefoxExe() => FindExe(
        @"C:\Program Files\Mozilla Firefox\firefox.exe",
        @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe");

    private static string? FindExe(params string[] paths) =>
        paths.FirstOrDefault(File.Exists);
}
