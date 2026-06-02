using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.Ads.Editor
{
    public static class AdsSettingsUIUtils
    {
        public static void DrawSectionBackground(Color bg, System.Action content)
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = bg;
            EditorGUILayout.BeginVertical("helpBox");
            GUI.backgroundColor = prevBg;
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void DrawSymbolWarning(AdsSettings instance, AdsMediation mediation, string symbol)
        {
            if ((instance.FlagMediations & mediation) != 0 &&
                !PackagesManagerIntergration.IsSymbolEnabled(symbol))
            {
                EditorGUILayout.HelpBox(
                    $"Press SAVE to add scripting define \"{symbol}\" to Player Settings.",
                    MessageType.Warning);
            }
        }

        public static void DrawValidationWarnings(AdsSettings instance)
        {
            DrawSymbolWarning(instance, AdsMediation.Iron, "USE_IRON");
            DrawSymbolWarning(instance, AdsMediation.Max, "USE_MAX");
            DrawSymbolWarning(instance, AdsMediation.Admob, "USE_ADMOB");

            if (instance.useFirebase && !PackagesManagerIntergration.IsSymbolEnabled("USE_FIREBASE"))
                EditorGUILayout.HelpBox("Press SAVE to add scripting define \"USE_FIREBASE\"", MessageType.Warning);

            if (instance.useAppsFlyer && !PackagesManagerIntergration.IsSymbolEnabled("USE_APPSFLYER"))
                EditorGUILayout.HelpBox("Press SAVE to add scripting define \"USE_APPSFLYER\"", MessageType.Warning);

            if (instance.useDatabuckets && !PackagesManagerIntergration.IsSymbolEnabled("USE_DATABUCKETS"))
                EditorGUILayout.HelpBox("Press SAVE to add scripting define \"USE_DATABUCKETS\"", MessageType.Warning);

            if (instance.useFacebook && !PackagesManagerIntergration.IsSymbolEnabled("USE_FACEBOOK"))
                EditorGUILayout.HelpBox("Press SAVE to add scripting define \"USE_FACEBOOK\"", MessageType.Warning);
        }
    }
}
