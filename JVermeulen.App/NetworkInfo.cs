﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.App
{
    public static class NetworkInfo
    {
        public static Dictionary<string, string> NetworkAddresses => GetNetworkAddresses();

        private static Dictionary<string, string> GetNetworkAddresses()
        {
            var name = Dns.GetHostName();
            var localEntry = Dns.GetHostEntry(name);

            return localEntry.AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                         .ToDictionary(a => a.ToString(), a => Dns.GetHostEntry(a)?.HostName);
        }
    }
}