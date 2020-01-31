namespace SneakRobber2.Shared
{
    public interface IPlayerToLobby
    {
        void JoinRoom(string room);

        void Prepare();

        void Unprepare();

        void Exit();
    }
}
