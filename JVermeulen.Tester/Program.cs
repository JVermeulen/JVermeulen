using JVermeulen.App;
using JVermeulen.App.Windows;
using JVermeulen.MQTT;
using JVermeulen.Processing;
using JVermeulen.Spatial.Types;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using JVermeulen.TCP.Encoders;
using System;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            TestSpatial();
        }

        private static void TestSpatial()
        {
            var p1 = new Point(0, 0);
            var p2 = new Point(155000, 463000);
            var polyline = new Polyline(new Point[] { p1, p2 });
            var polygon = new Polygon(new Polyline[] { polyline, polyline });

            Console.WriteLine($"{p1}");
            Console.WriteLine($"{p2}");
            Console.WriteLine($"{polyline}");
            Console.WriteLine($"{polygon}");
        }

        private static void TestAppInfo()
        {
            Console.WriteLine("AppInfo:");
            Console.WriteLine($"- Name={AppInfo.Name}");
            Console.WriteLine($"- Title={AppInfo.Title}");
            Console.WriteLine($"- Version={AppInfo.Version}");
            Console.WriteLine($"- Architecture={AppInfo.Architecture}");
            Console.WriteLine($"- Description={AppInfo.Description}");
            Console.WriteLine($"- Guid={AppInfo.Guid}");
            Console.WriteLine($"- Is64bit={AppInfo.Is64bit}");
            Console.WriteLine($"- FileName={AppInfo.FileName}");
            Console.WriteLine($"- DirectoryName={AppInfo.DirectoryName}");
            Console.WriteLine($"- HasUI={AppInfo.HasUI}");
            Console.WriteLine($"- HasWindow={AppInfo.HasWindow}");
            Console.WriteLine($"- Type={AppInfo.Type}");
            Console.WriteLine($"- Culture={AppInfo.Culture}");
            Console.WriteLine($"- IsDebug={AppInfo.IsDebug}");
            Console.WriteLine($"- OSFriendlyName={AppInfo.OSFriendlyName}");
            Console.WriteLine($"- OSDescription={AppInfo.OSDescription}");
            Console.WriteLine();
        }

        public static void TestNetworkInfo()
        {

            Console.WriteLine("NetworkInfo:");
            Console.WriteLine($"- Primary (v4): {NetworkInfo.PrimaryHostname} ({NetworkInfo.PrimaryIPAddress})");
            Console.WriteLine($"- Primary (v6): {NetworkInfo.PrimaryHostname} ({NetworkInfo.PrimaryIPAddressV6})");

            var networkAddresses = NetworkInfo.NetworkAddresses;
            foreach (var networkAddress in networkAddresses)
            {
                Console.WriteLine($"- Address (v4): {networkAddress.Value} ({networkAddress.Key})");
            }

            var networkAddressesV6 = NetworkInfo.NetworkAddressesV6;
            foreach (var networkAddress in networkAddressesV6)
            {
                Console.WriteLine($"- Address (v6): {networkAddress.Value} ({networkAddress.Key})");
            }
            Console.WriteLine();
        }

        public static void OnExceptionOccured(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }

        public static void OnExceptionOccured2(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}
