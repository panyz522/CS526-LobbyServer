namespace SneakRobber2.Shared
{
    public interface ILobbyToPlayer
    {
        /// <summary>
        /// Called when connected to lobby server.
        /// </summary>
        /// <param name="givenName">Name given by lobby.</param>
        /// <param name="joinedRoom">The joined room.</param>
        void OnConnected(string givenName, string joinedRoom);

        /// <summary>
        /// Called when some player joined a room.
        /// </summary>
        /// <param name="name">The player's name.</param>
        /// <param name="room">The room.</param>
        void OnPlayerJoined(string name, string room);

        /// <summary>
        /// Called when player leaved.
        /// </summary>
        /// <param name="name">The name.</param>
        void OnPlayerLeaved(string name);

        /// <summary>
        /// Called when player change room.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="room">The room.</param>
        void OnPlayerChangeRoom(string name, string room);

        /// <summary>
        /// Called when player change name.
        /// </summary>
        /// <param name="oldName">The old name.</param>
        /// <param name="newName">The new name.</param>
        void OnPlayerChangeName(string oldName, string newName);

        /// <summary>
        /// Called when player prepared.
        /// </summary>
        /// <param name="name">The name.</param>
        void OnPlayerPrepared(string name);

        /// <summary>
        /// Called when player unprepared.
        /// </summary>
        /// <param name="name">The name.</param>
        void OnPlayerUnprepared(string name);

        /// <summary>
        /// Called when game started. Frontend need to load Game Scene and start game.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        /// <param name="players">The players.</param>
        /// <param name="token">The token.</param>
        void OnGameStarted(string ip, int port, string[] players, int token);

        /// <summary>
        /// Called when game start failed.
        /// </summary>
        /// <param name="err">The error.</param>
        void OnGameStartFailed(string err);
    }
}
