#if UNITY_EDITOR && UNITY_IOS
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using TheLegends.Base.Ads;

namespace TheLegends.Base.Ads
{
    public class DisableBitcodeiOS
    {
        [PostProcessBuildAttribute(997)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToXcode)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                DisableBitcode(pathToXcode);
            }
        }
        
        private static void DisableBitcode(string pathToXcode)
        {
            try
            {
                string projectPath = pathToXcode + "/Unity-iPhone.xcodeproj/project.pbxproj";
                PBXProject pbxProject = new PBXProject();
                pbxProject.ReadFromFile(projectPath);

                //Disabling Bitcode on all targets
                //Main
                string target = pbxProject.GetUnityMainTargetGuid();
                pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
                pbxProject.SetBuildProperty(target, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
                //Unity Tests
                target = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
                pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
                pbxProject.SetBuildProperty(target, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
                //Unity Framework
                target = pbxProject.GetUnityFrameworkTargetGuid();
                pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
                pbxProject.SetBuildProperty(target, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

                pbxProject.WriteToFile(projectPath);

                AdsManager.Instance.Log("DisableBitcode");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
#endif
