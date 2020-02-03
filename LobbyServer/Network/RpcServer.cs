using SneakRobber2.Shared;
using SneakRobber2.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SneakRobber2.Network
{
    public class RpcServer : IDisposable
    {
        public const int MaxLength = 1024 * 8;

        private Task netTask;
        private Dictionary<EndPoint, Task> netClientTasks;
        private ConcurrentDictionary<EndPoint, TcpClient> remoteClients;
        private TcpListener listener;
        private BlockingCollection<KeyValuePair<EndPoint, byte[]>> sendQ;  // TODO: Use multi sendQ for all clients
        private Task sendTask;

        public event EventHandler<EventArg<EndPoint>> ClientConnected;
        public event EventHandler<EventArg<EndPoint>> ClientDisconnected;
        public event EventHandler<EventArg<EndPoint>> ClientSendFailed;
        public event EventHandler<EventArgs<EndPoint, Invocation>> ReceivedData;

        public RpcServer()
        {
        }

        public void Start(int port)
        {
            LogInfo("Starting...");
            System.Diagnostics.Debug.Assert(listener == null);

            sendQ?.Dispose();
            sendQ = new BlockingCollection<KeyValuePair<EndPoint, byte[]>>();
            remoteClients = new ConcurrentDictionary<EndPoint, TcpClient>();
            netClientTasks = new Dictionary<EndPoint, Task>();  // NOTE: Reserved for future
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            sendTask = Task.Run(SenderRun);
            netTask = Task.Run(ListenerRun);
        }

        public void Stop()
        {
            LogInfo("Stopping...");

            listener.Stop();
            netTask.Wait();
            sendQ.CompleteAdding();
            sendQ.Dispose();
            foreach (var client in remoteClients)
            {
                try { client.Value.Close(); }
                catch { }
            }
            sendTask.Wait();
            listener = null;
            disposedValue = true;

            LogInfo("Successfully Stopped and Disposed");
        }

        public void Send(EndPoint endPoint, string func, params object[] ps)
        {
            IFormatter formatter = new BinaryFormatter();
            byte[] data;
            using (var serStream = new MemoryStream())
            {
                formatter.Serialize(serStream, func);
                formatter.Serialize(serStream, ps);
                data = serStream.ToArray();
            }
            System.Diagnostics.Debug.Assert(data.Length < MaxLength);
            sendQ.Add(new KeyValuePair<EndPoint, byte[]>(endPoint, data));
            //LogInfo($"Queued msg to {endPoint} for func {func}");
        }

        private void ListenerRun()
        {
            try
            {
                while (true)
                {
                    LogInfo($"Waiting for connection... Currently connected {remoteClients.Count}");
                    var client = listener.AcceptTcpClient();
                    remoteClients[client.Client.RemoteEndPoint] = client;
                    ClientConnected?.Invoke(this, new EventArg<EndPoint>(client.Client.RemoteEndPoint));
                    /*netClientTasks[client.Client.RemoteEndPoint] = */Task.Run(() => { ReaderRun(client); });
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            LogInfo("Listener Stopped");
        }

        private void ReaderRun(TcpClient client)
        {
            NetworkStream stream = null;
            EndPoint endPoint = client.Client.RemoteEndPoint;
            LogInfo($"Client {endPoint} connected");
            byte[] data = new byte[MaxLength];
            stream = client.GetStream();
            IFormatter formatter = new BinaryFormatter();

            while (true)
            {
                LogInfo($"Reading input from {endPoint}...");
                int len = 0;
                try
                {
                    len = stream.Read(data, 0, sizeof(int));
                }
                catch { }
                if (len == 0) break;
                int dataSize = BitConverter.ToInt32(data, 0);
                len = 0;
                int curLen = 0;
                while (len < dataSize)
                {
                    try
                    {
                        curLen = stream.Read(data, len, dataSize - len);
                        len += curLen;
                    }
                    catch { }
                    if (curLen == 0) break;
                }
                if (curLen == 0) break;
                System.Diagnostics.Debug.Assert(len == dataSize && dataSize < MaxLength);

                string func;
                object[] ps;
                using (var serStream = new MemoryStream(data))
                {
                    func = (string)formatter.Deserialize(serStream);
                    ps = (object[])formatter.Deserialize(serStream);
                }
                LogInfo($"Reading input from {endPoint} finished. Firing event...");

                OnReceivedData(endPoint, func, ps);
                ReceivedData?.Invoke(
                    this,
                    new EventArgs<EndPoint, Invocation>(
                        endPoint,
                        new Invocation
                        {
                            FuncName = func,
                            Parameters = ps
                        }
                    )
                );
                //LogInfo($"Reading input from {endPoint} finished. Firing event finished.");
            }
            LogInfo($"Client {endPoint} disconnected");
            remoteClients.TryRemove(endPoint, out var _);
            ClientDisconnected?.Invoke(this, new EventArg<EndPoint>(endPoint));
        }

        private void SenderRun()
        {
            LogInfo("Sender Running...");
            try
            {
                foreach (var item in sendQ.GetConsumingEnumerable())
                {
                    try
                    {
                        var stream = remoteClients[item.Key].GetStream();
                        stream.Write(BitConverter.GetBytes(item.Value.Length), 0, sizeof(int));
                        stream.Write(item.Value, 0, item.Value.Length);
                        //LogInfo($"Dequeued a msg and sent to {item.Key}");
                    }
                    catch
                    {
                        remoteClients.TryRemove(item.Key, out var _);
                        ClientSendFailed?.Invoke(this, new EventArg<EndPoint>(item.Key));
                    }
                }
            }
            catch (ObjectDisposedException) { }
            LogInfo("Sender stopped.");
        }

        protected virtual void OnReceivedData(EndPoint endPoint, string func, object[] ps) { }

        protected void LogInfo(object s)
        {
            Logger.LogInfo("RpcServer: " + s);
        }

        protected void LogWarning(object s)
        {
            Logger.LogWarning("RpcServer: " + s);
        }

        protected void LogError(object s)
        {
            Logger.LogError("RpcServer: " + s);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
