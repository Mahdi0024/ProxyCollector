namespace ProxyCollector.Models;

public class CountryInfo
{
    public string? CountryCode { get; set; } = null!;
    public string? CountryName { get; set; } = null!;

    private string? _countryFlag;

    public string CountryFlag
    {
        get
        {
            if (_countryFlag is null)
                _countryFlag = IsoCountryCodeToFlagEmoji(CountryCode!);
            return _countryFlag;
        }
        set
        {
            _countryFlag = value;
        }
    }

    private string IsoCountryCodeToFlagEmoji(string countryCode)
    {
        if (countryCode is "Unknown" or null)
        {
            Console.WriteLine(CountryCode);
            return string.Empty;
        }
        return string.Concat(countryCode.Select(x => char.ConvertFromUtf32(x + 0x1F1A5)));
    }
}