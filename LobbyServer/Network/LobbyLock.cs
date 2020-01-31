using System;
using System.Collections.Generic;
using System.Text;

namespace SneakRobber2.Network
{
    public static class LobbyLock
    {
        public static object Instance { get; } = new object();
    }
}
