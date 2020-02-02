﻿namespace SneakRobber2.Shared
{
    public interface ILobbyToPlayer
    {
        void OnConnected(string givenName);

        void OnPlayerJoined(string name, string room);

        void OnPlayerLeaved(string name);

        void OnPlayerChangeRoom(string name, string room);

        void OnPlayerChangeName(string oldName, string newName);

        void OnPlayerPrepared(string name);

        void OnPlayerUnprepared(string name);

        void OnGameStarted(string ip, int port, string[] players);

        void OnGameStartFailed(string err);
    }
}
