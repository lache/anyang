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

        public void Disconnect()
        {
            try
            {
                _socket.Close();
            }
            catch
            { }
        }

        public object Source { get; set; }
        public bool ForDebug { get; set; }

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
                if (!e.IsDisconnected())
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

        public TPacket GetPacket<TPacket>(int timeout = 5000)
        {
            if (!ForDebug)
                throw new InvalidOperationException();

            var task = Task.Run(() =>
                {
                    while (true)
                    {
                        foreach (var msg in MessageQueue.Flush())
                        {
                            if (msg.GetType() == typeof(TPacket))
                                return msg;
                        }
                        Thread.Sleep(64);
                    }
                });
            task.Wait(timeout);
            return (TPacket)task.Result;
        }
    }

    public class Network : IDisposable
    {
        public event Action<Session> OnConnect;
        public event Action<Session> OnDisconnect;

        private readonly List<Socket> _sockets = new List<Socket>();
        private readonly List<Session> _connectedSessions = new List<Session>();

        public bool ForDebug { get; set; }
        private readonly ConcurrentQueue<Session> _debugConnectedSessions = new ConcurrentQueue<Session>();

        public Session GetConnectedConnection(int timeout = 2000)
        {
            if (!ForDebug)
                throw new InvalidOperationException();

            var task = Task.Run(() =>
            {
                Session outSession = null;
                _debugConnectedSessions.TryDequeue(out outSession);
                return outSession;
            });
            task.Wait(timeout);
            return task.Result;
        }

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
                    var session = new Session(clientSocket) { ForDebug = ForDebug };
                    ProcessClientSocket(session, /* fromServer = */ true);
                }
            }
            catch (Exception e)
            {
                if (!e.IsDisconnected())
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
            ProcessClientSocket(session, /* fromServer = */ false);
            return session;
        }

        private async void ProcessClientSocket(Session session, bool fromServer)
        {
            lock (_sockets)
            {
                _sockets.Add(session._socket);
            }

            if (OnConnect != null)
                OnConnect(session);

            lock (_connectedSessions)
            {
                _connectedSessions.Add(session);
            }

            if (fromServer && ForDebug)
            {
                lock (_debugConnectedSessions)
                    _debugConnectedSessions.Enqueue(session);
            }

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
                if (!e.IsDisconnected())
                    Logger.Write(e);
            }

            lock (_connectedSessions)
            {
                _connectedSessions.Add(session);
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

    public static class SocketHelper
    {
        public static bool IsDisconnected(this Exception exception)
        {
            if (exception is ObjectDisposedException)
                return true;

            if (!(exception is SocketException))
                return false;

            return IsDisconnected(exception as SocketException);
        }

        public static bool IsDisconnected(SocketException exception)
        {
            switch ((SocketError)exception.ErrorCode)
            {
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.Fault:
                case SocketError.NetworkReset:
                case SocketError.OperationAborted:
                case SocketError.Shutdown:
                case SocketError.SocketError:
                case SocketError.TimedOut:
                    return true;
            }
            return false;
        }
    }
}
