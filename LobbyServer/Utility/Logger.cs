using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
#endif

namespace SneakRobber2.Utility
{
    public static class Logger
    {
        private static Func<object, string> format = (s) => $"[{DateTime.Now}] {s}";

        private static Action<string> logInfo =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.Log(s); };
#else
            (s) =>
            {
                Console.WriteLine(s);
            };
#endif
        private static Action<string> logWarning =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.LogWarning(s); };
#else
            (s) =>
            {
                var color = Console.BackgroundColor;
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(s);
                Console.BackgroundColor = color;
            };
#endif
        private static Action<string> logError =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.LogError(s); };
#else
            (s) =>
            {
                var color = Console.BackgroundColor;
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(s);
                Console.BackgroundColor = color;
            };
#endif

        public static void SetLogger(Action<string> log)
        {
            Logger.logInfo = log;
            Logger.logWarning = log;
            Logger.logError = log;
        }

        public static void LogInfo(object obj)
        {
            logInfo(format(obj));
        }

        public static void LogWarning(object obj)
        {
            logWarning(format(obj));
        }

        public static void LogError(object obj)
        {
            logError(format(obj));
        }
    }
}
