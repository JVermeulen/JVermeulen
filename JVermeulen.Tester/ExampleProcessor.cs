using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    public class ExampleProcessor : Processor<object>
    {
        public ExampleProcessor() : base()
        {
            EnableHeartbeat(TimeSpan.FromSeconds(1), false);
        }

        public override void OnExceptionOccured(Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        public override void OnHeartbeat(long count)
        {
            Console.WriteLine($"Hearbeat ({count}) Pending values {NumberOfValuesPending}");
        }

        public override void OnStarting()
        {
            Console.WriteLine("Starting");

            Task.Delay(1000).Wait();
        }

        public override void OnStarted()
        {
            Console.WriteLine("Started");
        }

        public override void OnStopping()
        {
            Console.WriteLine("Stopping");
        }

        public override void OnStopped()
        {
            Console.WriteLine($"Number of values processed: {NumberOfValuesProcessed}");
            Console.WriteLine($"Number of values pending: {NumberOfValuesPending}");
            Console.WriteLine("Stopped");
        }

        public override void OnReceived(object value)
        {
            if (value is string text)
                Console.WriteLine($"Value: {text}");
            else if (value is int delay)
            {
                Console.WriteLine("Value process started");
                Task.Delay(delay).Wait();
                Console.WriteLine("Value process stopped");
            }
        }
    }
}
