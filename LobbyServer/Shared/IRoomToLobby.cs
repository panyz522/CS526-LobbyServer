namespace SneakRobber2.Shared
{
    public interface IRoomToLobby
    {
        /// <summary>
        /// Notify lobby server the backend is ready.
        /// </summary>
        /// <param name="roomName">Name of the room.</param>
        /// <param name="port">The backend port.</param>
        /// <param name="token">The base token.</param>
        void GameReady(string roomName, int port, int token);

        /// <summary>
        /// Notify lobby server the game is over.
        /// </summary>
        /// <param name="roomName">Name of the room.</param>
        /// <param name="winner">The winner token.</param>
        void GameOver(string roomName, int winner);

        /// <summary>
        /// Games the interrupted.
        /// </summary>
        /// <param name="roomName">Name of the room.</param>
        /// <param name="code">The code.</param>
        void GameInterrupted(string roomName, int code);
    }
}
