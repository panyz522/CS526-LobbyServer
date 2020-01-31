using System;
using System.Collections.Generic;
using System.Text;

namespace SneakRobber2.Utils
{
    public static class Logger
    {
        private static Action<string> log = (s) => { Console.WriteLine(s); };

        public static void SetLogger(Action<string> log)
        {
            Logger.log = log;
        }

        public static void LogInfo(object obj)
        {
            log($"[{DateTime.Now}] ");
            log(obj.ToString());
            log(Environment.NewLine);
        }

        public static void LogWarning(object obj)
        {
            var color = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkYellow;

            log($"[{DateTime.Now}] ");
            log(obj.ToString());
            log(Environment.NewLine);

            Console.BackgroundColor = color;
        }

        public static void LogError(object obj)
        {
            var color = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkRed;

            log($"[{DateTime.Now}] ");
            log(obj.ToString());
            log(Environment.NewLine);

            Console.BackgroundColor = color;
        }
    }
}
