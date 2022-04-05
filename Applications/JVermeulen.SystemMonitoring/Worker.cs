using JVermeulen.Monitoring.Influx;
using JVermeulen.WebSockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.SystemMonitoring
{
#pragma warning disable CA1416 // Validate platform compatibility
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private WsClient Monitoring { get; set; }
        private PerformanceCounter ProcessorMonitor { get; set; }
        private PerformanceCounter MemoryMonitor { get; set; }
        private float TotalMemory { get; set; }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Monitoring = new WsClient(WsEncoder.Text, "http://grafana.local:3000/api/live/push/test", true);
            Monitoring.OptionRequestHeader = new Tuple<string, string>("Authorization", "Bearer ==");
            Monitoring.Start();

            TotalMemory = GetTotalMemory();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (NextMetrics(out int cpu, out int ram))
                {
                    var measurement = new InfluxMeasurement("system");
                    measurement.Fields.Add("CPU", cpu);
                    measurement.Fields.Add("Memory", ram);

                    var message = measurement.ToString();

                    _logger.LogInformation($"[{DateTimeOffset.Now}] {message}");

                    Monitoring.Send(message);
                }


                await Task.Delay(1000, stoppingToken);
            }
        }

        private bool NextMetrics(out int processor, out int memory)
        {
            processor = 0;
            memory = 0;

            if (ProcessorMonitor == null)
            {
                ProcessorMonitor = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", true);
                MemoryMonitor = new PerformanceCounter("Memory", "Available MBytes");

                return false;
            }
            else
            {
                processor = (int)(ProcessorMonitor.NextValue());

                if (processor > 100)
                    processor = 100;

                memory = (int)((TotalMemory - MemoryMonitor.NextValue()) / TotalMemory * 100);

                return true;
            }
        }

        private float GetTotalMemory()
        {
            var searcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");
            UInt64 total = 0;

            foreach (ManagementObject ram in searcher.Get())
            {
                total += (UInt64)ram.GetPropertyValue("Capacity");
            }

            return total / 1024 / 1024;
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}
