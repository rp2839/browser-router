using System;
using Microsoft.Win32;

namespace BrowserRouter.Services;

public class RegistryService
{
    private readonly string _exePath;

    public RegistryService(string exePath)
    {
        _exePath = exePath;
    }

    public void Register()
    {
        var exeArg = $"\"{_exePath}\" \"%1\"";

        // Register ProgID
        SetValue(@"Software\Classes\BrowserRouterURL", null, "BrowserRouter URL Handler");
        SetValue(@"Software\Classes\BrowserRouterURL", "URL Protocol", "");
        SetValue(@"Software\Classes\BrowserRouterURL\shell\open\command", null, exeArg);

        // Register http/https handlers
        SetValue(@"Software\Classes\http\shell\open\command", null, exeArg);
        SetValue(@"Software\Classes\https\shell\open\command", null, exeArg);

        // Register application capabilities
        SetValue(@"Software\BrowserRouter\Capabilities", "ApplicationName", "Browser Router");
        SetValue(@"Software\BrowserRouter\Capabilities", "ApplicationDescription", "Routes URLs to browser profiles");
        SetValue(@"Software\BrowserRouter\Capabilities\URLAssociations", "http", "BrowserRouterURL");
        SetValue(@"Software\BrowserRouter\Capabilities\URLAssociations", "https", "BrowserRouterURL");

        // Register in RegisteredApplications
        SetValue(@"Software\RegisteredApplications", "BrowserRouter", @"Software\BrowserRouter\Capabilities");
    }

    public void Unregister()
    {
        DeleteKey(@"Software\Classes\BrowserRouterURL");
        DeleteKey(@"Software\BrowserRouter");

        using var regApps = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", writable: true);
        regApps?.DeleteValue("BrowserRouter", throwOnMissingValue: false);

        // Restore http/https to system defaults if we own them
        RestoreProtocolHandler("http");
        RestoreProtocolHandler("https");
    }

    public bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\BrowserRouterURL\shell\open\command");
        if (key == null) return false;
        var value = key.GetValue(null) as string;
        return value != null && value.Contains("BrowserRouter.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetValue(string subKey, string? valueName, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey);
        key.SetValue(valueName, value);
    }

    private static void DeleteKey(string subKey)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
        }
        catch { }
    }

    private static void RestoreProtocolHandler(string protocol)
    {
        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{protocol}\shell\open\command");
        if (key == null) return;
        var value = key.GetValue(null) as string;
        if (value != null && value.Contains("BrowserRouter.exe", StringComparison.OrdinalIgnoreCase))
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{protocol}", throwOnMissingSubKey: false);
        }
    }
}
