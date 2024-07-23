using Newtonsoft.Json;
using ProxyCollector.Models;
using System.Net;

namespace ProxyCollector.Services;

public sealed class IPToCountryResolver
{
    private readonly HttpClient _httpClient;

    public IPToCountryResolver()
    {
        _httpClient = new HttpClient();
    }

    public async Task<CountryInfo> GetCountry(string address, CancellationToken cancellationToken = default)
    {
        IPAddress? ip = null;
        if (!IPAddress.TryParse(address, out ip))
        {
            var ips = Dns.GetHostAddresses(address);
            ip = ips[0];
        }

        return await GetCountry(ip, cancellationToken);
    }

    public async Task<CountryInfo> GetCountry(IPAddress ip, CancellationToken cancellationToken = default)
    {
        string? response = null;
        for (int i = 1; i <= 5; i++)
        {
            try
            {
                response = await _httpClient.GetStringAsync($"https://api.iplocation.net/?ip={ip}");
                break;
            }
            catch (HttpRequestException)
            {
                if(i == 5)
                {
                    throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
        }

        var ipInfo = JsonConvert.DeserializeObject<IpLocationResponse>(response!)!;

        return new CountryInfo
        {
            CountryName = ipInfo.CountryName,
            CountryCode = ipInfo.CountryCode
        };
    }
}
