using System;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
#endif

namespace SneakRobber2.Utility
{
    public class Logger<T>
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        public static string formatStr = "{LoggerName}: {Log}";
#else
        public static string formatStr = "[{Time}] {LoggerName}: {Log}";
#endif

        public static Logger<T> Instance = new Logger<T>();
        private static readonly string loggerName = typeof(T).Name;
        private static Func<object, string> format =
            (s) => formatStr
                .Replace("{Time}", DateTime.Now.ToString("HH:mm:ss.fff"))
                .Replace("{LoggerName}", loggerName)
                .Replace("{Log}", s.ToString());

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
            logInfo = log;
            logWarning = log;
            logError = log;
        }

        public void LogInfo(object obj)
        {
            logInfo(format(obj));
        }

        public void LogWarning(object obj)
        {
            logWarning(format(obj));
        }

        public void LogError(object obj)
        {
            logError(format(obj));
        }
    }
}
