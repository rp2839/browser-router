using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using BrowserRouter.Models;
using BrowserRouter.Services;

namespace BrowserRouter.Windows;

public partial class PickerWindow : Window
{
    public event Action<BrowserProfile>? ProfileSelected;
    public event Action? Cancelled;

    private readonly string _url;
    private readonly AppConfig _config;
    private readonly ConfigService _configService;

    public PickerWindow(string url, List<BrowserProfile> profiles, AppConfig config, ConfigService configService)
    {
        InitializeComponent();
        _url = url;
        _config = config;
        _configService = configService;

        UrlText.Text = url;
        ProfileList.ItemsSource = profiles;
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is BrowserProfile profile)
        {
            if (RememberCheck.IsChecked == true)
                SaveDomainRule(profile);

            ProfileSelected?.Invoke(profile);
            Close();
        }
    }

    private void SaveDomainRule(BrowserProfile profile)
    {
        try
        {
            var uri = new Uri(_url);
            var domain = uri.Host;
            _config.Rules.Add(new UrlRule
            {
                Pattern = domain,
                PatternType = PatternType.Domain,
                Profiles = new List<string> { profile.Id },
                Comment = $"Auto-created from picker for {domain}"
            });
            _configService.Save(_config);
        }
        catch { }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (ProfileSelected == null)
            Cancelled?.Invoke();
    }
}
