namespace SneakRobber2.Shared
{
    public interface ILobbyToPlayer
    {
        void OnPlayerJoined(string name, string room);

        void OnPlayerLeaved(string name, string room);

        void OnPlayerPrepared(string name);

        void OnPlayerUnprepared(string name);

        void OnGameStarted(string ip, int port, string[] players);

        void OnGameStartFailed(string err);
    }
}
