using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.Ads.Editor
{
    public class AdsNetworkConfigsDrawer
    {
        public void Draw(AdsSettings instance, SerializedObject serializedObject, GUIStyle titleStyle)
        {
            #region IronSource
#if USE_IRON
            if ((instance.FlagMediations & AdsMediation.Iron) != 0)
            {
                if (AssetDatabase.IsValidFolder("Assets/LevelPlay/Editor/"))
                {
                    IronSourceMediationSettings ironSourceMediationSettings = Resources.Load<IronSourceMediationSettings>(IronSourceConstants.IRONSOURCE_MEDIATION_SETTING_NAME);
                    if (ironSourceMediationSettings == null)
                    {
                        IronSourceMediationSettings asset = ScriptableObject.CreateInstance<IronSourceMediationSettings>();
                        Directory.CreateDirectory(IronSourceConstants.IRONSOURCE_RESOURCES_PATH);
                        AssetDatabase.CreateAsset(asset, IronSourceMediationSettings.IRONSOURCE_SETTINGS_ASSET_PATH);
                        ironSourceMediationSettings = asset;
                    }
                    ironSourceMediationSettings.AndroidAppKey = instance.ironAndroidAppKey;
                    ironSourceMediationSettings.IOSAppKey     = instance.ironIOSAppKey;
                    ironSourceMediationSettings.AddIronsourceSkadnetworkID    = true;
                    ironSourceMediationSettings.DeclareAD_IDPermission        = true;
                    ironSourceMediationSettings.EnableIronsourceSDKInitAPI    = false;

                    IronSourceMediatedNetworkSettings ironSourceMediatedNetworkSettings = Resources.Load<IronSourceMediatedNetworkSettings>(IronSourceConstants.IRONSOURCE_MEDIATED_NETWORK_SETTING_NAME);
                    if (ironSourceMediatedNetworkSettings == null)
                    {
                        IronSourceMediatedNetworkSettings asset = ScriptableObject.CreateInstance<IronSourceMediatedNetworkSettings>();
                        Directory.CreateDirectory(IronSourceConstants.IRONSOURCE_RESOURCES_PATH);
                        AssetDatabase.CreateAsset(asset, IronSourceMediatedNetworkSettings.MEDIATION_SETTINGS_ASSET_PATH);
                        ironSourceMediatedNetworkSettings = asset;
                    }

                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField("IronSource / LevelPlay", titleStyle);

                    instance.ironAndroidAppKey = EditorGUILayout.TextField("Android AppKey", instance.ironAndroidAppKey);
                    instance.ironIOSAppKey     = EditorGUILayout.TextField("iOS AppKey",     instance.ironIOSAppKey);
                    instance.isIronTest        = EditorGUILayout.Toggle("Is Testing",        instance.isIronTest);

                    instance.ironEnableAdmob = EditorGUILayout.Toggle("Enable Admob Mediation", instance.ironEnableAdmob);
                    ironSourceMediatedNetworkSettings.EnableAdmob = instance.ironEnableAdmob;
                    if (instance.ironEnableAdmob)
                    {
                        instance.ironAdmobAndroidAppID = EditorGUILayout.TextField("Admob Android AppID", instance.ironAdmobAndroidAppID);
                        instance.ironAdmobIOSAppID     = EditorGUILayout.TextField("Admob iOS AppID",     instance.ironAdmobIOSAppID);
                        ironSourceMediatedNetworkSettings.AdmobAndroidAppId = instance.ironAdmobAndroidAppID;
                        ironSourceMediatedNetworkSettings.AdmobIOSAppId     = instance.ironAdmobIOSAppID;
                    }
                    else
                    {
                        instance.ironAdmobAndroidAppID = instance.ironAdmobIOSAppID = string.Empty;
                        ironSourceMediatedNetworkSettings.AdmobAndroidAppId = ironSourceMediatedNetworkSettings.AdmobIOSAppId = string.Empty;
                    }

                    AssetDatabase.SaveAssetIfDirty(ironSourceMediationSettings);
                }
            }
#endif
            #endregion

            #region MAX
#if USE_MAX
            if ((instance.FlagMediations & AdsMediation.Max) != 0)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("MAX AppLovin", titleStyle);
                instance.isShowMediationDebugger = EditorGUILayout.Toggle("Use Mediation Debugger", instance.isShowMediationDebugger);
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MAX_Android"), new GUIContent("Android Unit IDs"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("MAX_iOS"),     new GUIContent("iOS Unit IDs"),     true);
            }
#endif
            #endregion

            #region ADMOB
#if USE_ADMOB
            if ((instance.FlagMediations & AdsMediation.Admob) != 0)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Google Admob", titleStyle);
                instance.isShowAdmobNativeValidator = EditorGUILayout.Toggle("Show Admob Validator", instance.isShowAdmobNativeValidator);
                instance.isUseNativeUnity = EditorGUILayout.Toggle("Use Native Unity", instance.isUseNativeUnity);
                instance.isHideWhenFullscreenShowed = EditorGUILayout.Toggle("Hide When Fullscreen Showed", instance.isHideWhenFullscreenShowed);
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ADMOB_Android"), new GUIContent("Android Unit IDs"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ADMOB_IOS"), new GUIContent("iOS Unit IDs"), true);
            }
#endif
            #endregion
        }
    }
}
