using System.Net.Sockets;

namespace ProxyCollector.Collector;
internal class Tcpinger
{
    public async Task<bool> Ping(string address,int port, int timeout)
    {
        using TcpClient client = new();
        using CancellationTokenSource cts = new(timeout);
        try
        {
            await client.ConnectAsync(address, port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
