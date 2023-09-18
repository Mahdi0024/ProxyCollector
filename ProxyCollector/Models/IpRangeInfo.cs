using ProxyCollector.Services.IpToCountry;

namespace ProxyCollector.Models;
public class IpRangeInfo
{
    public IpV4Range Range { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public string CountryName { get; set; } = null!;
    public string? CountryFlag { get; set; }
}
