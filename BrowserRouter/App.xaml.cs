using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using BrowserRouter.Models;
using BrowserRouter.Services;
using BrowserRouter.TrayIcon;
using BrowserRouter.Windows;

namespace BrowserRouter;

public partial class App : Application
{
    private TrayIconManager? _trayIcon;
    private ConfigService _configService = new();
    private UrlRouter _router = new();
    private BrowserLauncher _launcher = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var args = e.Args;

        if (args.Contains("--register"))
        {
            RunRegister();
            Shutdown(0);
            return;
        }

        if (args.Contains("--unregister"))
        {
            RunUnregister();
            Shutdown(0);
            return;
        }

        if (args.Contains("--settings"))
        {
            ShowSettings();
            return;
        }

        // Check if first arg looks like a URL
        var url = args.FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                        || a.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        if (url != null)
        {
            RouteUrl(url);
            return;
        }

        // No args: show settings + tray
        InitTray();
        ShowSettings();
    }

    private void RouteUrl(string url)
    {
        var config = _configService.Load();
        var result = _router.Route(url, config);

        if (result.MatchedProfiles.Count == 1)
        {
            LaunchProfile(result.MatchedProfiles[0], url);
            Shutdown(0);
        }
        else
        {
            var profilesToShow = result.MatchedProfiles.Count > 0
                ? result.MatchedProfiles
                : config.Browsers;

            if (profilesToShow.Count == 0)
            {
                MessageBox.Show(
                    "No browser profiles configured.\n\nOpen Browser Router settings to add profiles.",
                    "Browser Router", MessageBoxButton.OK, MessageBoxImage.Information);
                ShowSettings();
                return;
            }

            var picker = new PickerWindow(url, profilesToShow, config, _configService);
            picker.ProfileSelected += (profile) =>
            {
                LaunchProfile(profile, url);
                Shutdown(0);
            };
            picker.Cancelled += () => Shutdown(0);
            picker.Show();
        }
    }

    private void LaunchProfile(BrowserProfile profile, string url)
    {
        try
        {
            _launcher.Launch(profile, url);
            _trayIcon?.AddRecentUrl(url, profile.Name);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch {profile.Name}:\n{ex.Message}",
                "Browser Router", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitTray()
    {
        _trayIcon = new TrayIconManager(ShowSettings, () => Shutdown(0));
        _trayIcon.Initialize();
    }

    private void ShowSettings()
    {
        if (_trayIcon == null)
            InitTray();

        var existing = Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var settings = new SettingsWindow(_configService);
        settings.Show();
    }

    private void RunRegister()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var svc = new RegistryService(exePath);
        svc.Register();
        MessageBox.Show(
            "Browser Router has been registered.\n\n" +
            "Now go to Windows Settings > Apps > Default apps\n" +
            "and set 'Browser Router' as your default browser.",
            "Browser Router - Registered",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RunUnregister()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var svc = new RegistryService(exePath);
        svc.Unregister();
        MessageBox.Show("Browser Router has been unregistered.",
            "Browser Router", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
