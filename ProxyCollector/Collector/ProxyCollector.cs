using Octokit;
using ProxyCollector.Configuration;
using ProxyCollector.Services;
using SingBoxLib.Configuration;
using SingBoxLib.Configuration.Inbound;
using SingBoxLib.Configuration.Outbound;
using SingBoxLib.Configuration.Outbound.Abstract;
using SingBoxLib.Configuration.Route;
using SingBoxLib.Configuration.Shared;
using SingBoxLib.Parsing;
using SingBoxLib.Runtime;
using SingBoxLib.Runtime.Testing;
using System.Collections.Concurrent;
using System.Text;

namespace ProxyCollector.Collector;

public class ProxyCollector
{
    private readonly CollectorConfig _config;
    private readonly IPToCountryResolver _ipToCountryResolver;

    public ProxyCollector()
    {
        _config = CollectorConfig.Instance;
        _ipToCountryResolver = new IPToCountryResolver();
    }

    private void LogToConsole(string log)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay} - {log}");
    }

    public async Task StartAsync()
    {
        var startTime = DateTime.Now;
        LogToConsole("Collector started.");

        var profiles = (await CollectAllProfiesFromConfigSources()).Distinct().ToList();
        LogToConsole($"Collected {profiles.Count} unique profiles.");

        LogToConsole($"Beginning UrlTest proccess.");
        var workingResults = (await TestProfiles(profiles)).ToList();
        LogToConsole($"Testing has finished, found {workingResults.Count} working profiles.");

        LogToConsole($"Compiling results...");
        var finalResults = workingResults
            .Select(r => new { TestResult = r, CountryInfo = _ipToCountryResolver.GetCountry(r.Profile.Address!).Result })
            .GroupBy(p => p.CountryInfo.CountryCode)
            .Select
            (
                x => x.OrderBy(x => x.TestResult.Delay)
                    .WithIndex()
                    .Select(x =>
                    {
                        var profile = x.Item.TestResult.Profile;
                        var countryInfo = x.Item.CountryInfo;
                        profile.Name = $"{countryInfo.CountryFlag} {countryInfo.CountryName} {x.Index + 1}";
                        return (profile);
                    })
            )
            .SelectMany(x => x)
            .ToList();

        LogToConsole($"Uploading results...");
        await CommitResults(finalResults.ToList());

        var timeSpent = DateTime.Now - startTime;
        LogToConsole($"Job finished, time spent: {timeSpent}");
    }

    private async Task CommitResults(List<ProfileItem> profiles)
    {
        LogToConsole($"Uploading V2ray Subscription...");
        await CommitV2raySubscriptionResult(profiles);
        LogToConsole($"Uploading sing-box Subscription...");
        await CommitSingboxSubscription(profiles);
    }

    private async Task CommitSingboxSubscription(List<ProfileItem> profiles)
    {
        var outbounds = new List<OutboundConfig>(profiles.Count + 3);
        foreach (var profile in profiles)
        {
            var outbound = profile.ToOutboundConfig();
            outbound.Tag = profile.Name;
            outbounds.Add(outbound);
        }

        var allOutboundTags = profiles.Select(profile => profile.Name!).ToList();
        var selector = new SelectorOutbound
        {
            Outbounds = new List<string>(profiles.Count + 1)
            {
               "auto"
            },
            Default = "auto"
        };
        selector.Outbounds.AddRange(allOutboundTags);

        outbounds.Add(selector);
        outbounds.Add(new DnsOutbound());

        var urlTest = new UrlTestOutbound
        {
            Outbounds = allOutboundTags,
            Interval = "10m",
            Tolerance = 200,
            Url = "https://www.gstatic.com/generate_204",
        };
        outbounds.Add(urlTest);

        var config = new SingBoxConfig
        {
            Outbounds = outbounds,
            Inbounds = new()
            {
                new TunInbound
                {
                    InterfaceName = "tun0",
                    INet4Address = "172.19.0.1/30",
                    Mtu = 1500,
                    AutoRoute = true,
                    Stack = TunStacks.System,
                    EndpointIndependantNat = true,
                    StrictRoute = true,
                    Sniff = true,
                    SniffOverrideDestination = true,
                    DomainStrategy = DomainStrategies.PreferIPV4
                },
                new MixedInbound
                {
                    Listen = "127.0.0.1",
                    ListenPort = 2080,
                    Sniff = true,
                    SniffOverrideDestination = true,
                    DomainStrategy = DomainStrategies.PreferIPV4
                }
            },
            Route = new()
            {
                AutoDetectInterface = true,
                OverrideAndroidVpn = true,
                Final = "selector-out",
                Rules = new()
                {
                    new RouteRule
                    {
                        Port = new() { 53},
                        Outbound = "dns-out"
                    },
                }
            },
            Experimental = new()
            {
                ClashApi = new()
                {
                    ExternalController = "127.0.0.1:9090",
                    ExternalUi = "yacd",
                    ExternalUiDownloadUrl = "https://github.com/MetaCubeX/Yacd-meta/archive/gh-pages.zip",
                    StoreSelected = true,
                    CacheFile = "clash.db",
                }
            }
        };
        var finalResult = config.ToJson();

        await CommitFileToGithub(finalResult, _config.SingboxFormatResultPath);
    }

    private async Task CommitV2raySubscriptionResult(List<ProfileItem> profiles)
    {
        var finalResult = new StringBuilder();
        foreach (var profile in profiles)
        {
            finalResult.AppendLine(profile.ToProfileUrl());
        }
        await CommitFileToGithub(finalResult.ToString(), _config.V2rayFormatResultPath);
    }

    private async Task CommitFileToGithub(string content, string path)
    {
        string? sha = null;
        var client = new GitHubClient(new ProductHeaderValue("ProxyCollector"))
        {
            Credentials = new Credentials(_config.GithubApiToken)
        };
        try
        {
            var contents = await client.Repository.Content.GetAllContents(_config.GithubUser, _config.GithubRepo, path);
            sha = contents.FirstOrDefault()?.Sha;
        }
        catch { }

        if (sha is null)
        {
            await client.Repository
                .Content
                .CreateFile(_config.GithubUser, _config.GithubRepo, path,
                new CreateFileRequest("Added subscription file", content));
            LogToConsole("Result file did not exist, created a new file.");
        }
        else
        {
            await client.Repository
                .Content
                .UpdateFile(_config.GithubUser, _config.GithubRepo, path,
                new UpdateFileRequest("Updated subscription", content, sha));
            LogToConsole("Subscription file updated successfully.");
        }
    }

    private async Task<IEnumerable<UrlTestResult>> TestProfiles(IEnumerable<ProfileItem> profiles)
    {
        var tester = new ParallelUrlTester(
            new SingBoxWrapper(_config.SingboxPath),
    20000,
            _config.MaxThreadCount,
            _config.Timeout);

        var workingResults = new ConcurrentBag<UrlTestResult>();
        await tester.ParallelTestAsync(profiles, new Progress<UrlTestResult>((result =>
        {
            if (result.Success)
                workingResults.Add(result);
        })), default);
        return workingResults;
    }

    private async Task<List<ProfileItem>> CollectAllProfiesFromConfigSources()
    {
        using var client = new HttpClient();

        var subs = new ConcurrentBag<string>();
        await Parallel.ForEachAsync(_config.Sources, async (source, ct) =>
        {
            try
            {
                var result = await client.GetStringAsync(source);
                subs.Add(result);
            }
            catch { }
        });

        var profiles = Enumerable.Empty<ProfileItem>();
        foreach (var subContent in subs)
        {
            profiles = profiles.Concat(TryParseSubContent(subContent));
        }
        return profiles.ToList();

        IEnumerable<ProfileItem> TryParseSubContent(string subContent)
        {
            try
            {
                var contentData = Convert.FromBase64String(subContent);
                subContent = Encoding.UTF8.GetString(contentData);
            }
            catch
            {
            }

            var profiles = new List<ProfileItem>();

            using var reader = new StringReader(subContent);
            string? line = null;
            while ((line = reader.ReadLine()?.Trim()) is not null)
            {
                ProfileItem? profile = null;
                try
                {
                    profile = ProfileParser.ParseProfileUrl(line);
                }
                catch { }

                if (profile is not null)
                {
                    yield return profile;
                }
            }
        }
    }
}

public static class HelperExtentions
{
    public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> items)
    {
        int index = 0;
        foreach (var item in items)
        {
            yield return (index++, item);
        }
    }
}