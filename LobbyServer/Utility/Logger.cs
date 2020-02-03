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

        private static Action<object> logInfo =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.Log(format(s)); };
#else
            (s) =>
            {
                Console.WriteLine(format(s));
            };
#endif
        private static Action<object> logWarning =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.LogWarning(format(s)); };
#else
            (s) =>
            {
                var color = Console.BackgroundColor;
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(format(s));
                Console.BackgroundColor = color;
            };
#endif
        private static Action<object> logError =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.LogError(format(s)); };
#else
            (s) =>
            {
                var color = Console.BackgroundColor;
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(format(s));
                Console.BackgroundColor = color;
            };
#endif

        public static void SetLogger(Action<object> log)
        {
            Logger.logInfo = log;
            Logger.logWarning = log;
            Logger.logError = log;
        }

        public static void LogInfo(object obj)
        {
            logInfo(obj);
        }

        public static void LogWarning(object obj)
        {
            logWarning(obj);
        }

        public static void LogError(object obj)
        {
            logError(obj);
        }
    }
}
