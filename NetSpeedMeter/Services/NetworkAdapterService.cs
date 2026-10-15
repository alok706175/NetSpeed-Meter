using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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

                // Prioritize Ethernet and Wi-Fi adapters that have a default gateway or IP address
                var prioritized = upInterfaces
                    .OrderByDescending(ni =>
                    {
                        try
                        {
                            var ipProps = ni.GetIPProperties();
                            return ipProps.GatewayAddresses.Any(g => g.Address != null);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
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
                    .FirstOrDefault();

                return prioritized ?? upInterfaces.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
