using SingBoxLib.Parsing;
using System.Collections.Concurrent;
using System.Text;
using SingBoxLib.Runtime.Testing;
using SingBoxLib.Runtime;
using Octokit;
using ProxyCollector.Configuration;
using ProxyCollector.Services.IpToCountry;
using ProxyCollector.Properties;
using ProxyCollector.Models;

namespace ProxyCollector.Collector;

public class ProxyCollector
{
    private readonly CollectorConfig _config;
    private readonly IPToCountryResolver _ipToCountryResolver;

    public ProxyCollector()
    {
        _config = CollectorConfig.Instance;
        _ipToCountryResolver = new IPToCountryResolver(Resources.ip2country_v4);
    }

    public async Task StartAsync()
    {
        var profiles = await CollectAllProfiesFromConfigSources();
        var workingResults = await TestProfiles(profiles);

        var profCountries = new List<(UrlTestResult TestResult, CountryInfo CountryInfo)>();
        foreach (var result in workingResults)
        {
            var country = await _ipToCountryResolver.GetCountry(result.Profile.Address!);
            profCountries.Add((result, country));
        }

        var finalResults = profCountries
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
            .SelectMany(x => x);

        var finalResult = new StringBuilder();
        foreach (var profile in finalResults)
        {
            finalResult.AppendLine(profile.ToProfileUrl());
        }
        await CommitToGithub(finalResult.ToString());

    }

    private async Task CommitToGithub(string results)
    {
        string? sha = null;
        var client = new GitHubClient(new ProductHeaderValue("ProxyCollector"))
        {
            Credentials = new Credentials(_config.GithubApiToken)
        };
        try
        {
            var contents = await client.Repository.Content.GetAllContents(_config.GithubUser, _config.GithubRepo, _config.ResultFilePath);
            sha = contents.FirstOrDefault()?.Sha;
        }
        catch { }

        if (sha is null)
        {
            await client.Repository
                .Content
                .CreateFile(_config.GithubUser, _config.GithubRepo, _config.ResultFilePath,
                new CreateFileRequest("Added subscription file", results));
        }
        else
        {
            await client.Repository
                .Content
                .UpdateFile(_config.GithubUser, _config.GithubRepo, _config.ResultFilePath,
                new UpdateFileRequest("Updated subscription", results, sha));
        }
    }
    private async Task<IEnumerable<UrlTestResult>> TestProfiles(List<ProfileItem> profiles)
    {
        var tester = new ParallelUrlTester(
            new SingBoxWrapper(_config.SingboxPath),
            Enumerable.Range(20000, _config.MaxThreadCount),
            _config.MaxThreadCount,
            _config.Timeout,
            _config.Retries);

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
            while ((line = reader.ReadLine()) is not null)
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