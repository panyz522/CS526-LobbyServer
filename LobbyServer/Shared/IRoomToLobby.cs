namespace SneakRobber2.Shared
{
    public interface IRoomToLobby
    {
        void GameOver(int winner);

        void GameInterrupted(int code);
    }
}
