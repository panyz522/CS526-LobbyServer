namespace SneakRobber2.Shared
{
    public interface IRoomToLobby
    {
        void GameReady(string ip, int port, int token);

        void GameOver(int winner);

        void GameInterrupted(int code);
    }
}
