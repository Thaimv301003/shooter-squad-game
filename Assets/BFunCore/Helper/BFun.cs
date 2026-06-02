// FILE: BFun.cs
using UnityEngine;
#if USE_APPSFLYER
using System.Collections.Generic; // Cần thiết cho Dictionary
#endif
#if USE_FIREBASE
using TheLegends.Base.Firebase;
using Firebase.Analytics;
using static TheLegends.Base.Firebase.FirebaseManager;
#endif

namespace BFunCoreKit
{
    public static class BFun
    {
        public static bool IsEnabled = true;

        private const string ICON_BULLET = "●";

        // ===== HÀM LOG CHÍNH, CHỊU TRÁCH NHIỆM TÔ MÀU VÀ THÊM PREFIX =====
        private static void LogInternal(object message, LogType logType, string iconColor = "white", string prefix = "")
        {
            if (!IsEnabled) return;

            string coloredIcon = $"<color={iconColor}>{ICON_BULLET}</color>";
            string coloredPrefix = "";
            if (!string.IsNullOrEmpty(prefix))
            {
                coloredPrefix = $"<color={iconColor}>[{prefix}]</color> ";
            }
            string formattedMessage = $"{coloredIcon} {coloredPrefix}<color=white>{message}</color>";

            #if BFUN_INSTALLED_TRUE
            if (DebugConsole.IsExecutingCommand)
            {
                DebugConsole.Instance.HandleCommandLog($"> {message}");
                return;
            }
            #endif

            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(formattedMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }

        // ===== CÁC HÀM PUBLIC MÀ BẠN SẼ GỌI =====
        
        public static void Log(object message)
        {
            LogInternal(message, LogType.Log, "white");
        }

        public static void LogWarning(object message)
        {
            LogInternal(message, LogType.Warning, "yellow", "Warning");
        }

        public static void LogError(object message)
        {
            LogInternal(message, LogType.Error, "red", "Error");
        }

#if UNITY_EDITOR
        public static void LogEditor(object message)
        {
            LogInternal(message, LogType.Log, "#15ff00ff", "Editor");
        }
#endif

        public static void LogEvent(string message)
        {
            LogInternal(message, LogType.Log, "#65c4ffff", "Event");

            #if USE_FIREBASE
            FirebaseManager.Instance.LogEvent(message);
            #endif
        }

        public static void LogFB(string titleName, string paraName, string paraValue)
        {
            // <<< THAY ĐỔI ĐỊNH DẠNG LOG CHO DỄ ĐỌC HƠN >>>
            LogInternal($"{titleName}: {paraName}='{paraValue}'", LogType.Log, "#ff4800ff", "FB");
            
            #if USE_FIREBASE
                if (FirebaseManager.Instance.Status != FirebaseStatus.Initialized)
                {
                    Debug.LogWarning("[Firebase] AnalyticStatus: " + FirebaseManager.Instance.Status);
                    return;
                }
                FirebaseAnalytics.LogEvent(titleName, new Parameter(paraName, paraValue));
            #endif

            #if USE_APPSFLYER
                Dictionary<string, string> eventValues = new Dictionary<string, string>();
                eventValues.Add(paraName, paraValue);
                AppsFlyerSDK.AppsFlyer.sendEvent(titleName, eventValues);
            #endif
        }

        public static void LogFB2(string titleName, string para1Name, string para1Value, string para2Name, string para2Value)
        {
            // <<< THAY ĐỔI ĐỊNH DẠNG LOG CHO DỄ ĐỌC HƠN >>>
            LogInternal($"{titleName}: {para1Name}='{para1Value}', {para2Name}='{para2Value}'", LogType.Log, "#ff4800ff", "FB");
            
            #if USE_FIREBASE
                if (FirebaseManager.Instance.Status != FirebaseStatus.Initialized)
                {
                    Debug.LogWarning("[Firebase] AnalyticStatus: " + FirebaseManager.Instance.Status);
                    return;
                }
                FirebaseAnalytics.LogEvent(titleName, new Parameter[] { new Parameter(para1Name, para1Value), new Parameter(para2Name, para2Value) });
            #endif

            // <<< THÊM MỚI APPSFLYER >>>
            #if USE_APPSFLYER
                Dictionary<string, string> eventValues = new Dictionary<string, string>();
                eventValues.Add(para1Name, para1Value);
                eventValues.Add(para2Name, para2Value);
                AppsFlyerSDK.AppsFlyer.sendEvent(titleName, eventValues);
            #endif
        }
        
        public static void LogFB4(string titleName, string para1Name, string para1Value, string para2Name, int para2Value, string para3Name, string para3Value, string para4Name, int para4Value)
        {
            // <<< THAY ĐỔI ĐỊNH DẠNG LOG CHO DỄ ĐỌC HƠN >>>
            LogInternal($"{titleName}: {para1Name}='{para1Value}', {para2Name}='{para2Value}', {para3Name}='{para3Value}', {para4Name}='{para4Value}'", LogType.Log, "#ff4800ff", "FB");

            #if USE_FIREBASE
                if (FirebaseManager.Instance.Status != FirebaseStatus.Initialized)
                {
                    Debug.LogWarning("[Firebase] AnalyticStatus: " + FirebaseManager.Instance.Status);
                    return;
                }
                FirebaseAnalytics.LogEvent(titleName, new Parameter[] { new Parameter(para1Name, para1Value), new Parameter(para2Name, para2Value), new Parameter(para3Name, para3Value), new Parameter(para4Name, para4Value) });
            #endif

            // <<< THÊM MỚI APPSFLYER, LƯU Ý VIỆC CHUYỂN ĐỔI SANG STRING >>>
            #if USE_APPSFLYER
                Dictionary<string, string> eventValues = new Dictionary<string, string>();
                eventValues.Add(para1Name, para1Value);
                eventValues.Add(para2Name, para2Value.ToString()); // Chuyển int sang string
                eventValues.Add(para3Name, para3Value);
                eventValues.Add(para4Name, para4Value.ToString()); // Chuyển int sang string
                AppsFlyerSDK.AppsFlyer.sendEvent(titleName, eventValues);
            #endif
        }
    }
}