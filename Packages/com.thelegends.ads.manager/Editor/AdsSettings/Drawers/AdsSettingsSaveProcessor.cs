using System;
using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.Ads.Editor
{
    public class AdsSettingsSaveProcessor
    {
        public void DrawSaveButton(AdsSettings instance, Action saveCallback)
        {
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.45f);
            if (GUILayout.Button("SAVE", GUILayout.Height(32)))
            {
                GUI.backgroundColor = Color.white;
                saveCallback?.Invoke();

                PackagesManagerIntergration.SetSymbolEnabled("USE_IRON", instance.showIRON);
                PackagesManagerIntergration.SetSymbolEnabled("USE_MAX", instance.showMAX);
                PackagesManagerIntergration.SetSymbolEnabled("USE_ADMOB", instance.showADMOB);
                PackagesManagerIntergration.SetSymbolEnabled("USE_FIREBASE", instance.useFirebase);
                PackagesManagerIntergration.SetSymbolEnabled("USE_APPSFLYER", instance.useAppsFlyer);
                PackagesManagerIntergration.SetSymbolEnabled("USE_DATABUCKETS", instance.useDatabuckets);
                PackagesManagerIntergration.SetSymbolEnabled("USE_FACEBOOK", instance.useFacebook);

                PackagesManagerIntergration.SetSymbolEnabled("USE_ADMOB_NATIVE_UNITY",
                    instance.showADMOB && instance.isUseNativeUnity);
                PackagesManagerIntergration.SetSymbolEnabled("HIDE_WHEN_FULLSCREEN_SHOWED",
                    instance.showADMOB && instance.isHideWhenFullscreenShowed);

                PackagesManagerIntergration.UpdateManifest();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
