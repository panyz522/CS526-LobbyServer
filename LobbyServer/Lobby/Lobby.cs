using SneakRobber2.Network;
using SneakRobber2.Shared;
using SneakRobber2.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace SneakRobber2.Lobby
{
    public class Lobby : IDisposable
    {
        public const string LobbyRoomName = "Lobby";
        public const string StartCmd = @"c:\Users\Turnip\source\repos\CS526_SneakRobber\ServerBuild\SneakNight.exe";
        public object Lock { get; } = new object();

        private static Lobby instance;
        private Dictionary<EndPoint, PlayerData> players;
        private Dictionary<string, RoomData> inGameRooms;
        private NameGenerator nameGen;
        private string localIp;

        public RpcServerWithType<LobbyFEExecutor, IPlayerToLobby, ILobbyToPlayer> FrontendServer { get; private set; }

        public RpcServerWithType<LobbyBEExecutor, IRoomToLobby, ILobbyToRoom> BackendServer { get; private set; }

        public Lobby()
        {
            FrontendServer = new RpcServerWithType<LobbyFEExecutor, IPlayerToLobby, ILobbyToPlayer>();
            FrontendServer.ClientConnected += OnClientConnected;
            FrontendServer.ClientDisconnected += OnClientDisconnected;
            FrontendServer.ClientSendFailed += OnClientSendFailed;
            BackendServer = new RpcServerWithType<LobbyBEExecutor, IRoomToLobby, ILobbyToRoom>();
            players = new Dictionary<EndPoint, PlayerData>();
            inGameRooms = new Dictionary<string, RoomData>();
            nameGen = new NameGenerator();
            instance = this;
        }

        public void Start(int frontendPort, int backendPort, string localIp = "127.0.0.1")
        {
            LogInfo($"Lobby starting...");
            FrontendServer.Start(frontendPort);
            BackendServer.Start(backendPort);
            this.localIp = localIp;
            LogInfo($"Lobby started");
        }

        private void OnClientSendFailed(object sender, Utility.EventArg<EndPoint> e)
        {
            lock (Lock)
            {
                players.Remove(e.Value);
                LogInfo($"{nameof(OnClientSendFailed)} {e.Value}");
            }
        }

        private void OnClientConnected(object sender, Utility.EventArg<EndPoint> e)
        {
            lock (Lock)
            {
                LogInfo($"{nameof(OnClientConnected)} {e.Value}");
                var player = new PlayerData
                {
                    Name = nameGen.Next(),
                    IsReady = false,
                    Room = LobbyRoomName
                };
                var res = players.TryAdd(e.Value, player);
                Debug.Assert(res);

                FrontendServer.InvokeTo(e.Value).OnConnected(player.Name, player.Room);

                ForOthers(e.Value, (p) =>
                {
                    // Tell new player other players' info
                    FrontendServer.InvokeTo(e.Value).OnPlayerJoined(p.Value.Name, p.Value.Room);
                    // Tell other player new player's info
                    FrontendServer.InvokeTo(p.Key).OnPlayerJoined(player.Name, LobbyRoomName);
                });
            }
        }

        private void OnClientDisconnected(object sender, Utility.EventArg<EndPoint> e)
        {
            lock (Lock)
            {
                LogInfo($"{nameof(OnClientDisconnected)} {e.Value}");

                if (!players.ContainsKey(e.Value)) return;
                var player = players[e.Value];

                ForOthers(e.Value, (p) =>
                {
                    FrontendServer.InvokeTo(p.Key).OnPlayerLeaved(player.Name);
                });
                players.Remove(e.Value);

            }
        }

        private void ForOthers(EndPoint sender, Action<KeyValuePair<EndPoint, PlayerData>> action)
        {
            foreach (var p in players)
            {
                if (p.Key != sender)
                {
                    action(p);
                }
            }
        }

        private void ForAll(Action<KeyValuePair<EndPoint, PlayerData>> action)
        {
            foreach (var p in players)
            {
                action(p);
            }
        }

        private void ForOthersInRoom(EndPoint sender, string senderRoom, Action<KeyValuePair<EndPoint, PlayerData>> action)
        {
            foreach (var p in players)
            {
                if (senderRoom == p.Value.Room && p.Key != sender)
                {
                    action(p);
                }
            }
        }

        private void ForAllInRoom(string room, Action<KeyValuePair<EndPoint, PlayerData>> action)
        {
            foreach (var p in players)
            {
                if (room == p.Value.Room)
                {
                    action(p);
                }
            }
        }

        public class LobbyFEExecutor : IPlayerToLobby, IRpcContext
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void ChangeName(string name)
            {
                LogInfo($"{RemoteEndpoint} ChangeName to {name}");

                lock (instance.Lock)
                {
                    var player = instance.players[RemoteEndpoint];
                    if (player.Room != LobbyRoomName)
                    {
                        return; // Cannot change name in game rooms
                    }

                    var oldName = player.Name;
                    player.Name = name;

                    instance.ForOthers(RemoteEndpoint, (p) =>
                    {
                        instance.FrontendServer.InvokeTo(p.Key).OnPlayerChangeName(oldName, name);
                    });
                }
            }

            public void Exit()
            {
                LogInfo($"{RemoteEndpoint} exit");

                lock (instance.Lock)
                {
                    var player = instance.players[RemoteEndpoint];

                    instance.ForOthers(RemoteEndpoint, (p) =>
                    {
                        instance.FrontendServer.InvokeTo(p.Key).OnPlayerLeaved(player.Name);
                    });

                    instance.players.Remove(RemoteEndpoint);
                }
            }

            public void ChangeRoom(string room)
            {
                LogInfo($"{RemoteEndpoint} ChangeRoom to {room}");

                lock (instance.Lock)
                {
                    var player = instance.players[RemoteEndpoint];

                    var leavedRoom = player.Room;
                    player.Room = room;
                    player.IsReady = false;

                    instance.ForOthers(RemoteEndpoint, (p) =>
                    {
                        instance.FrontendServer.InvokeTo(p.Key).OnPlayerChangeRoom(player.Name, room);
                    });

                    instance.ForOthersInRoom(RemoteEndpoint, room, (p) =>
                    {
                        if (p.Value.IsReady)
                        {
                            instance.FrontendServer.InvokeTo(RemoteEndpoint).OnPlayerPrepared(p.Value.Name);
                        }
                    });
                }
            }

            public void Prepare()
            {
                LogInfo($"{RemoteEndpoint} Prepare");

                lock (instance.Lock)
                {
                    var player = instance.players[RemoteEndpoint];
                    if (player.Room == LobbyRoomName)
                    {
                        return; // Cannot prepare in lobby
                    }

                    player.IsReady = true;

                    int nPlayers = 1;  // Self
                    bool prepared = true;
                    instance.ForOthersInRoom(RemoteEndpoint, player.Room, (p) =>
                    {
                        instance.FrontendServer.InvokeTo(p.Key).OnPlayerPrepared(player.Name);
                        nPlayers++;
                        prepared &= p.Value.IsReady;
                    });
                    if (nPlayers == 3 && prepared)
                    {
                        // Start Game
                        LogInfo("===== Game Init =====");
                        instance.inGameRooms[player.Room] = new RoomData();

                        ProcessStartInfo pInfo = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            WindowStyle = ProcessWindowStyle.Normal,
                            FileName = StartCmd,
                            Arguments = "-rname " + player.Room
                        };
                        Process.Start(pInfo);
                    }
                }
            }

            public void Unprepare()
            {
                LogInfo($"{RemoteEndpoint} Unprepare");

                lock (instance.Lock)
                {
                    var player = instance.players[RemoteEndpoint];

                    player.IsReady = false;

                    instance.ForOthersInRoom(RemoteEndpoint, player.Room, (p) =>
                    {
                        instance.FrontendServer.InvokeTo(p.Key).OnPlayerUnprepared(player.Name);
                    });
                }
            }
        }

        public class LobbyBEExecutor : IRoomToLobby, IRpcContext
        {
            public EndPoint RemoteEndpoint { get; set; }

            public void GameInterrupted(string roomName, int code)
            {
                LogWarning($"{RemoteEndpoint} {roomName} GameInterrupted {code}");
                lock (instance.Lock)
                {
                    instance.ForAllInRoom(roomName, (p) =>
                    {
                        p.Value.IsReady = false;
                    });
                    instance.inGameRooms.Remove(roomName);
                }
            }

            public void GameOver(string roomName, int winner)
            {
                LogInfo($"{RemoteEndpoint} {roomName} GameOver, winner {winner}");
                lock (instance.Lock)
                {
                    instance.ForAllInRoom(roomName, (p) =>
                    {
                        p.Value.IsReady = false;
                    });
                    instance.inGameRooms.Remove(roomName);
                }
            }

            public void GameReady(string name, int port, int tokenBase)
            {
                LogInfo($"{RemoteEndpoint} GameReady, room {name} at {port}, token: {tokenBase}");
                lock (instance.Lock)
                {
                    var room = instance.inGameRooms[name];
                    room.Port = port;
                    room.Token = tokenBase;
                    var players = new List<KeyValuePair<EndPoint, PlayerData>>();
                    instance.ForAllInRoom(name, (p) =>
                    {
                        players.Add(p);
                    });
                    Debug.Assert(players.Count == 3);
                    var playerNames = players.Select((p) => p.Value.Name).ToArray();

                    var token = tokenBase;
                    foreach (var player in players)
                    {
                        instance.FrontendServer.InvokeTo(player.Key).OnGameStarted(instance.localIp, port, playerNames, token++);
                    }
                }
            }
        }

        protected static void LogInfo(object s)
        {
            Logger.LogInfo("Lobby: " + s);
        }

        protected static void LogWarning(object s)
        {
            Logger.LogWarning("Lobby: " + s);
        }

        protected static void LogError(object s)
        {
            Logger.LogError("Lobby: " + s);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FrontendServer.Dispose();
                    FrontendServer.ClientConnected -= OnClientConnected;
                    FrontendServer.ClientDisconnected -= OnClientDisconnected;
                    FrontendServer.ClientSendFailed -= OnClientSendFailed;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
