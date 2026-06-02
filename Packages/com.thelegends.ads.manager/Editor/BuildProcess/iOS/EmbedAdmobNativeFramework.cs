#if UNITY_EDITOR && UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// Post-build script to automatically embed admob_native_unity.xcframework
    /// into the Xcode project's "Frameworks, Libraries, and Embedded Content".
    /// 
    /// Compatible with Unity 2021, 2022, and Unity 6.
    /// </summary>
    public class EmbedAdmobNativeFramework
    {
        private const string XCFRAMEWORK_NAME = "admob_native_unity.xcframework";
        
        [PostProcessBuildAttribute(999)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToXcode)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                EmbedFramework(pathToXcode);
            }
        }
        
        private static void EmbedFramework(string pathToXcode)
        {
            try
            {
                Debug.Log("--- EmbedAdmobNativeFramework: Executing... ---");
                
                // Get paths
                string projectPath = PBXProject.GetPBXProjectPath(pathToXcode);
                
                // Unity copies UPM package files with package structure:
                // Frameworks/com.thelegends.ads.manager/Runtime/Plugins/iOS/admob_native_unity.xcframework
                string relativePath = Path.Combine("Frameworks", "com.thelegends.ads.manager", "Runtime", "Plugins", "iOS", XCFRAMEWORK_NAME);
                string xcframeworkPath = Path.Combine(pathToXcode, relativePath);
                
                // Check if xcframework exists
                if (!Directory.Exists(xcframeworkPath))
                {
                    Debug.LogError($"EmbedAdmobNativeFramework: XCFramework not found at: {xcframeworkPath}");
                    Debug.LogError($"Expected relative path: {relativePath}");
                    Debug.LogError("Make sure admob_native_unity.xcframework is placed in Packages/com.thelegends.ads.manager/Runtime/Plugins/iOS/");
                    return;
                }
                
                Debug.Log($"EmbedAdmobNativeFramework: Found XCFramework at: {xcframeworkPath}");
                Debug.Log($"EmbedAdmobNativeFramework: Relative path: {relativePath}");
                
                // Load PBX Project
                PBXProject project = new PBXProject();
                project.ReadFromFile(projectPath);
                
                // Get target GUIDs (compatible with Unity 2021, 2022, Unity 6)
                string mainTargetGuid = GetMainTargetGuid(project);
                string frameworkTargetGuid = GetFrameworkTargetGuid(project);
                
                if (string.IsNullOrEmpty(mainTargetGuid))
                {
                    Debug.LogError("EmbedAdmobNativeFramework: Could not find main target GUID");
                    return;
                }
                
                Debug.Log($"EmbedAdmobNativeFramework: Main Target GUID: {mainTargetGuid}");
                Debug.Log($"EmbedAdmobNativeFramework: Framework Target GUID: {frameworkTargetGuid}");
                
                // Remove if already exists (to avoid duplicates)
                string existingFileGuid = project.FindFileGuidByProjectPath(relativePath);
                if (!string.IsNullOrEmpty(existingFileGuid))
                {
                    Debug.Log("EmbedAdmobNativeFramework: XCFramework already exists, removing old reference...");
                    project.RemoveFile(existingFileGuid);
                }
                
                // Add xcframework file to project
                string fileGuid = project.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                
                // Add to main target's frameworks build phase
                project.AddFileToBuild(mainTargetGuid, fileGuid);
                
                // CRITICAL: Add to "Embed Frameworks" build phase with CodeSignOnCopy
                // This is equivalent to "Embed & Sign" in Xcode UI
                project.AddFileToEmbedFrameworks(mainTargetGuid, fileGuid);
                
                // Also add to UnityFramework target if it exists (Unity 2019.3+)
                if (!string.IsNullOrEmpty(frameworkTargetGuid))
                {
                    project.AddFileToBuild(frameworkTargetGuid, fileGuid);
                }
                
                // Set framework search paths - add the directory containing the xcframework
                string frameworkSearchPath = "$(PROJECT_DIR)/" + Path.GetDirectoryName(relativePath).Replace("\\", "/");
                project.AddBuildProperty(mainTargetGuid, "FRAMEWORK_SEARCH_PATHS", frameworkSearchPath);
                
                if (!string.IsNullOrEmpty(frameworkTargetGuid))
                {
                    project.AddBuildProperty(frameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", frameworkSearchPath);
                }
                
                // Write changes to file
                project.WriteToFile(projectPath);
                
                Debug.Log($"--- EmbedAdmobNativeFramework: Successfully embedded {XCFRAMEWORK_NAME}! ---");
                Debug.Log("XCFramework will appear in 'Frameworks, Libraries, and Embedded Content' with 'Embed & Sign'");
            }
            catch (Exception ex)
            {
                Debug.LogError("EmbedAdmobNativeFramework: Error embedding xcframework - " + ex.Message);
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Get main target GUID - compatible with all Unity versions
        /// </summary>
        private static string GetMainTargetGuid(PBXProject project)
        {
            // Try Unity 2019.3+ method first (returns main app target)
            try
            {
                string guid = project.GetUnityMainTargetGuid();
                if (!string.IsNullOrEmpty(guid))
                {
                    return guid;
                }
            }
            catch
            {
                // Method might not exist in older Unity versions
            }
            
            // Fallback: Try getting target by name
            try
            {
                string targetName = "Unity-iPhone";
                string guid = project.TargetGuidByName(targetName);
                if (!string.IsNullOrEmpty(guid))
                {
                    return guid;
                }
            }
            catch
            {
                // Continue to next fallback
            }
            
            // Last resort: Try getting first target
            try
            {
#if UNITY_2021_1_OR_NEWER
                string guid = project.GetUnityMainTargetGuid();
                return guid;
#else
                return project.TargetGuidByName("Unity-iPhone");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to get main target GUID: " + ex.Message);
                return null;
            }
        }
        
        /// <summary>
        /// Get UnityFramework target GUID - returns null if not found (Unity 2019.2 and below)
        /// </summary>
        private static string GetFrameworkTargetGuid(PBXProject project)
        {
            try
            {
                // Unity 2019.3+ has separate UnityFramework target
                string guid = project.GetUnityFrameworkTargetGuid();
                return guid;
            }
            catch
            {
                // Unity 2019.2 and below don't have UnityFramework target
                return null;
            }
        }
    }
}
#endif
