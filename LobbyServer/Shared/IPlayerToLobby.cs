namespace SneakRobber2.Shared
{
    public interface IPlayerToLobby
    {
        /// <summary>
        /// Changes the name.
        /// </summary>
        /// <param name="name">The name.</param>
        void ChangeName(string name);

        /// <summary>
        /// Changes the room.
        /// </summary>
        /// <param name="room">The room.</param>
        void ChangeRoom(string room);

        /// <summary>
        /// Prepare to start.
        /// </summary>
        void Prepare();

        /// <summary>
        /// Unprepares.
        /// </summary>
        void Unprepare();

        /// <summary>
        /// Exits the game. Reserved for future use.
        /// </summary>
        void Exit();
    }
}
