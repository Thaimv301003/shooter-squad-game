#if UNITY_EDITOR && UNITY_IOS
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using TheLegends.Base.Ads;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// A post-build script that adds/removes GADNativeAdValidatorEnabled key in Info.plist
    /// to enable/disable the AdMob Native Ad Validator in iOS builds based on AdsSettings.
    /// </summary>
    public class ActiveAdmobNativeValidatorIOS
    {
        [PostProcessBuildAttribute(998)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToXcode)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                ConfigureNativeAdValidator(pathToXcode);
            }
        }
        
        private static void ConfigureNativeAdValidator(string pathToXcode)
        {
            try
            {
                Debug.Log("--- ActiveAdmobNativeValidatorIOS: Executing... ---");
                
                // Get path to Info.plist
                string plistPath = pathToXcode + "/Info.plist";
                
                if (!System.IO.File.Exists(plistPath))
                {
                    Debug.LogError("ActiveAdmobNativeValidatorIOS: Info.plist not found at path: " + plistPath);
                    return;
                }
                
                // Load Info.plist
                PlistDocument plist = new PlistDocument();
                plist.ReadFromFile(plistPath);
                
                PlistElementDict rootDict = plist.root;
                
                const string validatorKey = "GADNativeAdValidatorEnabled";
                
                // Check AdsSettings to determine action
                if (AdsSettings.Instance.isShowAdmobNativeValidator)
                {
                    // ENABLE validator: Set key to true
                    if (rootDict.values.ContainsKey(validatorKey))
                    {
                        // Update existing key
                        rootDict.SetBoolean(validatorKey, true);
                        Debug.Log("ActiveAdmobNativeValidatorIOS: Updated GADNativeAdValidatorEnabled to 'true' (ENABLED).");
                    }
                    else
                    {
                        // Add new key
                        rootDict.SetBoolean(validatorKey, true);
                        Debug.Log("ActiveAdmobNativeValidatorIOS: Added GADNativeAdValidatorEnabled = 'true' to ENABLE the validator.");
                    }
                }
                else
                {
                    // DISABLE validator: Set key to false
                    if (rootDict.values.ContainsKey(validatorKey))
                    {
                        // Update existing key
                        rootDict.SetBoolean(validatorKey, false);
                        Debug.Log("ActiveAdmobNativeValidatorIOS: Updated GADNativeAdValidatorEnabled to 'false' (DISABLED).");
                    }
                    else
                    {
                        // Add new key with false value
                        rootDict.SetBoolean(validatorKey, false);
                        Debug.Log("ActiveAdmobNativeValidatorIOS: Added GADNativeAdValidatorEnabled = 'false' to DISABLE the validator.");
                    }
                }
                
                // Save modified plist
                plist.WriteToFile(plistPath);
                
                Debug.Log("--- ActiveAdmobNativeValidatorIOS: Info.plist modification successful! ---");
            }
            catch (Exception ex)
            {
                Debug.LogError("ActiveAdmobNativeValidatorIOS: Error modifying Info.plist - " + ex.Message);
                Debug.LogException(ex);
            }
        }
    }
}
#endif
