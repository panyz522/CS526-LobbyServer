using SneakRobber2.Network;
using SneakRobber2.Utils;
using System;
using System.Net;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LobbyUnitTest
{
    public class ServerClientFixture : IDisposable
    {
        public RpcServer server;
        public RpcClient client;

        public ServerClientFixture()
        {
            server = new RpcServer();
            client = new RpcClient();
            server.Start(10001);
        }

        public void Dispose()
        {
            server.Stop();
            client.Stop();
        }
    }

    public class Basic : IDisposable
    {
        private readonly ITestOutputHelper output;
        private readonly ServerClientFixture sc;

        public Basic(ITestOutputHelper output)
        {
            this.output = output;
            Logger.SetLogger((s) => output.WriteLine(s));
            sc = new ServerClientFixture();
        }

        [Fact]
        public void Connect()
        {
            sc.client.StartAsync("127.0.0.1", 10001).Wait();
        }

        [Fact]
        public void SendData()
        {
            using (var evt = new ManualResetEvent(false))
            {
                EndPoint ep = null ;
                sc.server.ClientConnected += (sender, e) =>
                {
                    Log($"Server connected to {e.Value}");
                    ep = e.Value;
                    evt.Set();
                };
                sc.server.ReceivedData += (sender, e) =>
                {
                    Log($"Server read from {e.V1} {e.V2.FuncName}, {e.V2.Parameters.Length}");
                    Assert.Equal("Test", e.V2.FuncName);
                    Assert.Equal(3, e.V2.Parameters.Length);
                    evt.Set();
                };
                sc.client.ReceivedData += (sender, e) =>
                {
                    Log($"Client read from {e.V1} {e.V2.FuncName}, {e.V2.Parameters.Length}");
                    Assert.Equal("Test1", e.V2.FuncName);
                    Assert.Equal(2, e.V2.Parameters.Length);
                    evt.Set();
                };

                sc.client.StartAsync("127.0.0.1", 10001).Wait();
                evt.WaitOne();
                evt.Reset();

                sc.client.Send("Test", new object[] { 1, 2, 3 });
                evt.WaitOne();
                evt.Reset();

                sc.server.Send(ep, "Test1", new object[] { 1, 2 });
                evt.WaitOne();
                evt.Reset();
            }
        }

        public void Dispose()
        {
            sc.Dispose();
        }
        
        private void Log(string s)
        {
            output.WriteLine("[Basic Test]: " + s);
        }
    }
}
