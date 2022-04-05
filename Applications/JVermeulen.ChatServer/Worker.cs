using JVermeulen.WebSockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.ChatServer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private Task Runner;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Runner = Task.Run(() => Execute(stoppingToken));
        }

        private void Execute(CancellationToken stoppingToken)
        {
            using (var server = new WsServer(WsEncoder.Text, "ws://localhost:8083", true))
            {
                server.OptionLogToConsole = true;
                server.OptionBroadcastMessages = true;

                server.StartAndWait(stoppingToken);
            }
        }
    }
}
