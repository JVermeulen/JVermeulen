//using JVermeulen.Processing;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;
//using System.Threading.Tasks;

//namespace JVermeulen.TCP
//{
//    public class TcpClient<T> : HeartbeatSession
//    {
//        private Socket Socket { get; set; }
//        public List<TcpSession<T>> Sessions { get; set; }
//        public List<TcpSession<T>> ConnectedSessions => Sessions.Where(s => s.Socket.Connected).ToList();
//        public long NumberOfBytesReceived => (long)Sessions.Sum(s => s.NumberOfBytesReceived.Value);
//        public long NumberOfBytesSent => (long)Sessions.Sum(s => s.NumberOfBytesSent.Value);
//        public long NumberOfMessagesReceived => (long)Sessions.Sum(s => s.NumberOfMessagesReceived.Value);
//        public long NumberOfMessagesSent => (long)Sessions.Sum(s => s.NumberOfMessagesSent.Value);
//        public long NumberOfReconnects { get; private set; }

//        private IPEndPoint LocalEndPoint { get; set; }
//        private IPEndPoint RemoteEndPoint { get; set; }
//        public string ServerAddress { get; private set; }
//        public string ClientAddress { get; private set; }
//        public ITcpEncoder<T> Encoder { get; private set; }

//        public EventHandler<TcpClient<T>> ClientStarted;
//        public EventHandler<TcpClient<T>> ClientStopped;
//        public EventHandler<TcpSession<T>> SessionStarted;
//        public EventHandler<TcpSession<T>> SessionStopped;
//        public EventHandler<T> MessageReceived;
//        public EventHandler<T> MessageSent;

//        private readonly ManualResetEvent ConnectSignal = new ManualResetEvent(false);

//        public TcpClient(ITcpEncoder<T> encoder, string address, int port) : this(encoder, new IPEndPoint(IPAddress.Parse(address), port))
//        {
//            //
//        }

//        public TcpClient(ITcpEncoder<T> encoder, IPEndPoint remoteEndPoint) : base(TimeSpan.FromSeconds(5))
//        {
//            RemoteEndPoint = remoteEndPoint;
//            ServerAddress = RemoteEndPoint.ToString();

//            Encoder = encoder;

//            NumberOfReconnects = -1;
//            Sessions = new List<TcpSession<T>>();
//        }

//        public override void OnStarting()
//        {

//            ClientStarted?.Invoke(this, this);

//            Reset();

//            ConnectFireAndForget();
//        }

//        public override void OnStopping()
//        {
//            ConnectSignal.Set();

//            ConnectedSessions.ForEach(s => s.Stop());

//            ClientStopped?.Invoke(this, this);
//        }

//        private void Reset()
//        {
//            Socket = new Socket(RemoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//            ConnectSignal.Set();
//        }

//        private void ConnectFireAndForget()
//        {
//            Task.Run(() => Connect()).ConfigureAwait(false);
//        }

//        private void Connect()
//        {
//            while (Status == SessionStatus.Started)
//            {
//                ConnectSignal.Reset();

//                Socket.BeginConnect(RemoteEndPoint, new AsyncCallback(ConnectCallback), Socket);

//                ConnectSignal.WaitOne();
//            }
//        }

//        private void ConnectCallback(IAsyncResult ar)
//        {
//            try
//            {
//                Socket.EndConnect(ar);

//                NumberOfReconnects++;

//                var session = new TcpSession<T>(Socket, false, Encoder);
//                //session.SessionStopped += (sender, e) => SessionStopped?.Invoke(sender, e);
//                session.MessageReceived += (sender, e) => MessageReceived?.Invoke(sender, e);
//                session.MessageSent += (sender, e) => MessageSent?.Invoke(sender, e);
//                //session.SessionStopped += (sender, e) => { Reset(); };

//                SessionStarted?.Invoke(this, session);

//                Sessions.Add(session);
//            }
//            catch
//            {
//                if (Status == SessionStatus.Started && !Socket.Connected)
//                {
//                    // Client is started but connection could not be made.
//                    Task.Delay(1000).Wait();

//                    ConnectSignal.Set();
//                }
//            }
//        }

//        public void Send(T content)
//        {
//            ConnectedSessions.ForEach(s => s.Write(content));
//        }

//        public override string ToString()
//        {
//            return $"TCP Server {ServerAddress}";
//        }
//    }
//}
