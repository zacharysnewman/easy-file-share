using System;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Open.Nat;

namespace EasyFileShare
{
    public static class Utils
    {
        [Serializable]
        private class PublicIP
        {
            public string ip;
        }

        public static async Task<string> GetPublicIpAddress()
        {
            try
            {
                using (var client = new WebClient())
                {
                    string response = await client.DownloadStringTaskAsync("https://ipv4.jsonip.com/");
                    return JsonConvert.DeserializeObject<PublicIP>(response).ip;
                }
            }
            catch (Exception)
            {
                return "Unable to retrieve public IP.";
            }
        }

        public static async Task ForwardPort(int port)
        {
            try
            {
                var discoverer = new NatDiscoverer();
                var cts = new System.Threading.CancellationTokenSource(5000); // Timeout after 5 seconds

                NatDevice device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

                if (device != null)
                {
                    await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, "FileSharing"));
                    Console.WriteLine($"Port {port} forwarded successfully.");
                }
                else
                {
                    Console.WriteLine($"Unable to discover UPnP device. Port forwarding may not work.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error forwarding port: {ex.Message}");
            }
        }

    }
}
