using SneakRobber2.Shared;
using SneakRobber2.Utils;
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
    public class RpcClient
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

        public async Task StartAsync(string ip, int port)
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
        }

        public void Stop()
        {
            LogInfo("Stopping...");

            client.Close();
            netTask.Wait();
            sendQ.CompleteAdding();
            sendQ.Dispose();
            sendTask.Wait();

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
            EndPoint endPoint = client.Client.RemoteEndPoint;

            LogInfo($"Client {endPoint} connected");
            byte[] data = new byte[MaxLength];
            IFormatter formatter = new BinaryFormatter();

            while (true)
            {
                LogInfo($"Reading input from {endPoint}...");
                int len = stream.Read(data, 0, MaxLength);
                if (len == 0) break;
                System.Diagnostics.Debug.Assert(len < MaxLength);
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
                LogInfo($"Reading input from {endPoint} finished. Firing event finished.");
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
                        stream.Write(item, 0, item.Length);
                    }
                    catch
                    {
                        ClientSendFailed?.Invoke(this, new EventArg<EndPoint>(endPoint));
                    }
                }
            }
            catch (ObjectDisposedException) { }
            LogInfo("Sender stopped.");
        }

        protected virtual void OnReceivedData(EndPoint endPoint, string func, object[] ps) { }

        protected void LogInfo(object s)
        {
            Logger.LogInfo("RpcClient: " + s);
        }

        protected void LogWarning(object s)
        {
            Logger.LogWarning("RpcClient: " + s);
        }

        protected void LogError(object s)
        {
            Logger.LogError("RpcClient: " + s);
        }
    }
}
