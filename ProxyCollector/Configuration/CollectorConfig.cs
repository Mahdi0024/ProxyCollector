namespace ProxyCollector.Configuration;

public class CollectorConfig
{
    public static CollectorConfig Instance { get; private set; }
    public string GithubApiToken { get; private set; } = null!;
    public string GithubUser { get; private set; } = null!;
    public string GithubRepo { get; private set; } = null!;
    public string SingboxPath { get; private set; } = null!;
    public string V2rayFormatResultPath { get; private set; } = null!;
    public string SingboxFormatResultPath { get; set; } = null!;
    public int MaxThreadCount { get; private set; }
    public int Timeout { get; private set; }
    public int Retries { get; private set; }
    public string[] Sources { get; private set; } = null!;
    public string IpInfoApiToken { get; private set; } = null!;

    static CollectorConfig()
    {
        Instance = CreateInstance();
    }
    private CollectorConfig()
    {

    }
    private static CollectorConfig CreateInstance()
    {
        return new CollectorConfig
        {
            GithubApiToken = Environment.GetEnvironmentVariable("GithubApiToken")!,
            GithubUser = Environment.GetEnvironmentVariable("GithubUser")!,
            GithubRepo = Environment.GetEnvironmentVariable("GithubRepo")!,
            V2rayFormatResultPath = Environment.GetEnvironmentVariable("V2rayFormatResultPath")!,
            SingboxFormatResultPath = Environment.GetEnvironmentVariable("SingboxFormatResultPath")!,
            IpInfoApiToken = Environment.GetEnvironmentVariable("IpInfoApiToken")!,
            SingboxPath = Environment.GetEnvironmentVariable("SingboxPath")!,
            MaxThreadCount = int.Parse(Environment.GetEnvironmentVariable("MaxThreadCount")!),
            Timeout = int.Parse(Environment.GetEnvironmentVariable("Timeout")!),
            Retries = int.Parse(Environment.GetEnvironmentVariable("Retries")!),
            Sources = Environment.GetEnvironmentVariable("Sources")!.Split("\n")
        };
    }
}