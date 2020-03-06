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
using System.Text;
using System.Threading.Tasks;

namespace SneakRobber2.Network
{
    public class RpcClient : IDisposable
    {

        public const int MaxLength = 1024 * 8;

        private Task netTask;
        private TcpClient client;
        private NetworkStream stream;
        private BlockingCollection<byte[]> sendQ;
        private Task sendTask;

        public event EventHandler<EventArg<EndPoint>> ClientSendFailed;
        public event EventHandler<EventArgs<EndPoint, Invocation>> ReceivedData;

        public RpcClient()
        {
        }

        public async Task<int> StartAsync(string ip, int port)
        {
            LogInfo("Starting...");
            System.Diagnostics.Debug.Assert(client == null);

            sendQ?.Dispose();
            sendQ = new BlockingCollection<byte[]>();
            client = new TcpClient();
            await client.ConnectAsync(ip, port);
            stream = client.GetStream();
            netTask = Task.Run(ReaderRun);
            sendTask = Task.Run(SenderRun);
            return ((IPEndPoint)client.Client.LocalEndPoint).Port;
        }

        public int Start(string ip, int port)
        {
            LogInfo("Starting...");
            System.Diagnostics.Debug.Assert(client == null);

            sendQ?.Dispose();
            sendQ = new BlockingCollection<byte[]>();
            client = new TcpClient();
            client.Connect(ip, port);
            stream = client.GetStream();
            netTask = Task.Run(ReaderRun);
            sendTask = Task.Run(SenderRun);
            return ((IPEndPoint)client.Client.LocalEndPoint).Port;
        }

        public void Stop()
        {
            LogInfo("Stopping...");

            client.Close();
            netTask.Wait();
            sendQ.CompleteAdding();
            sendQ.Dispose();
            sendTask.Wait();
            disposedValue = true;

            LogInfo("Successfully Stopped and Disposed");
        }

        public void Send(string func, params object[] ps)
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
            sendQ.Add(data);
        }

        private void ReaderRun()
        {
            EndPoint endPoint;
            try
            {
                endPoint = client.Client.RemoteEndPoint;
            }
            catch
            {
                LogWarning("Client stopped while starting reader.");
                return;
            }

            LogInfo($"Client {endPoint} connected");
            byte[] data = new byte[MaxLength];
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
                LogInfo($"Reading input from {endPoint} finished. Firing event for {func}...");

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
                //LogInfo($"Reading input from {endPoint} finished. Firing event for {func} finished.");
            }
            LogInfo($"Client {endPoint} disconnected");
        }

        private void SenderRun()
        {
            LogInfo("Sender Running...");
            try
            {
                EndPoint endPoint = client.Client.RemoteEndPoint;
                foreach (var item in sendQ.GetConsumingEnumerable())
                {
                    try
                    {
                        stream.Write(BitConverter.GetBytes(item.Length), 0, sizeof(int));
                        stream.Write(item, 0, item.Length);
                    }
                    catch
                    {
                        ClientSendFailed?.Invoke(this, new EventArg<EndPoint>(endPoint));
                    }
                }
            }
            catch (Exception) { }
            LogInfo("Sender stopped.");
        }

        protected virtual void OnReceivedData(EndPoint endPoint, string func, object[] ps) { }

        protected static Logger<RpcClient> Logger => Logger<RpcClient>.Instance;

        protected void LogInfo(object s)
        {
            Logger.LogInfo(s);
        }

        protected void LogWarning(object s)
        {
            Logger.LogWarning(s);
        }

        protected void LogError(object s)
        {
            Logger.LogError(s);
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
