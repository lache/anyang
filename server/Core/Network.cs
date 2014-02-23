using Server.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Core
{
    public class FlushableMessageQueue
    {
        private ConcurrentBag<IMessage> _messageBag = new ConcurrentBag<IMessage>();

        public void Add(IMessage message)
        {
            _messageBag.Add(message);
        }

        public IEnumerable<IMessage> Flush()
        {
            if (_messageBag.IsEmpty)
                return Enumerable.Empty<IMessage>();

            var oldBag = Interlocked.Exchange(ref _messageBag, new ConcurrentBag<IMessage>());
            return oldBag.ToArray();
        }
    }

    public class Session
    {
        public readonly FlushableMessageQueue MessageQueue = new FlushableMessageQueue();
        internal readonly Socket _socket;

        internal Session(Socket socket)
        {
            _socket = socket;
        }

        public object Source { get; set; }

        public void Send(IMessage message)
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0);
                message.WriteTo(writer);

                bytes = stream.ToArray();
            }

            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            Array.Copy(lengthBytes, bytes, lengthBytes.Length);
            try
            {
                _socket.Send(bytes);
            }
            catch (Exception e)
            {
                Logger.Write(e);
            }
        }

        public async Task<IMessage> Receive()
        {
            var sizeBytes = await _socket.ReceiveAsync(sizeof(int)).ConfigureAwait(false);
            var size = BitConverter.ToInt32(sizeBytes, 0);

            var bytes = await _socket.ReceiveAsync(size - sizeof(int)).ConfigureAwait(false);
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                var typeId = reader.ReadInt32();
                var message = MessageFactory.Create(typeId);
                message.ReadFrom(reader);
                return message;
            }
        }
    }

    public class Network : IDisposable
    {
        public event Action<Session> OnConnect;
        public event Action<Session> OnDisconnect;

        private readonly List<Socket> _sockets = new List<Socket>();
        public async void StartServer(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            lock (_sockets)
            {
                _sockets.Add(socket);
            }

            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(16);
            try
            {
                while (true)
                {
                    var clientSocket = await socket.AcceptAsync().ConfigureAwait(false);
                    var session = new Session(clientSocket);
                    ProcessClientSocket(session);
                }
            }
            catch (Exception e)
            {
                Logger.Write(e);
            }

            lock (_sockets)
            {
                _sockets.Remove(socket);
            }
        }

        public Session Connect(string host, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(host, port);

            var session = new Session(socket);
            ProcessClientSocket(session);
            return session;
        }

        private async void ProcessClientSocket(Session session)
        {
            lock (_sockets)
            {
                _sockets.Add(session._socket);
            }

            if (OnConnect != null)
                OnConnect(session);

            try
            {
                while (true)
                {
                    var message = await session.Receive().ConfigureAwait(false);
                    session.MessageQueue.Add(message);
                }
            }
            catch (Exception e)
            {
                Logger.Write(e);
            }

            if (OnDisconnect != null)
                OnDisconnect(session);

            lock (_sockets)
            {
                _sockets.Remove(session._socket);
            }
        }

        public void Dispose()
        {
            lock (_sockets)
            {
                foreach (var socket in _sockets)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch {}
                }
                _sockets.Clear();
            }
        }
    }
}
