namespace SneakRobber2.Shared
{
    public interface IRoomToLobby
    {
        void GameReady(string roomName, int port, int token);

        void GameOver(int winner);

        void GameInterrupted(int code);
    }
}
