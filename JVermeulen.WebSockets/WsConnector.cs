using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsConnector : Session
    {
        public HttpListener Listener { get; set; }
        
        public TimeSpan OptionKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
        public EventHandler<WebSocket> ClientConnected;

        public WsConnector(string url)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add(url);
        }

        protected override void OnStarting()
        {
            Listener.Start();
        }

        protected override void OnStarted()
        {
            StartAsync().ConfigureAwait(false);
        }

        private async Task StartAsync()
        {
            try
            {
                while (Status == SessionStatus.Started)
                {
                    var context = await Listener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null, OptionKeepAliveInterval);

                        ClientConnected?.Invoke(this, wsContext.WebSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (HttpListenerException hlex)
            {
                if (hlex.ErrorCode != 995)
                    throw;
            }
            catch
            {
                throw;
            }
        }

        protected override void OnStopping()
        {
            Listener.Stop();
        }
    }
}
