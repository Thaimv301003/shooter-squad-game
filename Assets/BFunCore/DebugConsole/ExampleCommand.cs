// FILE: ExampleCommands.cs
#if BFUN_INSTALLED_TRUE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text; // <-- THÊM DÒNG NÀY
using BFunCoreKit;

#if USE_ADMOB
using TheLegends.Base.Ads;
#endif
using UnityEngine;
using UnityEngine.Scripting;

namespace BFunCoreKit
{
    [Preserve]
    public static class ExampleCommands
    {
        // =========================================================================
        // === THAY THẾ LỆNH HELP CŨ BẰNG LỆNH NÀY ================================
        // =========================================================================
        [DebugCommand("help", "Hiển thị tất cả các lệnh có sẵn.")]
        private static void Command_Help()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<b><color=#FFFFFF>══════════════ DEBUG COMMANDS ══════════════</color></b>\n");

            var sortedCommands = DebugConsole.Instance.Commands
                .OrderBy(kvp => kvp.Key);

            foreach (var kvp in sortedCommands)
            {
                var commandId = kvp.Key;
                var method = kvp.Value;
                var attr = method.GetCustomAttribute<DebugCommand>();
                var desc = attr?.Description ?? "Không có mô tả.";

                var parameters = method.GetParameters();
                var paramText = parameters.Length == 0
                    ? "<color=grey>( )</color>"
                    : $"<color=grey>({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})</color>";

                sb.AppendLine($"<b><color=#FFD700>▶ {commandId}</color></b>");
                sb.AppendLine($"   {paramText}");
                sb.AppendLine($"   → {desc}\n");
            }

            sb.AppendLine("<b><color=#FFFFFF>══════════════ DEBUG COMMANDS ══════════════</color></b>\n");

            Debug.Log(sb.ToString());
        }


        [DebugCommand("clear", "Xóa toàn bộ log trên console.")]
        private static void Command_Clear()
        {
            if (DebugConsole.Instance != null)
            {
                DebugConsole.Instance.ClearLogs();
            }
        }

#if USE_ADMOB

        // =========================================================================
        // === DEVICE INFO COMMAND =================================================
        // =========================================================================
        [DebugCommand("device_info", "Hiển thị thống số thiết bị.")]
        private static void Command_Device()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<b><color=#00FF00>════════════ DEVICE INFO ════════════</color></b>");

            sb.AppendLine($"<b>Device:</b> {SystemInfo.deviceName}");
            sb.AppendLine($"<b>Model:</b> {SystemInfo.deviceModel}");
            sb.AppendLine($"<b>OS:</b> {SystemInfo.operatingSystem}");
            sb.AppendLine($"<b>Platform:</b> {Application.platform}");

            sb.AppendLine("\n<b><color=#AAAAAA>— Hardware —</color></b>");
            sb.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            sb.AppendLine($"RAM: {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"GPU RAM: {SystemInfo.graphicsMemorySize} MB");

            sb.AppendLine("\n<b><color=#AAAAAA>— Display —</color></b>");
            sb.AppendLine($"Resolution: {Screen.width} x {Screen.height}");
            sb.AppendLine($"Refresh: {Screen.currentResolution.refreshRate} Hz");
            sb.AppendLine($"DPI: {Screen.dpi}");

            sb.AppendLine("\n<b><color=#AAAAAA>— Power —</color></b>");
            sb.AppendLine($"Battery: {(SystemInfo.batteryLevel * 100):F0}% ({SystemInfo.batteryStatus})");

            sb.AppendLine("<b><color=#00FF00>══════════════════════════════════════</color></b>");

            Debug.Log(sb.ToString());
        }


        // =========================================================================
        // === BUILD INFO COMMAND ==================================================
        // =========================================================================
        [DebugCommand("build_info", "Hiển thị thống số Build.")]
        private static void Command_BuildInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<b><color=#00FFFF>════════════ BUILD INFO ════════════</color></b>");

            sb.AppendLine($"<b>Product:</b> {Application.productName}");
            sb.AppendLine($"<b>Bundle ID:</b> {Application.identifier}");
            sb.AppendLine($"<b>Company:</b> {Application.companyName}");
            sb.AppendLine($"<b>Unity:</b> {Application.unityVersion}");

            sb.AppendLine("\n<b><color=#AAAAAA>— Version —</color></b>");
            var info = BuildInfo.Get();
            if (info != null)
            {
                sb.AppendLine($"Build Version: {info.version}");
                sb.AppendLine($"Build Time: {info.buildTime}");
            }
            else
            {
                sb.AppendLine("<color=red>BuildInfo missing</color>");
            }

            sb.AppendLine("\n<b><color=#AAAAAA>— Android —</color></b>");
            sb.AppendLine(GetAndroidBuildDetails());

            sb.AppendLine("<b><color=#00FFFF>══════════════════════════════════════</color></b>");

            Debug.Log(sb.ToString());
        }


        [DebugCommand("ads_enable", "Bật tắt ads.")]
        private static void EnableAds()
        {
            AdsManager.Instance.IsCanShowAds = !AdsManager.Instance.IsCanShowAds;
            if(AdsManager.Instance.IsCanShowAds)
            {
                GUIManager.Instance.SetMainCanvasBannerHeight(140);
            }
            else
            {
                GUIManager.Instance.SetMainCanvasBannerHeight(0);
            }
            BFun.Log("Ads is " + (AdsManager.Instance.IsCanShowAds ? "Enable" : "Dsiable"));
        }

        private static string GetAndroidBuildDetails()
        {
#if UNITY_EDITOR
            return "<b>Target API:</b> (Editor) | <b>Bundle Code:</b> (Editor)";
#elif UNITY_ANDROID
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
                {
                    string packageName = currentActivity.Call<string>("getPackageName");
                    
                    // 1. Lấy PackageInfo để lấy Bundle Version Code
                    string bundleCode = "0";
                    using (var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0))
                    {
                        if (SystemInfo.operatingSystem.Contains("API-28") || SystemInfo.operatingSystem.Contains("API-29") || SystemInfo.operatingSystem.Contains("API-3"))
                        {
                             try { bundleCode = packageInfo.Get<long>("longVersionCode").ToString(); } catch {}
                        }
                        else
                        {
                             bundleCode = packageInfo.Get<int>("versionCode").ToString();
                        }
                    }

                    // 2. Lấy ApplicationInfo để lấy Target SDK và Min SDK
                    string targetSdk = "Unknown";
                    string minSdk = "Unknown";
                    
                    using (var appInfo = packageManager.Call<AndroidJavaObject>("getApplicationInfo", packageName, 0))
                    {
                        // Lấy Target SDK Version
                        targetSdk = appInfo.Get<int>("targetSdkVersion").ToString();
                        
                        // Lấy Min SDK Version (Trường này có sẵn từ API 24+)
                        // Vì game của bạn set Min API 25 nên chắc chắn trường này tồn tại.
                        if (SystemInfo.operatingSystem.Contains("API") && !SystemInfo.operatingSystem.Contains("API-1") && !SystemInfo.operatingSystem.Contains("API-20")) 
                        {
                             try { minSdk = appInfo.Get<int>("minSdkVersion").ToString(); } catch {}
                        }
                    }

                    // Format kết quả trả về
                    var sb = new StringBuilder();
                    sb.AppendLine($"<b>Bundle Code:</b> {bundleCode}");
                    sb.AppendLine($"<b>Target API:</b> {targetSdk}");
                    sb.AppendLine($"<b>Min API:</b> {minSdk}");
                    return sb.ToString().TrimEnd(); // TrimEnd để xóa ký tự xuống dòng thừa
                }
            }
            catch (System.Exception e)
            {
                return $"<b>Android Info Error:</b> {e.Message}";
            }
#else
            return "<b>Platform Info:</b> N/A (Not Android)";
#endif
        }
        [DebugCommand("ads_info", "Xem chi tiết Ads: Status, ID List, Configs.")]
        private static void Command_Ads()
        {
            if (AdsManager.Instance == null) return;

            var manager = AdsManager.Instance;
            var settings = manager.SettingsAds;
            var config = manager.adsConfigs;
            var type = manager.GetType();

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<b><color=#FFD700>════════════ ADS INFO ════════════</color></b>");

            // ============================================================
            // 1. SYSTEM STATUS
            // ============================================================
            sb.AppendLine("<b><color=#AAAAAA>— System —</color></b>");

            var statusField = type.GetField("status", BindingFlags.NonPublic | BindingFlags.Instance);
            sb.AppendLine($"Init Status: {statusField?.GetValue(manager)}");
            sb.AppendLine($"Internet: {Application.internetReachability}");

            var timeField = type.GetField("lastTimeShowAd", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timeField != null)
            {
                DateTime lastTime = (DateTime)timeField.GetValue(manager);
                float played = (float)(DateTime.Now - lastTime).TotalSeconds;
                bool ready = Mathf.FloorToInt(played) >= config.timePlayToShowAds;

                string color = ready ? "#00FF00" : "yellow";
                sb.AppendLine(
                    $"Cooldown: <color={color}>{(ready ? "READY" : "WAITING")}</color> " +
                    $"({played:F0}s / {config.timePlayToShowAds}s)"
                );
            }

            // ============================================================
            // 2. LIVE INVENTORY (STATUS)
            // ============================================================
            sb.AppendLine("\n<b><color=#AAAAAA>— Inventory —</color></b>");

            string interAuto = "";
            if (config.adInterOnStart) interAuto += "Start ";
            if (config.adInterOnComplete) interAuto += "Complete";
            if (string.IsNullOrEmpty(interAuto)) interAuto = "None";

            sb.AppendLine($"Interstitial: {GetAdStatusSilent(manager, AdsType.Interstitial)} <color=grey>(Auto: {interAuto})</color>");
            sb.AppendLine($"Rewarded: {GetAdStatusSilent(manager, AdsType.Rewarded)}");
            sb.AppendLine($"Banner: {GetAdStatusSilent(manager, AdsType.Banner)}");
            sb.AppendLine($"App Open: {GetAdStatusSilent(manager, AdsType.AppOpen)}");

            // ============================================================
            // 3. CONFIG
            // ============================================================
            sb.AppendLine("\n<b><color=#AAAAAA>— Config —</color></b>");
            sb.AppendLine($"Reload Time: {config.adTimeReload}s (Inter) / {config.nativeBannerTimeReload}s (Native)");
            sb.AppendLine($"Open Timeout: {config.adInterOpenTimeOut}s");
            sb.AppendLine($"Preload: {GetPreloadInfo(settings)}");

            // ============================================================
            // 4. NETWORK & IDS (GIỮ NGUYÊN ĐẦY ĐỦ)
            // ============================================================
            sb.AppendLine("\n<b><color=#AAAAAA>— Network & IDs —</color></b>");
            sb.AppendLine($"Primary: <color=yellow>{settings.primaryMediation}</color>");
            sb.AppendLine($"Active: {string.Join(", ", settings.AdsMediations)}");

            if (settings.showADMOB)
            {
                sb.AppendLine("\n<color=#FF9900>[ GOOGLE ADMOB ]</color>");
#if UNITY_ANDROID
                PrintDeepObject(sb, settings.ADMOB_Android);
#elif UNITY_IOS
        PrintDeepObject(sb, settings.ADMOB_IOS);
#endif
            }

            if (settings.showMAX)
            {
                sb.AppendLine("\n<color=#FF9900>[ APPLOVIN MAX ]</color>");
#if UNITY_ANDROID
                PrintDeepObject(sb, settings.MAX_Android);
#elif UNITY_IOS
        PrintDeepObject(sb, settings.MAX_iOS);
#endif
            }

            sb.AppendLine("<b><color=#FFD700>══════════════════════════════════════</color></b>");

            Debug.Log(sb.ToString());
        }



        // ========================================================================
        // === CÁC HÀM HELPER ===
        // ========================================================================

        // --- HÀM MỚI: TẮT LOG TRƯỚC KHI GỌI ĐỂ TRÁNH LỖI ĐỎ ---
        private static string GetAdStatusSilent(AdsManager manager, AdsType type)
        {
            // 1. Lưu lại trạng thái log hiện tại
            bool logEnabled = Debug.unityLogger.logEnabled;

            // 2. TẮT LOG (Bịt miệng Unity lại)
            Debug.unityLogger.logEnabled = false;

            AdsEvents status = AdsEvents.None;
            try
            {
                // 3. Gọi hàm gây lỗi (Nó sẽ hét lên, nhưng không ai nghe thấy vì đã tắt log)
                status = manager.GetAdsStatus(type, PlacementOrder.One);
            }
            catch
            {
                status = AdsEvents.None;
            }
            finally
            {
                // 4. BẬT LOG LẠI NGAY LẬP TỨC
                Debug.unityLogger.logEnabled = logEnabled;
            }

            // 5. Trả về kết quả đẹp đẽ
            switch (status)
            {
                case AdsEvents.LoadAvailable:
                    return "<color=#00FF00><b>READY</b></color>";
                case AdsEvents.LoadRequest:
                    return "<color=yellow>LOADING...</color>";
                case AdsEvents.LoadFail:
                    return "<color=red>FAILED</color>";
                case AdsEvents.LoadNotAvailable:
                case AdsEvents.None:
                    return "<color=grey>NO CONFIG</color>";
                default:
                    return $"<color=grey>{status}</color>";
            }
        }

        private static string GetPreloadInfo(AdsSettings settings)
        {
            if (settings.preloadSettings == null) return "None";
            var p = settings.preloadSettings;
            var list = new List<string>();
            if (p.preloadBanner) list.Add("Banner");
            if (p.preloadInterstitial) list.Add("Inter");
            if (p.preloadRewarded) list.Add("Reward");
            if (p.preloadAppOpen) list.Add("AppOpen");
            return list.Count > 0 ? string.Join(", ", list) : "None";
        }

        // --- CÁC HÀM IN ID (GIỮ NGUYÊN) ---
        private static void PrintDeepObject(StringBuilder sb, object obj)
        {
            if (obj == null) return;
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var members = fields.Cast<MemberInfo>().Concat(props);

            foreach (var member in members)
            {
                object value = null;
                try
                {
                    if (member is FieldInfo f) value = f.GetValue(obj);
                    else if (member is PropertyInfo p) value = p.GetValue(obj);
                }
                catch { continue; }

                if (value == null) continue;
                if (value is IEnumerable list && !(value is string))
                {
                    int count = 0;
                    foreach (var item in list) count++;
                    if (count > 0)
                    {
                        sb.AppendLine($"   - <b>{member.Name}</b> ({count}):");
                        foreach (var item in list) PrintPlacementItem(sb, item);
                    }
                }
            }
        }

        private static void PrintPlacementItem(StringBuilder sb, object item)
        {
            if (item == null) return;
            string orderVal = FindValueByNames(item, "order", "placement", "pos", "position", "name");
            if (string.IsNullOrEmpty(orderVal) || orderVal.Contains("TheLegends") || orderVal.Contains("."))
            {
                var str = item.ToString();
                orderVal = (str.Contains("TheLegends") || str.Contains(".")) ? "Placement" : str;
            }

            string rawIds = FindValueByNames(item, "id", "ids", "ID", "adUnitId", "adId", "androidId", "iosId");
            if (string.IsNullOrEmpty(rawIds)) rawIds = FindAnyStringLikeId(item);

            sb.AppendLine($"      + {orderVal}:");
            if (rawIds != "---")
            {
                var idList = rawIds.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < idList.Length; i++)
                    sb.AppendLine($"          <color=grey>[ID {i + 1:00}]</color> <color=#00FF00>{idList[i]}</color>");
            }
            else
            {
                sb.AppendLine($"          <color=red>[NO ID]</color>");
            }
        }

        private static string FindValueByNames(object obj, params string[] clues)
        {
            var type = obj.GetType();
            string ProcessValue(object val)
            {
                if (val == null) return null;
                if (val is IEnumerable list && !(val is string))
                {
                    var strItems = new List<string>();
                    foreach (var item in list) strItems.Add(item.ToString());
                    return string.Join(", ", strItems);
                }
                return val.ToString();
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var clue in clues)
                {
                    if (field.Name.Equals(clue, StringComparison.OrdinalIgnoreCase) ||
                       (field.Name.ToLower().Contains(clue.ToLower()) && clue.Length > 2))
                    {
                        var res = ProcessValue(field.GetValue(obj));
                        if (!string.IsNullOrEmpty(res)) return res;
                    }
                }
            }
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var clue in clues)
                {
                    if (prop.Name.Equals(clue, StringComparison.OrdinalIgnoreCase) ||
                       (prop.Name.ToLower().Contains(clue.ToLower()) && clue.Length > 2))
                    {
                        try
                        {
                            var res = ProcessValue(prop.GetValue(obj));
                            if (!string.IsNullOrEmpty(res)) return res;
                        }
                        catch { }
                    }
                }
            }
            return null;
        }

        private static string FindAnyStringLikeId(object obj)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(string))
                {
                    var val = field.GetValue(obj) as string;
                    if (string.IsNullOrEmpty(val)) continue;
                    if (val.Contains("/") || val.Contains("_") || val.StartsWith("ca-") || val.Length > 8)
                    {
                        if (!val.Contains("TheLegends")) return val;
                    }
                }
            }
            return "---";
        }
#endif
    }
}
#endif