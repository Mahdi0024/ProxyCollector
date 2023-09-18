using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProxyCollector.Services.IpToCountry;
public sealed class IpV4Range : IEnumerable<IPAddress>
{
    public uint Start { get; private init; }
    public uint End { get; private init; }
    public uint Lenght { get; private init; }

    public IpV4Range(uint start, uint end)
    {
        Start = start;
        End = end;

        Lenght = End - Start + 1;
    }
    public IpV4Range(IPAddress start, IPAddress end) : this(start.ToUint32(), end.ToUint32()) { }

    public bool Contains(IPAddress ip)
    {
        uint ipNumber = ip.ToUint32();
        return ipNumber >= Start && ipNumber <= End;
    }

    public bool Contains(uint ipNumber)
    {
        return ipNumber >= Start && ipNumber <= End;
    }

    public IEnumerator<IPAddress> GetEnumerator()
    {
        for (uint i = Start; i <= End; i++)
        {
            yield return i.ToIpV4();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
public static class IpV4Extentions
{
    public static uint ToUint32(this IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    public static IPAddress ToIpV4(this uint ipNumber)
    {
        var bytes = BitConverter.GetBytes(ipNumber);
        Array.Reverse(bytes);
        return new IPAddress(bytes);
    }
}