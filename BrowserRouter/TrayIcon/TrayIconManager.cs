using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using BrowserRouter.Models;

namespace BrowserRouter.TrayIcon;

public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly Action _openSettings;
    private readonly Action _exitApp;
    private readonly List<string> _recentUrls = new();
    private const int MaxRecentUrls = 10;

    public TrayIconManager(Action openSettings, Action exitApp)
    {
        _openSettings = openSettings;
        _exitApp = exitApp;
    }

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Browser Router",
            Visible = true,
            Icon = LoadIcon()
        };

        _notifyIcon.DoubleClick += (_, _) => _openSettings();
        UpdateContextMenu();
    }

    public void AddRecentUrl(string url, string profileName)
    {
        _recentUrls.Insert(0, $"{profileName}: {url}");
        if (_recentUrls.Count > MaxRecentUrls)
            _recentUrls.RemoveAt(_recentUrls.Count - 1);
        UpdateContextMenu();
    }

    private void UpdateContextMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (_, _) => _openSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        if (_recentUrls.Count > 0)
        {
            var recentMenu = new ToolStripMenuItem("Recent URLs");
            foreach (var url in _recentUrls)
            {
                var item = new ToolStripMenuItem(TruncateUrl(url));
                item.ToolTipText = url;
                recentMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(recentMenu);
            menu.Items.Add(new ToolStripSeparator());
        }

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => _exitApp();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private static string TruncateUrl(string url, int maxLen = 60) =>
        url.Length > maxLen ? url[..maxLen] + "…" : url;

    private static Icon LoadIcon()
    {
        try
        {
            var exeDir = AppContext.BaseDirectory;
            var iconPath = System.IO.Path.Combine(exeDir, "Resources", "icon.ico");
            if (System.IO.File.Exists(iconPath))
                return new Icon(iconPath);
        }
        catch { }

        // Fallback: create a simple colored icon
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.SteelBlue);
            g.DrawString("B", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 1, 1);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
