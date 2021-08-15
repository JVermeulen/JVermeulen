using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    public class ExampleProcessor : Processor
    {
        public ExampleProcessor() : base()
        {
            EnableHeartbeat(TimeSpan.FromSeconds(1), false);
        }

        public override void OnExceptionOccured(Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        public override void OnHeartbeat(Heartbeat heartbeat)
        {
            Console.WriteLine($"Hearbeat ({heartbeat.Id}) Pending tasks {NumberOfPendingTasks}");
        }

        public override void OnStarted()
        {
            Console.WriteLine("Started");
        }

        public override void OnStopped()
        {
            Console.WriteLine("Stopped");
        }

        public override void OnTask(object value)
        {
            if (value is string text)
                Console.WriteLine($"Work: {text}");
            else if (value is int delay)
            {
                Console.WriteLine("Delay started");
                Task.Delay(delay).Wait();
                Console.WriteLine("Delay stopped");
            }
        }

        public override void OnFinished()
        {
            Console.WriteLine("Finished");
        }
    }
}
