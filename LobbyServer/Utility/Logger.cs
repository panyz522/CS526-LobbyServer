using System;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
#endif

namespace SneakRobber2.Utility
{
    public class Logger<T>
    {
        public static Logger<T> Instance = new Logger<T>();
        private static readonly string loggerName = typeof(T).Name;

        public void LogInfo(object obj)
        {
            LoggerBase.logInfo(LoggerBase.format(obj, loggerName));
        }

        public void LogWarning(object obj)
        {
            LoggerBase.logWarning(LoggerBase.format(obj, loggerName));
        }

        public void LogError(object obj)
        {
            LoggerBase.logWarning(LoggerBase.format(obj, loggerName));
        }
    }

    public class LoggerBase
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        public static string formatStr = "{LoggerName}: {Log}";
#else
        public static string formatStr = "[{Time}] {LoggerName}: {Log}";
#endif

        public static Func<object, string, string> format =
            (s, loggerName) => formatStr
                .Replace("{Time}", DateTime.Now.ToString("HH:mm:ss.fff"))
                .Replace("{LoggerName}", loggerName)
                .Replace("{Log}", s.ToString());

        public static Action<string> logInfo =
#if UNITY_EDITOR || UNITY_STANDALONE
            (s) => { Debug.Log(s); };
#else
            (s) =>
            {
                Console.WriteLine(s);
            };
#endif
        public static Action<string> logWarning =
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
        public static Action<string> logError =
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

        public static void SetAllLogger(Action<string> log)
        {
            logInfo = log;
            logWarning = log;
            logError = log;
        }
    }
}
