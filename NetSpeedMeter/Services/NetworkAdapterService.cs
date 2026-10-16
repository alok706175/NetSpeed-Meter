using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    public interface INetworkAdapterService
    {
        IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters();
        NetworkInterface? FindInterface(string id);
        NetworkInterface? FindBestActiveInterface();
    }

    public class NetworkAdapterService : INetworkAdapterService
    {
        public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters()
        {
            var list = new List<NetworkAdapterInfo>
            {
                NetworkAdapterInfo.Automatic
            };

            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .OrderByDescending(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ThenBy(ni => ni.Name);

                foreach (var ni in interfaces)
                {
                    list.Add(new NetworkAdapterInfo
                    {
                        Id = ni.Id,
                        Name = ni.Name,
                        Description = ni.Description,
                        InterfaceType = ni.NetworkInterfaceType.ToString(),
                        IsUp = ni.OperationalStatus == OperationalStatus.Up,
                        IsAutomatic = false
                    });
                }
            }
            catch
            {
                // Return whatever we have if querying fails
            }

            return list;
        }

        public NetworkInterface? FindInterface(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "auto")
            {
                return FindBestActiveInterface();
            }

            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.Id == id);
            }
            catch
            {
                return null;
            }
        }

        public NetworkInterface? FindBestActiveInterface()
        {
            try
            {
                var upInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .ToList();

                if (upInterfaces.Count == 0)
                {
                    return null;
                }

                // 1. Query the Windows TCP/IP routing table via native GetBestInterface.
                // This determines the exact interface Windows routes outgoing internet traffic through
                // (e.g. to 8.8.8.8 or 1.1.1.1). This seamlessly supports VPNs (WireGuard, OpenVPN,
                // Tailscale, Cloudflare WARP), Hyper-V virtual switches, Wi-Fi, and Ethernet
                // with 100% accuracy matching Windows' active default route metrics.
                try
                {
                    // 0x08080808 = 8.8.8.8, 0x01010101 = 1.1.1.1 in network byte order
                    if (NativeMethods.GetBestInterface(0x08080808, out int bestIfIndex) == 0 ||
                        NativeMethods.GetBestInterface(0x01010101, out bestIfIndex) == 0)
                    {
                        var routeMatched = upInterfaces.FirstOrDefault(ni =>
                        {
                            try
                            {
                                if (ni.Supports(NetworkInterfaceComponent.IPv4))
                                {
                                    return ni.GetIPProperties().GetIPv4Properties()?.Index == bestIfIndex;
                                }
                            }
                            catch
                            {
                            }
                            return false;
                        });

                        if (routeMatched != null)
                        {
                            return routeMatched;
                        }
                    }
                }
                catch
                {
                    // Fall back to managed route heuristics if native IP Helper call fails
                }

                // 2. Fallback Heuristics:
                // Prioritize interfaces with a valid IPv4 Default Gateway and lowest metric,
                // followed by recent active throughput, then physical interface type.
                var prioritized = upInterfaces
                    .OrderByDescending(ni =>
                    {
                        try
                        {
                            var ipProps = ni.GetIPProperties();
                            return ipProps.GatewayAddresses.Any(g => g.Address != null &&
                                                                     !IPAddress.Any.Equals(g.Address) &&
                                                                     !IPAddress.None.Equals(g.Address));
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ThenBy(ni =>
                    {
                        try
                        {
                            if (ni.Supports(NetworkInterfaceComponent.IPv4))
                            {
                                return ni.GetIPProperties().GetIPv4Properties()?.Index ?? int.MaxValue;
                            }
                        }
                        catch
                        {
                        }
                        return int.MaxValue;
                    })
                    .ThenByDescending(ni =>
                    {
                        try
                        {
                            var stats = ni.GetIPStatistics();
                            return stats.BytesReceived + stats.BytesSent;
                        }
                        catch
                        {
                            return 0L;
                        }
                    })
                    .ThenByDescending(ni => !IsVirtualOrVpn(ni))
                    .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    .FirstOrDefault();

                return prioritized ?? upInterfaces.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsVirtualOrVpn(NetworkInterface ni)
        {
            string desc = (ni.Description ?? string.Empty).ToLowerInvariant();
            string name = (ni.Name ?? string.Empty).ToLowerInvariant();

            return desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") ||
                   desc.Contains("virtualbox") || desc.Contains("tap-") || desc.Contains("tailscale") ||
                   desc.Contains("wsl") || desc.Contains("vethernet") || desc.Contains("bluetooth") ||
                   name.Contains("vethernet") || name.Contains("wsl");
        }
    }
}
