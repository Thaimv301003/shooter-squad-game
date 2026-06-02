// FILE: LogEntry.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BFunCoreKit
{
    public class LogEntry
    {
        public string Message;
        public LogType Type;
        public bool IsExpanded;
        public bool IsFromCommand;

        // Thêm trường lưu thời gian (nếu sau này cần dùng riêng)
        public string Timestamp { get; private set; }

        public List<string> StackTraceLines { get; private set; }
        public List<string> MessageLines { get; private set; }
        public string CachedStackTrace { get; private set; }

        public LogEntry(string message, string stackTrace, LogType type, bool isFromCommand = false)
        {
            // --- BẮT ĐẦU THÊM MỚI ---
            // 1. Lấy thời gian hiện tại (Giờ:Phút:Giây)
            Timestamp = DateTime.Now.ToString("HH:mm:ss");

            // 2. Tạo chuỗi hiển thị thời gian với màu xám (sử dụng Rich Text của TMPro)
            // Màu #888888 là màu xám nhạt, bạn có thể đổi màu khác nếu thích
            string timePrefix = $"<color=#888888>[{Timestamp}]</color> ";

            // 3. Chèn thời gian vào đầu message
            if (!string.IsNullOrEmpty(message))
            {
                message = timePrefix + message;
            }
            // --- KẾT THÚC THÊM MỚI ---

            Message = message;
            Type = type;
            IsExpanded = false;
            IsFromCommand = isFromCommand;
            StackTraceLines = new List<string>();

            if (!string.IsNullOrEmpty(stackTrace))
            {
                StackTraceLines = stackTrace.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(line => line.Trim())
                                            .ToList();
                CachedStackTrace = string.Join("\n", StackTraceLines);
            }
            else
            {
                CachedStackTrace = string.Empty;
            }

            if (!string.IsNullOrEmpty(message))
            {
                // Message lúc này đã bao gồm thời gian
                MessageLines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(line => line.Trim())
                                        .ToList();
            }
        }
    }
}