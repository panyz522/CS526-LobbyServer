using SneakRobber2.Network;
using SneakRobber2.Shared;
using SneakRobber2.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SneakRobber2.Lobby
{
    public class Lobby : IDisposable
    {
        public const string LobbyRoomName = "Lobby";
        public object Lock { get; } = new object();

        private static Lobby instance;
        private Dictionary<EndPoint, PlayerData> players;
        private NameGenerator nameGen;

        public RpcServerWithType<LobbyExecutor, IPlayerToLobby, ILobbyToPlayer> Server { get; private set; }

        public Lobby()
        {
            Server = new RpcServerWithType<LobbyExecutor, IPlayerToLobby, ILobbyToPlayer>();
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            Server.ClientSendFailed += OnClientSendFailed;
            players = new Dictionary<EndPoint, PlayerData>();
            nameGen = new NameGenerator();
            instance = this;
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

                Server.InvokeTo(e.Value).OnConnected(player.Name, player.Room);

                ForOthers(e.Value, (p) =>
                {
                    // Tell new player other players' info
                    Server.InvokeTo(e.Value).OnPlayerJoined(p.Value.Name, p.Value.Room);
                    // Tell other player new player's info
                    Server.InvokeTo(p.Key).OnPlayerJoined(player.Name, LobbyRoomName);
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
                    Server.InvokeTo(p.Key).OnPlayerLeaved(player.Name);
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

        public void Start(int port)
        {
            LogInfo($"Lobby starting...");
            Server.Start(port);
            LogInfo($"Lobby started");
        }

        public class LobbyExecutor : IPlayerToLobby, IRpcContext
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
                        instance.Server.InvokeTo(p.Key).OnPlayerChangeName(oldName, name);
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
                        instance.Server.InvokeTo(p.Key).OnPlayerLeaved(player.Name);
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
                        instance.Server.InvokeTo(p.Key).OnPlayerChangeRoom(player.Name, room);
                    });

                    instance.ForOthersInRoom(RemoteEndpoint, room, (p) =>
                    {
                        if (p.Value.IsReady)
                        {
                            instance.Server.InvokeTo(RemoteEndpoint).OnPlayerPrepared(p.Value.Name);
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

                    instance.ForOthersInRoom(RemoteEndpoint, player.Room, (p) =>
                    {
                        instance.Server.InvokeTo(p.Key).OnPlayerPrepared(player.Name);
                    });
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
                        instance.Server.InvokeTo(p.Key).OnPlayerUnprepared(player.Name);
                    });
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
                    Server.Dispose();
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
