using JVermeulen.App;
using System;

namespace JVermeulen.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var networkAddresses = NetworkInfo.NetworkAddresses;

            Console.WriteLine("NetworkInfo:");
            foreach(var networkAddress in networkAddresses)
            {
                Console.WriteLine($"- Address: {networkAddress.Value} ({networkAddress.Key})");
            }
            Console.WriteLine();

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
            Console.WriteLine($"- RunsSTA={AppInfo.RunsSTA}");
            Console.WriteLine($"- HasUI={AppInfo.HasUI}");
            Console.WriteLine($"- Culture={AppInfo.Culture}");
            Console.WriteLine($"- IsDebug={AppInfo.IsDebug}");

            Console.WriteLine();
        }
    }
}
