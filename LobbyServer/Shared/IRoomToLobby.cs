namespace SneakRobber2.Shared
{
    public interface IRoomToLobby
    {
        void GameReady(string roomName, int port, int token);

        void GameOver(string roomName, int winner);

        void GameInterrupted(string roomName, int code);
    }
}
