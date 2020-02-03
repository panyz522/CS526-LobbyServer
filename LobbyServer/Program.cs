using Castle.DynamicProxy;
using SneakRobber2.Lobby;
using SneakRobber2.Utility;
using System;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LobbyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using var lobby = new Lobby();
            lobby.Start(10001);
            Console.ReadLine();
        }
    }
}
