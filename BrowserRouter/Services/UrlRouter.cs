using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BrowserRouter.Models;

namespace BrowserRouter.Services;

public class RouteResult
{
    public List<BrowserProfile> MatchedProfiles { get; set; } = new();
    public bool HasMatch { get; set; }
}

public class UrlRouter
{
    public RouteResult Route(string url, AppConfig config)
    {
        var normalizedUrl = NormalizeUrl(url);
        var matchedProfileIds = new List<string>();

        foreach (var rule in config.Rules)
        {
            if (Matches(normalizedUrl, rule))
            {
                matchedProfileIds.AddRange(rule.Profiles);
            }
        }

        // Deduplicate while preserving order
        var seen = new HashSet<string>();
        var uniqueIds = matchedProfileIds.Where(id => seen.Add(id)).ToList();

        var profiles = uniqueIds
            .Select(id => config.Browsers.FirstOrDefault(b => b.Id == id))
            .Where(b => b != null)
            .Select(b => b!)
            .ToList();

        return new RouteResult
        {
            MatchedProfiles = profiles,
            HasMatch = profiles.Count > 0
        };
    }

    private static string NormalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.ToString().TrimEnd('/');
        }
        catch
        {
            return url.ToLowerInvariant().TrimEnd('/');
        }
    }

    private static bool Matches(string url, UrlRule rule)
    {
        return rule.PatternType switch
        {
            PatternType.Regex => MatchesRegex(url, rule.Pattern),
            PatternType.Domain => MatchesDomain(url, rule.Pattern),
            PatternType.Prefix => url.StartsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool MatchesRegex(string url, string pattern)
    {
        try
        {
            return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesDomain(string url, string domain)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();
            var normalizedDomain = domain.ToLowerInvariant().TrimStart('.');
            return host == normalizedDomain || host.EndsWith("." + normalizedDomain);
        }
        catch
        {
            return false;
        }
    }
}
