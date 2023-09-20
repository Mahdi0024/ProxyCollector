using Newtonsoft.Json;

namespace ProxyCollector.Models;

public class IpLocationResponse
{
    [JsonProperty("ip")]
    public string? Ip { get; set; }

    [JsonProperty("ip_number")]
    public string? IpNumber { get; set; }

    [JsonProperty("ip_version")]
    public int IpVersion { get; set; }

    [JsonProperty("country_name")]
    public string? CountryName { get; set; }

    [JsonProperty("country_code2")]
    public string? CountryCode { get; set; }

    [JsonProperty("isp")]
    public string? Isp { get; set; }

    [JsonProperty("response_code")]
    public string? ResponseCode { get; set; }

    [JsonProperty("response_message")]
    public string? ResponseMessage { get; set; }
}