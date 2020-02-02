namespace SneakRobber2.Shared
{
    public interface IPlayerToLobby
    {
        void ChangeName(string name);

        void ChangeRoom(string room);

        void Prepare();

        void Unprepare();

        void Exit();
    }
}
