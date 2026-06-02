// FILE: DebugConsoleInitializer.cs
using System.Collections.Generic;
using UnityEngine;

namespace BFunCoreKit
{
    public static class DebugConsoleInit
    {
        // Một buffer tạm thời để lưu các log được tạo ra trước khi DebugConsole sẵn sàng
        private static List<LogEntry> earlyLogBuffer = new List<LogEntry>();
        private static bool isSubscribed = false;

        // Hàm này sẽ được Unity tự động gọi khi runtime bắt đầu, trước cả khi scene được load
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isSubscribed) return;

            Application.logMessageReceivedThreaded += HandleEarlyLog;
            isSubscribed = true;
        }

        private static void HandleEarlyLog(string logString, string stackTrace, LogType type)
        {
            earlyLogBuffer.Add(new LogEntry(logString, stackTrace, type));
        }

        // DebugConsole sẽ gọi hàm này để lấy các log đã được buffer
        public static List<LogEntry> FlushBuffer()
        {
            var bufferedLogs = new List<LogEntry>(earlyLogBuffer);
            earlyLogBuffer.Clear();

            // Sau khi console đã sẵn sàng, chúng ta không cần buffer nữa
            Application.logMessageReceivedThreaded -= HandleEarlyLog;
            isSubscribed = false;

            return bufferedLogs;
        }
    }
}