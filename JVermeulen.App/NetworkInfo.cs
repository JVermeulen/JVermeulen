using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace JVermeulen.App
{
    /// <summary>
    /// Static class for network info.
    /// </summary>
    public static class NetworkInfo
    {
        /// <summary>
        /// Machine name.
        /// </summary>
        public static string PrimaryHostname => Environment.MachineName;

        /// <summary>
        /// IP address (v4) from MachineName.
        /// </summary>
        public static IPAddress PrimaryIPAddress => GetIPAddress(Environment.MachineName, AddressFamily.InterNetwork);

        /// <summary>
        /// IP address (v6) from MachineName.
        /// </summary>
        public static IPAddress PrimaryIPAddressV6 => GetIPAddress(Environment.MachineName, AddressFamily.InterNetworkV6);

        /// <summary>
        /// Available network addresses.
        /// </summary>
        public static Dictionary<IPAddress, string> NetworkAddresses => GetNetworkAddresses(AddressFamily.InterNetwork);

        /// <summary>
        /// Available network addresses.
        /// </summary>
        public static Dictionary<IPAddress, string> NetworkAddressesV6 => GetNetworkAddresses(AddressFamily.InterNetworkV6);

        private static Dictionary<IPAddress, string> GetNetworkAddresses(AddressFamily family)
        {
            var name = Dns.GetHostName();
            var localEntry = Dns.GetHostEntry(name);

            return localEntry.AddressList.Where(a => a.AddressFamily == family)
                                         .ToDictionary(a => a, a => Dns.GetHostEntry(a)?.HostName);
        }

        private static IPAddress GetIPAddress(string hostname, AddressFamily family)
        {
            return Dns.GetHostEntry(hostname).AddressList.Where(a => a.AddressFamily == family).FirstOrDefault();
        }

        /// <summary>
        /// Get Hostname and IPAddress from the given hostname or IPAddress.
        /// </summary>
        /// <param name="hostnameOrIpAddress">Input (hostname or ip address).</param>
        /// <param name="family">IPAddress version (e.g. v4 or v6).</param>
        /// <param name="hostname">Output hostname.</param>
        /// <param name="ipAddresses">Output IPAddress(es).</param>
        /// <returns>No errors occured.</returns>
        public static bool TryGetDnsInfo(string hostnameOrIpAddress, AddressFamily family, out string hostname, out IPAddress[] ipAddresses)
        {
            hostname = null;
            ipAddresses = null;

            try
            {
                if (string.IsNullOrWhiteSpace(hostnameOrIpAddress))
                    hostnameOrIpAddress = Dns.GetHostName();

                if (IPAddress.TryParse(hostnameOrIpAddress, out IPAddress address))
                {
                    hostname = Dns.GetHostEntry(address).HostName;
                    ipAddresses = new IPAddress[] { address };
                }
                else
                {
                    hostname = hostnameOrIpAddress;
                    ipAddresses = Dns.GetHostEntry(hostnameOrIpAddress).AddressList.Where(a => a.AddressFamily == family)
                                                                                   .Select(a => a)
                                                                                   .ToArray();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
