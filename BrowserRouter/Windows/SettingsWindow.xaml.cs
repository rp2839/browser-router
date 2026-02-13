using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using BrowserRouter.Models;
using BrowserRouter.Services;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace BrowserRouter.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private AppConfig _config;
    private bool _isDirty;

    public SettingsWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;
        _config = _configService.Load();
        LoadData();
    }

    private void LoadData()
    {
        ProfileListBox.ItemsSource = null;
        ProfileListBox.ItemsSource = _config.Browsers;

        RuleListBox.ItemsSource = null;
        RuleListBox.ItemsSource = _config.Rules;

        RefreshRuleProfilesList();
        UpdateRegistrationStatus();
        ConfigPathText.Text = ConfigService.GetConfigPath();
    }

    // ==================== PROFILES ====================

    private void ProfileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileListBox.SelectedItem is BrowserProfile profile)
        {
            ProfileNameBox.Text = profile.Name;
            ProfileExeBox.Text = profile.Executable;
            ProfileArgsBox.Text = string.Join(" ", profile.Args);
        }
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        ProfileListBox.SelectedItem = null;
        ProfileNameBox.Text = "New Profile";
        ProfileExeBox.Text = "";
        ProfileArgsBox.Text = "";
        ProfileNameBox.Focus();
        ProfileNameBox.SelectAll();
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        var name = ProfileNameBox.Text.Trim();
        var exe = ProfileExeBox.Text.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(exe))
        {
            MessageBox.Show("Name and Executable are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var argsText = ProfileArgsBox.Text.Trim();
        var args = string.IsNullOrEmpty(argsText)
            ? new List<string>()
            : ParseArgs(argsText);

        if (ProfileListBox.SelectedItem is BrowserProfile existing)
        {
            existing.Name = name;
            existing.Executable = exe;
            existing.Args = args;
        }
        else
        {
            var id = GenerateId(name);
            _config.Browsers.Add(new BrowserProfile { Id = id, Name = name, Executable = exe, Args = args });
        }

        _isDirty = true;
        RefreshProfiles();
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileListBox.SelectedItem is not BrowserProfile profile) return;
        if (MessageBox.Show($"Delete profile '{profile.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _config.Browsers.Remove(profile);
        _isDirty = true;
        RefreshProfiles();
    }

    private void AutoDetect_Click(object sender, RoutedEventArgs e)
    {
        var detector = new BrowserDetector();
        var detected = detector.DetectAll();

        if (detected.Count == 0)
        {
            MessageBox.Show("No browser profiles detected.", "Auto-Detect", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int added = 0;
        foreach (var d in detected)
        {
            var exists = _config.Browsers.Any(b => b.Executable == d.Executable &&
                string.Join(" ", b.Args) == string.Join(" ", d.Args));
            if (!exists)
            {
                _config.Browsers.Add(new BrowserProfile
                {
                    Id = GenerateId(d.Name),
                    Name = d.Name,
                    Executable = d.Executable,
                    Args = d.Args
                });
                added++;
            }
        }

        _isDirty = true;
        RefreshProfiles();
        MessageBox.Show($"Detected {detected.Count} profiles, added {added} new ones.",
            "Auto-Detect", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BrowseExe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
            Title = "Select Browser Executable"
        };
        if (dlg.ShowDialog() == true)
            ProfileExeBox.Text = dlg.FileName;
    }

    private void RefreshProfiles()
    {
        ProfileListBox.ItemsSource = null;
        ProfileListBox.ItemsSource = _config.Browsers;
        RefreshRuleProfilesList();
    }

    // ==================== RULES ====================

    private void RefreshRuleProfilesList()
    {
        RuleProfilesListBox.ItemsSource = null;
        RuleProfilesListBox.ItemsSource = _config.Browsers;
    }

    private void RuleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RuleListBox.SelectedItem is UrlRule rule)
        {
            RulePatternBox.Text = rule.Pattern;
            RuleCommentBox.Text = rule.Comment ?? "";

            // Set type combo
            foreach (ComboBoxItem item in RuleTypeCombo.Items)
            {
                if (item.Tag?.ToString() == rule.PatternType.ToString())
                {
                    RuleTypeCombo.SelectedItem = item;
                    break;
                }
            }

            // Select profiles in the list
            RuleProfilesListBox.SelectedItems.Clear();
            foreach (BrowserProfile profile in RuleProfilesListBox.Items)
            {
                if (rule.Profiles.Contains(profile.Id))
                    RuleProfilesListBox.SelectedItems.Add(profile);
            }
        }
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        RuleListBox.SelectedItem = null;
        RulePatternBox.Text = "";
        RuleCommentBox.Text = "";
        RuleTypeCombo.SelectedIndex = 0;
        RuleProfilesListBox.SelectedItems.Clear();
        RulePatternBox.Focus();
    }

    private void SaveRule_Click(object sender, RoutedEventArgs e)
    {
        var pattern = RulePatternBox.Text.Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            MessageBox.Show("Pattern is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var patternType = GetSelectedPatternType();
        var selectedProfiles = RuleProfilesListBox.SelectedItems.OfType<BrowserProfile>()
            .Select(p => p.Id).ToList();

        if (selectedProfiles.Count == 0)
        {
            MessageBox.Show("Select at least one profile.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (RuleListBox.SelectedItem is UrlRule existing)
        {
            existing.Pattern = pattern;
            existing.PatternType = patternType;
            existing.Profiles = selectedProfiles;
            existing.Comment = RuleCommentBox.Text.Trim() is { Length: > 0 } c ? c : null;
        }
        else
        {
            _config.Rules.Add(new UrlRule
            {
                Pattern = pattern,
                PatternType = patternType,
                Profiles = selectedProfiles,
                Comment = RuleCommentBox.Text.Trim() is { Length: > 0 } c2 ? c2 : null
            });
        }

        _isDirty = true;
        RefreshRules();
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if (RuleListBox.SelectedItem is not UrlRule rule) return;
        if (MessageBox.Show($"Delete rule '{rule.Pattern}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _config.Rules.Remove(rule);
        _isDirty = true;
        RefreshRules();
    }

    private void MoveRuleUp_Click(object sender, RoutedEventArgs e)
    {
        if (RuleListBox.SelectedItem is not UrlRule rule) return;
        var idx = _config.Rules.IndexOf(rule);
        if (idx <= 0) return;
        _config.Rules.RemoveAt(idx);
        _config.Rules.Insert(idx - 1, rule);
        _isDirty = true;
        RefreshRules();
        RuleListBox.SelectedItem = rule;
    }

    private void MoveRuleDown_Click(object sender, RoutedEventArgs e)
    {
        if (RuleListBox.SelectedItem is not UrlRule rule) return;
        var idx = _config.Rules.IndexOf(rule);
        if (idx >= _config.Rules.Count - 1) return;
        _config.Rules.RemoveAt(idx);
        _config.Rules.Insert(idx + 1, rule);
        _isDirty = true;
        RefreshRules();
        RuleListBox.SelectedItem = rule;
    }

    private void RefreshRules()
    {
        RuleListBox.ItemsSource = null;
        RuleListBox.ItemsSource = _config.Rules;
    }

    private PatternType GetSelectedPatternType()
    {
        if (RuleTypeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return Enum.TryParse<PatternType>(tag, out var pt) ? pt : PatternType.Domain;
        }
        return PatternType.Domain;
    }

    // ==================== REGISTRATION ====================

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var svc = new RegistryService(exePath);
        svc.Register();
        UpdateRegistrationStatus();
        MessageBox.Show(
            "Registered successfully.\n\nNow open Windows Settings > Apps > Default apps\nand set 'Browser Router' as your default browser.",
            "Registered", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Unregister_Click(object sender, RoutedEventArgs e)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var svc = new RegistryService(exePath);
        svc.Unregister();
        UpdateRegistrationStatus();
        MessageBox.Show("Unregistered.", "Browser Router", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenDefaultApps_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
    }

    private void UpdateRegistrationStatus()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        var svc = new RegistryService(exePath);
        RegistrationStatus.Text = svc.IsRegistered()
            ? "Status: Registered"
            : "Status: Not registered";
        RegistrationStatus.Foreground = svc.IsRegistered()
            ? System.Windows.Media.Brushes.Green
            : System.Windows.Media.Brushes.Gray;
    }

    private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
    {
        var dir = Path.GetDirectoryName(ConfigService.GetConfigPath());
        if (dir != null && Directory.Exists(dir))
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    // ==================== LOG ====================

    public void AddLogEntry(string entry)
    {
        LogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {entry}");
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogListBox.Items.Clear();
    }

    // ==================== BOTTOM BUTTONS ====================

    private void SaveAndClose_Click(object sender, RoutedEventArgs e)
    {
        _configService.Save(_config);
        _isDirty = false;
        Close();
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        if (_isDirty)
        {
            var result = MessageBox.Show("Save changes before closing?", "Unsaved Changes",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                _configService.Save(_config);
            else if (result == MessageBoxResult.Cancel)
                return;
        }
        Close();
    }

    // ==================== HELPERS ====================

    private static string GenerateId(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "-")
            .Trim('-');
    }

    private static List<string> ParseArgs(string argsText)
    {
        var result = new List<string>();
        var current = "";
        bool inQuote = false;
        foreach (var ch in argsText)
        {
            if (ch == '"') { inQuote = !inQuote; continue; }
            if (ch == ' ' && !inQuote)
            {
                if (current.Length > 0) { result.Add(current); current = ""; }
            }
            else current += ch;
        }
        if (current.Length > 0) result.Add(current);
        return result;
    }
}
