using CountryData.Standard;
using IPinfo;
using ProxyCollector.Configuration;
using ProxyCollector.Models;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;

namespace ProxyCollector.Services.IpToCountry;

public sealed class IPToCountryResolver
{
 //   private readonly List<IpRangeInfo> _ipV4Ranges = new();
 //   private readonly IPinfoClient _ipInfoClient;
    private readonly HttpClient _httpClient;

    public IPToCountryResolver(string ipToCountryData)
    {
        //LoadIpToCountryData(ipToCountryData);
        _httpClient = new HttpClient();
        //_ipInfoClient = new IPinfoClient.Builder()
        //    .AccessToken(CollectorConfig.Instance.IpInfoApiToken)
        //    .Build();
    }

    //private void LoadIpToCountryData(string ipToCountryData)
    //{
    //    using var reader = new StringReader(ipToCountryData);
    //    var countryHelper = new CountryHelper();

    //    string? line;
    //    while ((line = reader.ReadLine()) is not null)
    //    {
    //        var csvData = line.Split('\t');

    //        var start = csvData[0];
    //        var end = csvData[1];
    //        var countryCode = csvData[2];
    //        if (countryCode is "None" or "Unknown")
    //        {
    //            countryCode = "Unknown";
    //        };
    //        var countryInfo = countryHelper.GetCountryByCode(countryCode);

    //        var countryName = countryCode is not "Unknown" ? countryInfo?.CountryName ?? countryCode : "Unknown";
    //        var countryFlag = countryInfo?.CountryFlag;

    //        _ipV4Ranges.Add(new IpRangeInfo
    //        {
    //            Range = new IpV4Range(IPAddress.Parse(start), IPAddress.Parse(end)),
    //            CountryCode = countryCode,
    //            CountryName = countryName,
    //            CountryFlag = countryFlag
    //        });
    //    }
    //}

    public async ValueTask <CountryInfo> GetCountry(string address,CancellationToken cancellationToken = default)
    {
        IPAddress? ip = null;
        if(!IPAddress.TryParse(address,out ip))
        {
            var ips = Dns.GetHostAddresses(address);
            ip = ips[0];
        }

        return await GetCountry(ip, cancellationToken);
    }

    public async ValueTask<CountryInfo> GetCountry(IPAddress ip, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IpLocationResponse>($"https://api.iplocation.net/?ip={ip}");

        return new CountryInfo
        {
            CountryName = response!.CountryName,
            CountryCode = response!.CountryCode
        };

        //if (ip.AddressFamily is AddressFamily.InterNetwork)
        //{
        //    var lookupResult = LookUpIPV4(ip);
        //    return new CountryInfo
        //    {
        //        CountryName = lookupResult?.CountryName!,
        //        CountryCode = lookupResult?.CountryCode!,
        //        CountryFlag = lookupResult?.CountryFlag!
        //    };

        //}
        //else
        //    return await ResolveIPV6(ip, cancellationToken);
    }

    //private async Task<CountryInfo> ResolveIPV6(IPAddress ip, CancellationToken cancellationToken)
    //{
    //    var result = await _ipInfoClient.IPApi.GetDetailsAsync(ip, cancellationToken);
    //    return new CountryInfo
    //    {
    //        CountryCode = result?.Country ?? "Unknown",
    //        CountryName = result?.CountryName ?? "Unknown",
    //        CountryFlag = result?.CountryFlag?.Emoji ?? ""
    //    };
    //}

    //private IpRangeInfo? LookUpIPV4(IPAddress ip)
    //{
    //    var ipNumber = ip.ToUint32();

    //    int min = 0;
    //    int max = _ipV4Ranges.Count - 1;
    //    while (min <= max)
    //    {
    //        var mid = (min + max) / 2;
    //        var rangeInfo = _ipV4Ranges[mid];
    //        if (rangeInfo.Range.Contains(ipNumber))
    //            return rangeInfo;
    //        if (ipNumber < rangeInfo.Range.Start)
    //            max = mid - 1;
    //        else
    //        {
    //            min = mid + 1;
    //        }
    //    }
    //    return null;
    //}

}