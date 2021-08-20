using System;
using System.Net;
using Xunit;

namespace JVermeulen.App.Tests
{
    public class NetworkInfoTest
    {
        [Fact]
        public void NameNotDefault()
        {
            NetworkInfo.PrimaryHostname.AssertNotDefault("NetworkInfo.PrimaryHostname");
        }

        [Fact]
        public void PrimaryIPAddresseNotDefault()
        {
            NetworkInfo.PrimaryIPAddress.AssertNotDefault("NetworkInfo.PrimaryIPAddress");
        }

        [Fact]
        public void PrimaryIPAddressV6NotDefault()
        {
            NetworkInfo.PrimaryIPAddressV6.AssertNotDefault("NetworkInfo.PrimaryIPAddressV6");
        }

        [Fact]
        public void NetworkAddressesNotDefault()
        {
            NetworkInfo.NetworkAddresses.AssertNotDefault("NetworkInfo.NetworkAddresses");
        }

        [Fact]
        public void GetDnsInfoFromIPAddress()
        {
            var stubIPAddress = "127.0.0.1";

            var result = NetworkInfo.TryGetDnsInfo(stubIPAddress, System.Net.Sockets.AddressFamily.InterNetwork, out string hostname, out IPAddress[] ipAddresses);

            Assert.True(result, $"NetorkInfo.TryGetDnsInfo failed with ip address {stubIPAddress}.");
        }

        [Fact]
        public void GetDnsInfoFromHostname()
        {
            var stubHostname = Environment.MachineName;

            var result = NetworkInfo.TryGetDnsInfo(stubHostname, System.Net.Sockets.AddressFamily.InterNetwork, out string hostname, out IPAddress[] ipAddresses);

            Assert.True(result, $"NetorkInfo.TryGetDnsInfo failed with hostname {stubHostname}.");
        }
    }
}
