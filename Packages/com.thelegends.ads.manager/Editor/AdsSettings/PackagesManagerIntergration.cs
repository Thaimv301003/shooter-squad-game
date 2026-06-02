using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// Manages scripting define symbols and package manifest dependencies
    /// for Ads Manager optional dependencies (Admob, Max, Firebase, AppsFlyer).
    ///
    /// [InitializeOnLoad] causes the static constructor to run every time Unity
    /// finishes a script compilation (including the very first compile after
    /// importing this package), so optional dependencies are always kept in sync
    /// without any manual action from the user.
    /// </summary>
    [InitializeOnLoad]
    public class PackagesManagerIntergration
    {
        // ─── Build targets we care about ────────────────────────────────────────
        protected static readonly NamedBuildTarget[] targetGroups =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS
        };

        // ─── Auto-run on every compilation ──────────────────────────────────────
        static PackagesManagerIntergration()
        {
            // Delay one frame so the AssetDatabase is fully initialised.
            EditorApplication.delayCall += OnAfterCompilation;
        }

        private static void OnAfterCompilation()
        {
            try
            {
                UpdateManifest();
            }
            catch (Exception e)
            {
                // Non-fatal: never break the editor because of our auto-update.
                Debug.LogWarning($"[AdsManager] Auto manifest sync failed (non-critical): {e.Message}");
            }
        }

        // ─── Define Symbols ──────────────────────────────────────────────────────
        public static List<string> GetDefinesList(NamedBuildTarget target)
        {
            return new List<string>(
                PlayerSettings.GetScriptingDefineSymbols(target).Split(';'));
        }

        public static bool IsSymbolEnabled(string defineName)
        {
            bool android = false;
            bool ios = false;

            foreach (var target in targetGroups)
            {
                var defines = GetDefinesList(target);
                if (!defines.Contains(defineName)) continue;

                if (target == NamedBuildTarget.Android) android = true;
                if (target == NamedBuildTarget.iOS) ios = true;
            }

            return android && ios;
        }

        public static void SetSymbolEnabled(string defineName, bool enable)
        {
            // Special rule: disabling USE_ADMOB must also disable USE_ADMOB_NATIVE_UNITY.
            if (defineName == "USE_ADMOB" && !enable)
            {
                SetSymbolEnabledInternal("USE_ADMOB_NATIVE_UNITY", false);

                var adsSettings = AdsSettings.Instance;
                if (adsSettings != null)
                {
                    adsSettings.isUseNativeUnity = false;
                    EditorUtility.SetDirty(adsSettings);
                }
            }

            SetSymbolEnabledInternal(defineName, enable);
        }

        private static void SetSymbolEnabledInternal(string defineName, bool enable)
        {
            foreach (var target in targetGroups)
            {
                var defines = GetDefinesList(target);
                bool updated = false;

                if (enable)
                {
                    if (defines.Contains(defineName)) continue;
                    defines.Add(defineName);
                    updated = true;
                }
                else
                {
                    if (!defines.Contains(defineName)) continue;
                    while (defines.Contains(defineName))
                        defines.Remove(defineName);
                    updated = true;
                }

                if (updated)
                {
                    PlayerSettings.SetScriptingDefineSymbols(
                        target, string.Join(";", defines.ToArray()));
                }
            }
        }

        // ─── package.json optional-dependencies reader ───────────────────────────
        /// <summary>
        /// Locates the Ads Manager package.json file and returns the version string
        /// listed under "optionalDependencies" for <paramref name="packageName"/>.
        /// Returns null when the package or key cannot be found.
        /// </summary>
        public static string GetOptionalDependencyVersion(string packageName)
        {
            string packageJsonPath = FindAdsManagerPackageJsonPath();
            if (packageJsonPath == null) return null;

            string json = File.ReadAllText(packageJsonPath);

            // Locate the optionalDependencies block using a simple brace-scan so we
            // don't need a JSON library (keeping the editor dependency-free).
            int blockStart = json.IndexOf("\"optionalDependencies\"", StringComparison.Ordinal);
            if (blockStart < 0) return null;

            int braceOpen = json.IndexOf('{', blockStart);
            if (braceOpen < 0) return null;

            int depth = 0;
            int braceClose = -1;
            for (int i = braceOpen; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') { depth--; if (depth == 0) { braceClose = i; break; } }
            }
            if (braceClose < 0) return null;

            string block = json.Substring(braceOpen, braceClose - braceOpen + 1);

            // Extract version string: "packageName": "x.y.z"
            string escaped = packageName.Replace(".", "\\.");
            var match = Regex.Match(block,
                $"\"{escaped}\"\\s*:\\s*\"([^\"]+)\"");

            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Finds the package.json of the Ads Manager package by walking upward from
        /// this script's directory, falling back to a broad search in Library/PackageCache.
        /// </summary>
        private static string FindAdsManagerPackageJsonPath()
        {
            // Strategy 1: walk upward from this source file's directory.
            // When the package is installed from disk/git, the Editor scripts live inside
            // the package folder, so we just need to climb until we find package.json.
            try
            {
                // __FILE__ equivalent: use the MonoScript approach via reflection.
                // Instead, rely on the known relative structure:
                //   Packages/com.thelegends.ads.manager/Editor/AdsSettings/<thisFile>.cs
                // Application.dataPath => <project>/Assets
                string dataPath = Application.dataPath;
                string projectRoot = Path.GetDirectoryName(dataPath);
                string candidate = Path.Combine(projectRoot,
                    "Packages", "com.thelegends.ads.manager", "package.json");
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* ignore */ }

            // Strategy 2: PackageInfo for this assembly.
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    Assembly.GetExecutingAssembly());
                if (info != null)
                {
                    string candidate = Path.Combine(info.resolvedPath, "package.json");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch { /* ignore */ }

            // Strategy 3: search Library/PackageCache.
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string cacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
                if (Directory.Exists(cacheDir))
                {
                    foreach (var dir in Directory.GetDirectories(cacheDir, "com.thelegends.ads.manager*"))
                    {
                        string candidate = Path.Combine(dir, "package.json");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            catch { /* ignore */ }

            Debug.LogWarning("[AdsManager] Could not locate package.json for com.thelegends.ads.manager.");
            return null;
        }

        // ─── SemVer helpers ──────────────────────────────────────────────────────
        /// <summary>
        /// Returns true when <paramref name="required"/> is strictly greater than
        /// <paramref name="current"/>. Invalid strings are treated as 0.0.0.
        /// </summary>
        private static bool IsVersionRequired(string current, string required)
        {
            if (!TryParseVersion(current, out var cur)) cur = new Version(0, 0, 0);
            if (!TryParseVersion(required, out var req)) return false;
            return req > cur;
        }

        private static bool TryParseVersion(string raw, out Version result)
        {
            // Strip optional leading 'v' or git metadata after '+'.
            if (raw == null) { result = null; return false; }
            raw = raw.Split('+')[0].TrimStart('v');
            return Version.TryParse(raw, out result);
        }

        // ─── manifest.json helpers ────────────────────────────────────────────────
        private static string GetProjectRootPath()
        {
            string dataPath = Application.dataPath;
            string projectRoot = Path.GetDirectoryName(dataPath);

            if (File.Exists(Path.Combine(projectRoot, "Packages", "manifest.json")))
                return projectRoot;

            string currentDir = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(currentDir, "Packages", "manifest.json")))
                return currentDir;

            var dir = new DirectoryInfo(dataPath);
            while (dir?.Parent != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Packages", "manifest.json")))
                    return dir.FullName;
                dir = dir.Parent;
            }

            Debug.LogWarning("[AdsManager] Could not locate project root (manifest.json), using fallback.");
            return Path.GetDirectoryName(Application.dataPath);
        }

        /// <summary>
        /// Extracts the current version string of <paramref name="packageName"/> from
        /// the manifest content. Returns null if not present.
        /// </summary>
        /// <summary>
        /// Automatically syncs scripting define symbols with the current AdsSettings.
        /// </summary>
        private static void SyncSymbolsFromSettings(AdsSettings settings)
        {
            SetSymbolEnabled("USE_IRON", settings.showIRON);
            SetSymbolEnabled("USE_MAX", settings.showMAX);
            SetSymbolEnabled("USE_ADMOB", settings.showADMOB);
            SetSymbolEnabled("USE_FIREBASE", settings.useFirebase);
            SetSymbolEnabled("USE_APPSFLYER", settings.useAppsFlyer);
            SetSymbolEnabled("USE_DATABUCKETS", settings.useDatabuckets);
            SetSymbolEnabled("USE_FACEBOOK", settings.useFacebook);

            bool shouldEnableNativeUnity = settings.showADMOB && settings.isUseNativeUnity;
            SetSymbolEnabled("USE_ADMOB_NATIVE_UNITY", shouldEnableNativeUnity);

            bool shouldEnableHideWhenFullscreen = settings.showADMOB && settings.isHideWhenFullscreenShowed;
            SetSymbolEnabled("HIDE_WHEN_FULLSCREEN_SHOWED", shouldEnableHideWhenFullscreen);
        }

        private static string ExtractVersionFromManifest(string manifestContent, string packageName)
        {
            string escaped = packageName.Replace(".", "\\.");
            var match = Regex.Match(manifestContent,
                $"\"{escaped}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string RemovePackageFromManifest(string manifestContent, string packageName)
        {
            string escaped = packageName.Replace(".", "\\.");

            // Pattern 1: trailing comma
            string p1 = $"\\s*\"{escaped}\"\\s*:\\s*\"[^\"]*\"\\s*,";
            if (Regex.IsMatch(manifestContent, p1))
                return Regex.Replace(manifestContent, p1, "");

            // Pattern 2: leading comma
            string p2 = $",\\s*\"{escaped}\"\\s*:\\s*\"[^\"]*\"";
            if (Regex.IsMatch(manifestContent, p2))
                return Regex.Replace(manifestContent, p2, "");

            // Pattern 3: only entry
            string p3 = $"\\s*\"{escaped}\"\\s*:\\s*\"[^\"]*\"\\s*";
            return Regex.Replace(manifestContent, p3, "");
        }

        private static string CleanupJsonCommas(string manifestContent)
        {
            manifestContent = Regex.Replace(manifestContent, ",\\s*,", ",");
            manifestContent = Regex.Replace(manifestContent, ",\\s*\\}", "\n  }");
            manifestContent = Regex.Replace(manifestContent, "\\{\\s*,", "{\n");
            manifestContent = Regex.Replace(manifestContent, "\\n\\s*,\\s*\\n", "\n");
            return manifestContent;
        }

        private static string AddPackageToManifest(string manifestContent, string packageName, string version)
        {
            return Regex.Replace(manifestContent,
                "(\"dependencies\"\\s*:\\s*\\{)",
                $"$1\n    \"{packageName}\": \"{version}\",");
        }

        private static string UpdateVersionInManifest(string manifestContent, string packageName, string newVersion)
        {
            string escaped = packageName.Replace(".", "\\.");
            return Regex.Replace(manifestContent,
                $"(\"{escaped}\"\\s*:\\s*)\"[^\"]+\"",
                $"$1\"{newVersion}\"");
        }

        // ─── Main entry point ────────────────────────────────────────────────────
        /// <summary>
        /// Synchronises Packages/manifest.json with the current AdsSettings
        /// configuration. This is called automatically on every compilation
        /// (via InitializeOnLoad) and manually when the user presses SAVE.
        /// </summary>
        public static void UpdateManifest()
        {
            var settings = AdsSettings.Instance;
            if (settings == null)
            {
                // AdsSettings asset doesn't exist yet – nothing to sync.
                return;
            }

            // Sync Symbols first to ensure code matches settings
            SyncSymbolsFromSettings(settings);

            string projectRoot = GetProjectRootPath();
            string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");

            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[AdsManager] manifest.json not found at: " + manifestPath);
                return;
            }

            string original = File.ReadAllText(manifestPath);
            string content = original;
            bool changed = false;

            // ── Build the list of packages we need to handle ──────────────────
            // Each entry: (packageName, enabledByUser)
            var packages = new List<(string name, bool enabled)>
            {
                ("com.google.ads.mobile",            settings.showADMOB),
                ("com.applovin.mediation.ads",        settings.showMAX),
                ("com.thelegends.firebase.manager",   settings.useFirebase),
                ("com.thelegends.appsflyer.manager",  settings.useAppsFlyer),
                ("com.thelegends.databuckets.manager", settings.useDatabuckets),
                ("com.thelegends.facebook.manager",   settings.useFacebook),
            };

            foreach (var (name, enabled) in packages)
            {
                string required = GetOptionalDependencyVersion(name);
                bool exists = content.Contains($"\"{name}\"");

                if (enabled)
                {
                    if (!exists)
                    {
                        // Package not installed – add it with the required version.
                        if (required == null)
                        {
                            Debug.LogWarning($"[AdsManager] No optionalDependencies entry found for {name}. Skipping.");
                            continue;
                        }
                        content = AddPackageToManifest(content, name, required);
                        Debug.Log($"[AdsManager] Added {name}: {required} to manifest.");
                        changed = true;
                    }
                    else
                    {
                        // Package already installed – upgrade if our required version is higher.
                        if (required == null) continue;

                        string current = ExtractVersionFromManifest(content, name);
                        if (current != null && IsVersionRequired(current, required))
                        {
                            content = UpdateVersionInManifest(content, name, required);
                            Debug.Log($"[AdsManager] Upgraded {name}: {current} -> {required} in manifest.");
                            changed = true;
                        }
                    }
                }
                else
                {
                    // User disabled this package – remove it if present.
                    if (!exists) continue;

                    string newContent = RemovePackageFromManifest(content, name);
                    if (newContent != content)
                    {
                        content = newContent;
                        Debug.Log($"[AdsManager] Removed {name} from manifest.");
                        changed = true;
                    }
                }
            }

            // Cleanup any JSON comma artefacts from removal operations.
            content = CleanupJsonCommas(content);

            if (!changed && content == original)
            {
                // Nothing to do – avoid unnecessary file writes that would trigger
                // a Package Manager resolve cycle.
                return;
            }

            EditorApplication.LockReloadAssemblies();
            try
            {
                File.WriteAllText(manifestPath, content);
                Debug.Log("[AdsManager] manifest.json updated successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("[AdsManager] Failed to write manifest.json: " + e.Message);
                return; // Do not refresh on error.
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }

            ForcePackageManagerRefresh();
        }

        // ─── Package Manager refresh ──────────────────────────────────────────────
        private static void ForcePackageManagerRefresh()
        {
            try
            {
                Debug.Log("[AdsManager] Triggering Package Manager refresh...");
                AssetDatabase.Refresh();
                Client.Resolve();
                AssetDatabase.ImportAsset("Packages/manifest.json");

                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        Client.Resolve();

                        EditorApplication.delayCall += () =>
                        {
                            try { EditorApplication.RepaintProjectWindow(); }
                            catch (Exception ex)
                            { Debug.LogWarning("[AdsManager] Final repaint failed (non-critical): " + ex.Message); }
                        };
                    }
                    catch (Exception ex)
                    { Debug.LogWarning("[AdsManager] Delayed refresh failed (non-critical): " + ex.Message); }
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdsManager] Package Manager refresh failed: " + e.Message);
            }
        }
    }
}
