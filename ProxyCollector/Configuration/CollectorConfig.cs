namespace ProxyCollector.Configuration;

public class CollectorConfig
{
    public static CollectorConfig Instance { get; private set; }
    public required string GithubApiToken { get; init; } = null!;
    public required string GithubUser { get; init; } = null!;
    public required string GithubRepo { get; init; } = null!;
    public required string SingboxPath { get; init; } = null!;
    public required string V2rayFormatResultPath { get; init; } = null!;
    public required string SingboxFormatResultPath { get; init; } = null!;
    public required int MaxThreadCount { get; init; }
    public required int Timeout { get; init; }
    public required string[] Sources { get; init; } = null!;

    static CollectorConfig()
    {
        Instance = CreateInstance();
    }

    private CollectorConfig() { }

    private static CollectorConfig CreateInstance()
    {
        return new CollectorConfig
        {
            GithubApiToken = Environment.GetEnvironmentVariable("GithubApiToken")!,
            GithubUser = Environment.GetEnvironmentVariable("GithubUser")!,
            GithubRepo = Environment.GetEnvironmentVariable("GithubRepo")!,
            V2rayFormatResultPath = Environment.GetEnvironmentVariable("V2rayFormatResultPath")!,
            SingboxFormatResultPath = Environment.GetEnvironmentVariable("SingboxFormatResultPath")!,
            SingboxPath = Environment.GetEnvironmentVariable("SingboxPath")!,
            MaxThreadCount = int.Parse(Environment.GetEnvironmentVariable("MaxThreadCount")!),
            Timeout = int.Parse(Environment.GetEnvironmentVariable("Timeout")!),
            Sources = Environment.GetEnvironmentVariable("Sources")!.Split("\n")
        };
    }
}