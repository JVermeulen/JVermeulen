using JVermeulen.App;
using JVermeulen.Processing;
using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var networkAddresses = NetworkInfo.NetworkAddresses;

            Console.WriteLine("NetworkInfo:");
            foreach (var networkAddress in networkAddresses)
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
            Console.WriteLine($"- HasUI={AppInfo.HasUI}");
            Console.WriteLine($"- HasWindow={AppInfo.HasWindow}");
            Console.WriteLine($"- Type={AppInfo.Type}");
            Console.WriteLine($"- Culture={AppInfo.Culture}");
            Console.WriteLine($"- IsDebug={AppInfo.IsDebug}");
            Console.WriteLine($"- OSFriendlyName={AppInfo.OSFriendlyName}");
            Console.WriteLine($"- OSDescription={AppInfo.OSDescription}");
            Console.WriteLine();

            //using (var heart = new HeartbeatGenerator(TimeSpan.FromSeconds(1)))
            //{
            //    heart.OnReceive.Subscribe((h) => Console.WriteLine(h.Id.ToString()));
            //    heart.Start();

            //    Task.Delay(10000).Wait();
            //}

            using (var processor = new ExampleProcessor())
            {
                processor.Start();

                Task.Delay(2000).Wait();

                processor.Add(2000);
                processor.Add(2000);
                processor.Add(2000);
                processor.Add(2000);
                processor.Add(2000);

                Task.Delay(5000).Wait();

                processor.Stop();

                Task.Delay(10000).Wait();
            }
        }
    }
}
