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
        public const string StartCmd = @"c:\Users\Turnip\source\repos\CS526_SneakRobber\ServerBuild\SneakNight_server.exe";
        //public const string StartCmd = @"C:\Users\panyz522\Desktop\ServerBuild\SneakNight.exe";
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
                    Room = LobbyRoomName,
                    Role = 0 // Lobby dose not care about player's role
                };
                var res = players.TryAdd(e.Value, player);
                Debug.Assert(res);

                FrontendServer.InvokeTo(e.Value).OnConnected(player.Name, player.Room);

                ForOthers(e.Value, (p) =>
                {
                    // Tell new player other players' info
                    FrontendServer.InvokeTo(e.Value).OnPlayerJoined(p.Value.Name, p.Value.Room, p.Value.Role);
                    // Tell other player new player's info
                    FrontendServer.InvokeTo(p.Key).OnPlayerJoined(player.Name, LobbyRoomName, player.Role);
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

                    // Update player's role in the new room
                    if (room == LobbyRoomName)
                        player.Role = 0;
                    else
                    {
                        bool[] usedRoles = new bool[3];
                        instance.ForOthersInRoom(RemoteEndpoint, room, (p) =>
                        {
                            Debug.Assert(!usedRoles[p.Value.Role], $"{room} contains repeated role {p.Value.Role}");
                            usedRoles[p.Value.Role] = true;
                        });
                        int unusedRole = Array.IndexOf(usedRoles, false);
                        Debug.Assert(unusedRole >= 0, $"{room} is full");
                        player.Role = unusedRole;
                    }

                    // Notify others the player has changed to another room
                    instance.ForAll((p) =>
                    {
                        instance.FrontendServer.InvokeTo(p.Key).OnPlayerChangeRoom(player.Name, room, player.Role);
                    });

                    // Notify the new player in room with other players' status
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
                    var players = new List<KeyValuePair<EndPoint, PlayerData>>();
                    instance.ForAllInRoom(name, (p) =>
                    {
                        players.Add(p);
                    });
                    Debug.Assert(players.Count == 3);

                    var playerNames = new string[3];
                    foreach (var player in players)
                    {
                        Debug.Assert(playerNames[player.Value.Role] == null, $"Duplicated role found {player.Value.Role} for player {player.Value.Name}");
                        playerNames[player.Value.Role] = player.Value.Name;
                    }
                    var room = instance.inGameRooms[name];
                    room.Port = port;
                    room.Token = tokenBase;
                    room.Players = playerNames;

                    foreach (var player in players)
                    {
                        instance.FrontendServer.InvokeTo(player.Key).OnGameStarted(instance.localIp, port, playerNames, tokenBase);
                    }
                }
            }
            public static void Shuffle<T>(T[] array)
            {
                int n = array.Length;
                var rng = new Random();
                while (n > 1)
                {
                    int k = rng.Next(n--);
                    T temp = array[n];
                    array[n] = array[k];
                    array[k] = temp;
                }
            }
        }

        protected static Logger<Lobby> Logger => Logger<Lobby>.Instance;

        protected static void LogInfo(object s)
        {
            Logger.LogInfo(s);
        }

        protected static void LogWarning(object s)
        {
            Logger.LogWarning(s);
        }

        protected static void LogError(object s)
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
                    FrontendServer.ClientConnected -= OnClientConnected;
                    FrontendServer.ClientDisconnected -= OnClientDisconnected;
                    FrontendServer.ClientSendFailed -= OnClientSendFailed;
                    FrontendServer.Dispose();
                    BackendServer.Dispose();
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
