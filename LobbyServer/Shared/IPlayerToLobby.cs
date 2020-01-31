namespace SneakRobber2.Shared
{
    public interface IPlayerToLobby
    {
        bool JoinRoom(string room);

        bool Prepare();

        bool Unprepare();

        void Exit();
    }
}
