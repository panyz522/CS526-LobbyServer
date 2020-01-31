using SneakRobber2.Network;
using SneakRobber2.Shared;
using SneakRobber2.Utils;
using System;
using System.Net;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace LobbyUnitTest
{
    public class ServerClientPair<TServer, TClient> : IDisposable
        where TServer : RpcServer, new()
        where TClient : RpcClient, new()
    {
        public TServer server;
        public TClient client;

        public ServerClientPair()
        {
            server = new TServer();
            client = new TClient();
        }

        public void Start()
        {
            server.Start(10001);
            client.StartAsync("127.0.0.1", 10001).Wait();
        }

        public void Dispose()
        {
            server.Stop();
            client.Stop();
        }
    }

    public class Basic
    {
        private readonly ITestOutputHelper output;
        private static Basic instance;

        public Basic(ITestOutputHelper output)
        {
            this.output = output;
            Basic.instance = this;
            Logger.SetLogger((s) => output.WriteLine(s));
        }

        [Fact]
        public void Connect()
        {
            using var sc = new ServerClientPair<RpcServer, RpcClient>();
            sc.Start();
        }

        [Fact]
        public void SendData()
        {
            using var sc = new ServerClientPair<RpcServer, RpcClient>();
            using var evt = new ManualResetEvent(false);
            EndPoint ep = null;
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

            sc.Start();
            evt.WaitOne();
            evt.Reset();

            sc.client.Send("Test", new object[] { 1, 2, 3 });
            evt.WaitOne();
            evt.Reset();

            sc.server.Send(ep, "Test1", new object[] { 1, 2 });
            evt.WaitOne();
            evt.Reset();
        }

        private ManualResetEvent evt;
        [Fact]
        public void TypedSend()
        {
            using var sc = new ServerClientPair<RpcServerForLobbyToPlayer<LobbyExecutor>, RpcClientForPlayerToLobby<PlayerExecutor>>();
            using var evt = new ManualResetEvent(false);
            this.evt = evt;
            EndPoint ep = null;
            sc.server.ClientConnected += (sender, e) =>
            {
                Log($"Server connected to {e.Value}");
                ep = e.Value;
                evt.Set();
            };

            sc.Start();
            evt.WaitOne();
            evt.Reset();

            sc.client.Invoker.JoinRoom("Lobby");
            evt.WaitOne();
            evt.Reset();

            sc.server.InvokeTo(ep).OnPlayerJoined("Player1", "Lobby");
            evt.WaitOne();
            evt.Reset();
        }

        public class LobbyExecutor : IPlayerToLobby, IRpcData
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void Exit()
            {
                Log();
            }

            public void JoinRoom(string room)
            {
                Log(room); 
                instance.evt?.Set(); 
            }

            public void Prepare()
            {
                Log();
            }

            public void Unprepare()
            {
                Log();
            }

            private void Log(string ps = "", [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            {
                instance.Log($"Server RPC called: {memberName}({ps})");
            }
        }

        public class PlayerExecutor : ILobbyToPlayer, IRpcData
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void OnGameStarted(string ip, int port, string[] players)
            {
                Log();
            }

            public void OnGameStartFailed(string err)
            {
                Log();
            }

            public void OnPlayerJoined(string name, string room)
            {
                Log();
                instance.evt.Set();
            }

            public void OnPlayerLeaved(string name, string room)
            {
                Log();
            }

            public void OnPlayerPrepared(string name)
            {
                Log();
            }

            public void OnPlayerUnprepared(string name)
            {
                Log();
            }

            private void Log([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            {
                instance.Log("Server RPC called: " + memberName);
            }
        }

        private void Log(string s)
        {
            output.WriteLine("[Basic Test]: " + s);
        }
    }
}
