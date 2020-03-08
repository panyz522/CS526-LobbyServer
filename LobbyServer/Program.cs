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
            string path = CmdHelper.GetArg("-path");
            string ipIn = CmdHelper.GetArg("-ip");
            string ip = ipIn == null ? "127.0.0.1" : ipIn;
            lobby.Start(10001, 10002, ip, path);
            Console.ReadLine();
        }
    }
}
