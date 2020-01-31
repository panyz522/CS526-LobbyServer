using System;
using System.Collections.Generic;
using System.Text;

namespace SneakRobber2.Utils
{
    internal static class Logger
    {
        public static void LogInfo(object obj)
        {
            Console.Write($"[{DateTime.Now}] ");
            Console.WriteLine(obj);
        }

        public static void LogWarning(object obj)
        {
            var color = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.Write($"[{DateTime.Now}] ");
            Console.Write(obj);
            Console.BackgroundColor = color;
            Console.WriteLine();
        }

        public static void LogError(object obj)
        {
            var color = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Write($"[{DateTime.Now}] ");
            Console.Write(obj);
            Console.BackgroundColor = color;
            Console.WriteLine();
        }
    }
}
