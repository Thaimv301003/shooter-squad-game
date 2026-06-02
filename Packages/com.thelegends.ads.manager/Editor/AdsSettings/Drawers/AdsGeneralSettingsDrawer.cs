using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.Ads.Editor
{
    public class AdsGeneralSettingsDrawer
    {
        public void Draw(AdsSettings instance, SerializedObject serializedObject, GUIStyle titleStyle)
        {
            EditorGUILayout.LabelField("General Settings", titleStyle);
            EditorGUILayout.Space(2);

            instance.bannerPosition = (BannerPos)EditorGUILayout.EnumPopup("Banner Position", instance.bannerPosition);
            instance.fixBannerSmallSize = EditorGUILayout.Toggle("Fix Banner Small Size 320x50", instance.fixBannerSmallSize);
            instance.autoReLoadMax = EditorGUILayout.IntField("Max Auto Reload If No Ads", instance.autoReLoadMax);
            instance.isTest = EditorGUILayout.Toggle("Is Testing", instance.isTest);

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preloadSettings"), true);
            EditorGUILayout.Space(6);
        }
    }
}
