using SneakRobber2.Lobby;
using SneakRobber2.Network;
using SneakRobber2.Shared;
using SneakRobber2.Utility;
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
            server.Dispose();
            client.Dispose();
        }
    }

    public class Basic
    {
        public const string LocalHost = "127.0.0.1";

        private readonly ITestOutputHelper output;
        private static Basic instance;

        public Basic(ITestOutputHelper output)
        {
            this.output = output;
            Basic.instance = this;
            Logger.SetLogger((s) => output.WriteLine(s.ToString()));
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
            using var sc = new ServerClientPair<RpcServerWithType<LobbyExecutor, IPlayerToLobby, ILobbyToPlayer>, RpcClientWithType<SimplePlayerExecutor, ILobbyToPlayer, IPlayerToLobby>>();
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

            sc.client.Invoker.ChangeRoom("Lobby");
            evt.WaitOne();
            evt.Reset();

            sc.server.InvokeTo(ep).OnPlayerJoined("Player1", "Lobby", 0);
            evt.WaitOne();
            evt.Reset();
        }

        [Fact]
        public void LobbyConnect()
        {
            using Lobby lobby = new Lobby();
            using RpcClientWithType<EmptyPlayerExecutor, ILobbyToPlayer, IPlayerToLobby> 
                player1 = new RpcClientWithType<EmptyPlayerExecutor, ILobbyToPlayer, IPlayerToLobby>(),
                player2 = new RpcClientWithType<EmptyPlayerExecutor, ILobbyToPlayer, IPlayerToLobby>();
            lobby.Start(10001, 10002);
            player1.StartAsync(LocalHost, 10001).Wait();
            player2.StartAsync(LocalHost, 10001).Wait();
        }

        [Fact]
        public void LobbyRun()
        {
            using Lobby lobby = new Lobby();
            using RpcClientWithType<PlayerExecutor, ILobbyToPlayer, IPlayerToLobby>
                player1 = new RpcClientWithType<PlayerExecutor, ILobbyToPlayer, IPlayerToLobby>(),
                player2 = new RpcClientWithType<PlayerExecutor, ILobbyToPlayer, IPlayerToLobby>();
            using var evt = new ManualResetEvent(false);
            this.evt = evt;

            lobby.Start(10001, 10002);
            player1.StartAsync(LocalHost, 10001).Wait();
            evt.WaitOne();
            evt.Reset();
            player2.StartAsync(LocalHost, 10001).Wait();
            evt.WaitOne();
            evt.Reset();
        }

        public class LobbyExecutor : IPlayerToLobby, IRpcContext
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void Exit()
            {
                Log();
            }

            public void ChangeRoom(string room)
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

            public void ChangeName(string name)
            {
                Log();
            }
        }

        public class SimplePlayerExecutor : ILobbyToPlayer, IRpcContext
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void OnConnected(string givenName, string joinedRoom)
            {
                Log();
            }

            public void OnGameStarted(string ip, int port, string[] players, int token)
            {
                Log();
            }

            public void OnGameStartFailed(string err)
            {
                Log();
            }

            public void OnPlayerChangeName(string oldName, string newName)
            {
                throw new NotImplementedException();
            }

            public void OnPlayerChangeRoom(string name, string room, int role)
            {
                throw new NotImplementedException();
            }

            public void OnPlayerJoined(string name, string room, int role)
            {
                Log();
                instance.evt.Set();
            }

            public void OnPlayerLeaved(string name)
            {
                throw new NotImplementedException();
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

        public class EmptyPlayerExecutor : ILobbyToPlayer, IRpcContext
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void OnConnected(string givenName, string joinedRoom)
            {
                Log();
            }

            public void OnGameStarted(string ip, int port, string[] players, int token)
            {
                Log();
            }

            public void OnGameStartFailed(string err)
            {
                Log();
            }

            public void OnPlayerChangeName(string oldName, string newName)
            {
                Log();
            }

            public void OnPlayerChangeRoom(string name, string room, int role)
            {
                Log();
            }

            public void OnPlayerJoined(string name, string room, int role)
            {
                Log();
            }

            public void OnPlayerLeaved(string name)
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

        public class PlayerExecutor : ILobbyToPlayer, IRpcContext
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void OnConnected(string givenName, string joinedRoom)
            {
                Basic.instance.evt.Set();
                Log(givenName);
            }

            public void OnGameStarted(string ip, int port, string[] players, int token)
            {
                Log();
            }

            public void OnGameStartFailed(string err)
            {
                Log();
            }

            public void OnPlayerChangeName(string oldName, string newName)
            {
                Log();
            }

            public void OnPlayerChangeRoom(string name, string room, int role)
            {
                Log();
            }

            public void OnPlayerJoined(string name, string room, int role)
            {
                Log();
            }

            public void OnPlayerLeaved(string name)
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
