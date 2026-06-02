#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using Sirenix.OdinInspector;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if USE_ADMOB
using TheLegends.Base.Ads;
#endif
using Sirenix.OdinInspector.Editor;

namespace BFunCoreKit
{
    public class AdsData : ScriptableObject
    {
        [Title("Fetching packages version....")]
        [ProgressBar(0, 1, G = 1f, Height = 15, CustomValueStringGetter = "@_progressLabel")]
        [HideLabel]
        [ShowIf(nameof(IsProcessing))]
        public float _progress;
        private string _progressLabel = "Initializing...";

        private bool HasAllRequiredRegistries => CheckRequiredRegistries();

        [HideInInspector]
        private static readonly List<(string name, string url, string[] scopes)> RequiredRegistries = new()
    {
        ("TripSoft", "https://verdaccio.thelegends.io.vn", new[] { "com.google", "com.thelegends", "appsflyer-unity-plugin", "com.v0lt.editor-attributes", "com.pimdewitte.unitymainthreaddispatcher"}),
        ("OpenUPM", "https://package.openupm.com", new[] { "com.annulusgames"})
    };

        private bool CheckRequiredRegistries()
        {
            try
            {
                string manifestPath = Path.GetFullPath("Packages/manifest.json");
                if (!File.Exists(manifestPath)) return false;

                var manifestJson = JObject.Parse(File.ReadAllText(manifestPath));
                var scopedRegistries = manifestJson["scopedRegistries"] as JArray;
                if (scopedRegistries == null) return false;

                foreach (var req in RequiredRegistries)
                {
                    bool found = scopedRegistries.Any(r =>
                        r["name"]?.ToString() == req.name &&
                        r["url"]?.ToString() == req.url
                    );
                    if (!found) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        [Button("Install Required Registries", Icon = SdfIconType.CloudDownload)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [ShowIf("@!HasAllRequiredRegistries && !IsProcessing")]
        private async void InstallRegistries()
        {
            try
            {
                string manifestPath = Path.GetFullPath("Packages/manifest.json");
                if (!File.Exists(manifestPath))
                {
                    Debug.LogError("❌ manifest.json not found!");
                    return;
                }

                var manifestJson = JObject.Parse(await File.ReadAllTextAsync(manifestPath));
                var scopedRegistries = (JArray?)manifestJson["scopedRegistries"] ?? new JArray();

                // Xóa bản cũ (nếu có) để tránh duplicate
                scopedRegistries = new JArray(scopedRegistries
                    .Where(r => r["name"]?.ToString() != "TripSoft" && r["name"]?.ToString() != "OpenUPM"));

                // Thêm các registry cần thiết
                var tripSoft = new JObject
                {
                    ["name"] = "TripSoft",
                    ["url"] = "https://verdaccio.thelegends.io.vn",
                    ["scopes"] = new JArray
            {
                "com.google",
                "com.thelegends",
                "appsflyer-unity-plugin",
                "com.v0lt.editor-attributes",
                "com.pimdewitte.unitymainthreaddispatcher"
            }
                };

                var openUpm = new JObject
                {
                    ["name"] = "OpenUPM",
                    ["url"] = "https://package.openupm.com",
                    ["scopes"] = new JArray
            {
                "com.annulusgames",
            }
                };

                scopedRegistries.Add(tripSoft);
                scopedRegistries.Add(openUpm);
                manifestJson["scopedRegistries"] = scopedRegistries;

                await File.WriteAllTextAsync(manifestPath, manifestJson.ToString(Formatting.Indented));

                Debug.Log("✅ Required registries installed successfully!");

                // 🔄 Force Unity to refresh and reload registries
                AssetDatabase.Refresh();
                EditorApplication.delayCall += async () =>
                {
                    UnityEditor.PackageManager.Client.Resolve();
                    // ⏳ Chờ Unity resolve xong 1 chút
                    await Task.Delay(3000);

                    // 🔄 Gọi lại RefreshAll() để cập nhật giao diện & trạng thái
                    var adsData = Resources.FindObjectsOfTypeAll<AdsData>().FirstOrDefault();
                    if (adsData != null)
                        await adsData.RefreshAll();
                    Debug.Log("🔁 Unity Package Manager refreshed and reloaded registries.");
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to install registries: {e.Message}");
            }
        }
        // === END REGISTRY CHECK SECTION ===


        [ShowIf(nameof(HasAllRequiredRegistries))] // only show if registry valid
        [BoxGroup("Batch Actions", ShowLabel = false)]
        [HorizontalGroup("Batch Actions/Buttons")]
        [Button("Install All Missing", Icon = SdfIconType.BoxSeam)]
        [GUIColor(0, 1, 0.4f)]
        [DisableIf(nameof(IsProcessing))]
        [PropertyOrder(1)]
        private void StartInstallAll()
        {
            var packagesToInstall = Packages.Where(p => !p.IsInstalled && !string.IsNullOrEmpty(p.SelectedVersion)).ToList();
            if (packagesToInstall.Count == 0) { Debug.Log("✅ All required packages are already installed."); return; }
            _ = ModifyManifestAndResolve(packagesToInstall, null, true);
        }

        [ShowIf(nameof(HasAllRequiredRegistries))]
        [HorizontalGroup("Batch Actions/Buttons")]
        [Button("Remove All Installed", Icon = SdfIconType.Trash)]
        [GUIColor(1f, 0.4f, 0.4f)]
        [DisableIf(nameof(IsProcessing))]
        [PropertyOrder(1)]
        private void StartRemoveAll()
        {
            var packagesToRemove = Packages.Where(p => p.IsInstalled).ToList();
            if (packagesToRemove.Count == 0) { Debug.Log("✅ No packages from this list are currently installed."); return; }
            _ = ModifyManifestAndResolve(null, packagesToRemove, true);
        }

        [ShowIf(nameof(HasAllRequiredRegistries))]
        [HorizontalGroup("Batch Actions/Buttons")]
        [Button("Fetch All", Icon = SdfIconType.ArrowRepeat)]
        [GUIColor(1f, 1f, 0.4f)]
        [PropertyOrder(1)]
        private void StartRefreshAll()
        {
            _isProcessing = false;
            _ = RefreshAll();
        }

        [ShowIf(nameof(HasAllRequiredRegistries))]
        [TableList(DrawScrollView = true, IsReadOnly = true, ScrollViewHeight = 250)]
        [ListDrawerSettings(IsReadOnly = true, HideAddButton = true, HideRemoveButton = true, DraggableItems = false, Expanded = true, ShowPaging = false)]
        [HideReferenceObjectPicker]
        [PropertyOrder(1)]
        public List<PackageInfo> Packages = new();

        [Space(20)]

#if USE_ADMOB
    [SerializeField] [PropertyOrder(2)] [InlineEditor(InlineEditorObjectFieldModes.Hidden)] AdsSettings adsSettings;
#endif
        private bool _isProcessing;
        public bool IsProcessing => _isProcessing;

        private const string ManifestOpKey = "AdsData_ManifestOperation";
        private const string SinglePackageKey = "AdsData_SinglePackageRefresh";

        public async void InitializeAndRefresh()
        {
            if (!HasAllRequiredRegistries) return;
            _isProcessing = false;
            if (Packages == null || Packages.Count == 0)
                Packages = PackageInfo.DefaultList();

            await Task.Delay(200);

            if (SessionState.GetBool(ManifestOpKey, false))
            {
                SessionState.EraseBool(ManifestOpKey);
                string singlePackage = SessionState.GetString(SinglePackageKey, "");
                if (!string.IsNullOrEmpty(singlePackage))
                {
                    SessionState.EraseString(SinglePackageKey);
                    await RefreshSinglePackage(singlePackage, true);
                }
                else
                {
                    await RefreshAll();
                }
            }
            else
            {
                await RefreshAll();
            }
        }

        public async Task RefreshAll()
        {
            if (_isProcessing) return;
#if USE_ADMOB
        adsSettings = AssetDatabase.LoadAssetAtPath<AdsSettings>("Assets/Tripsoft/AdsManager/Resources/AdsSettingsAsset.asset");
#endif
            _isProcessing = true;

            _progress = 0f;
            _progressLabel = "Starting full refresh...";
            RepaintUI();

            try
            {
                string manifestPath = Path.GetFullPath("Packages/manifest.json");
                if (!File.Exists(manifestPath)) { _progressLabel = "❌ manifest.json not found!"; return; }

                var manifestJson = JObject.Parse(await File.ReadAllTextAsync(manifestPath));
                var dependencies = (JObject)manifestJson["dependencies"];

                for (int i = 0; i < Packages.Count; i++)
                {
                    var p = Packages[i];
                    p.IsLocked = true;
                    _progress = (float)(i + 1) / Packages.Count;
                    _progressLabel = $"[{i + 1}/{Packages.Count}] Refreshing {p.DisplayName}...";
                    RepaintUI();

                    p.CheckInstalled(dependencies);
                    await p.FetchVersionsAsync();

                    await Task.Delay(50);
                }
                _progressLabel = "✅ All packages refreshed!";
            }
            catch (Exception e) { _progressLabel = $"❌ Refresh failed: {e.Message}"; Debug.LogError(e); }
            finally
            {
                await Task.Delay(2000);
                foreach (var pkg in Packages) pkg.IsLocked = false;
                _isProcessing = false;
                RepaintUI();
            }
        }

        public async Task RefreshSinglePackage(string packageName, bool fetchVersions)
        {
            var package = Packages.FirstOrDefault(p => p.PackageName == packageName);
            if (package == null) return;

            if (_isProcessing) return;
            _isProcessing = true;
            _progressLabel = $"Refreshing {package.DisplayName}...";
            _progress = 1f;
            RepaintUI();

            try
            {
                string manifestPath = Path.GetFullPath("Packages/manifest.json");
                var manifestJson = JObject.Parse(await File.ReadAllTextAsync(manifestPath));
                var dependencies = (JObject)manifestJson["dependencies"];

                package.IsLocked = true;
                package.CheckInstalled(dependencies);
                if (fetchVersions) await package.FetchVersionsAsync();
            }
            catch (Exception e) { Debug.LogError($"Failed to refresh {packageName}: {e.Message}"); }
            finally
            {
                await Task.Delay(1000);
                package.IsLocked = false;
                _isProcessing = false;
                RepaintUI();
            }
        }

        public async Task ModifyManifestAndResolve(List<PackageInfo> toAdd, List<PackageInfo> toRemove, bool isBatch)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _progress = 1f;
            _progressLabel = "Modifying manifest.json...";
            RepaintUI();

            try
            {
                SessionState.SetBool(ManifestOpKey, true);
                if (!isBatch)
                {
                    string pkgName = toAdd?.FirstOrDefault()?.PackageName ?? toRemove?.FirstOrDefault()?.PackageName;
                    if (pkgName != null) SessionState.SetString(SinglePackageKey, pkgName);
                }

                string manifestPath = Path.GetFullPath("Packages/manifest.json");
                var manifestJson = JObject.Parse(await File.ReadAllTextAsync(manifestPath));
                var dependencies = (JObject)manifestJson["dependencies"];
                bool changed = false;

                if (toAdd != null) foreach (var pkg in toAdd) { dependencies[pkg.PackageName] = pkg.SelectedVersion; changed = true; }
                if (toRemove != null) foreach (var pkg in toRemove) { dependencies.Property(pkg.PackageName)?.Remove(); changed = true; }

                if (changed)
                {
                    _progressLabel = "Applying changes and resolving packages...";
                    RepaintUI();
                    await File.WriteAllTextAsync(manifestPath, manifestJson.ToString(Formatting.Indented));
                    Client.Resolve();
                }
                else
                {
                    _isProcessing = false; SessionState.EraseBool(ManifestOpKey); RepaintUI();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to modify manifest.json: {e.Message}");
                _isProcessing = false; SessionState.EraseBool(ManifestOpKey); RepaintUI();
            }
        }

        private void RepaintUI()
        {
            EditorApplication.delayCall += () =>
            {
                // ✅ Chỉ repaint cửa sổ hiện có, không mở mới
                foreach (var window in Resources.FindObjectsOfTypeAll<OdinEditorWindow>())
                {
                    window.Repaint();
                }
            };
        }
        private void OnValidate() { if (Packages == null || Packages.Count == 0) Packages = PackageInfo.DefaultList(); }
    }

    [Serializable]
    public class PackageInfo
    {
        [PropertyOrder(0)]
        [TableColumnWidth(80, Resizable = false)]
        [ShowInInspector, ReadOnly, HideLabel]
        [GUIColor(nameof(GetStatusColor))]
        [DisplayAsString(false)]
        private string Status => IsInstalled ? "✓ Success" : "✗ Missing";

        [PropertyOrder(1)]
        [TableColumnWidth(150)]
        [DisplayAsString(false)]
        public string DisplayName;

        // [PropertyOrder(1)]
        // [TableColumnWidth(200)]
        // [DisplayAsString(false)]
        // [ReadOnly]
        [HideInInspector]
        public string PackageName;

        [PropertyOrder(3)]
        [TableColumnWidth(180)]
        [HorizontalGroup("Version Control")]
        [ValueDropdown(nameof(GetAvailableVersions))]
        [HideLabel]
        public string SelectedVersion;

        [PropertyOrder(3)]
        [HorizontalGroup("Version Control", Width = 90)]
        [Button(Icon = SdfIconType.ArrowClockwise, Name = "Refresh")]
        [DisableIf(nameof(IsLocked))]
        private void Refresh()
        {
            var parent = Resources.FindObjectsOfTypeAll<AdsData>().FirstOrDefault();
            if (parent != null)
            {
                _ = parent.RefreshSinglePackage(this.PackageName, true);
            }
        }

        [PropertyOrder(4)]
        [TableColumnWidth(60)]
        [DisplayAsString(false)]
        [ReadOnly]
        public string InstalledVersion = "—";

        [PropertyOrder(5)]
        [TableColumnWidth(120, Resizable = false)]
        [Button("@GetActionButtonText()")]
        [GUIColor(nameof(GetActionButtonColor))]
        [DisableIf(nameof(IsLocked))]
        private void ToggleAction()
        {
            var parent = Resources.FindObjectsOfTypeAll<AdsData>().FirstOrDefault();
            if (parent == null) return;
            const bool isBatch = false;
            if (IsInstalled)
            {
                if (InstalledVersion != SelectedVersion)
                    _ = parent.ModifyManifestAndResolve(new List<PackageInfo> { this }, null, isBatch);
                else
                    _ = parent.ModifyManifestAndResolve(null, new List<PackageInfo> { this }, isBatch);
            }
            else
            {
                if (string.IsNullOrEmpty(SelectedVersion)) { Debug.LogWarning($"⚠️ Please select a version for {DisplayName}"); return; }
                _ = parent.ModifyManifestAndResolve(new List<PackageInfo> { this }, null, isBatch);
            }
        }

        [HideInInspector] public bool IsInstalled;
        [HideInInspector] public bool IsLocked;

        private List<string> _availableVersions = new();
        public IEnumerable<string> GetAvailableVersions() => _availableVersions;

        private Color GetStatusColor() => IsInstalled ? Color.green : new Color(1f, 0.5f, 0.5f);
        private string GetActionButtonText() { if (!IsInstalled) return "Install"; if (IsInstalled && InstalledVersion != SelectedVersion) return "Update"; return "Remove"; }
        private Color GetActionButtonColor() { if (!IsInstalled) return Color.white; if (IsInstalled && InstalledVersion != SelectedVersion) return new Color(1f, 0.8f, 0.4f); return new Color(1f, 0.5f, 0.5f); }

        public void CheckInstalled(JObject dependencies)
        {
            if (dependencies.TryGetValue(PackageName, out JToken versionToken))
            {
                IsInstalled = true;
                InstalledVersion = versionToken.ToString();
            }
            else
            {
                IsInstalled = false;
                InstalledVersion = "—";
            }
        }

        public async Task FetchVersionsAsync()
        {
            IsLocked = true;
            try
            {
                var registry = GetRegistry(PackageName);
                if (string.IsNullOrEmpty(registry)) { _availableVersions.Clear(); return; }
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var text = await client.GetStringAsync($"{registry}/{PackageName}");
                var json = JObject.Parse(text);
                var versions = json["versions"]?.ToObject<Dictionary<string, object>>()?.Keys?.ToList();
                if (versions != null)
                {
                    _availableVersions = versions.OrderBy(v => v, new VersionComparer()).ToList();
                    if ((string.IsNullOrEmpty(SelectedVersion) || !_availableVersions.Contains(SelectedVersion)) && _availableVersions.Any())
                        SelectedVersion = _availableVersions.First();
                }
            }
            catch (Exception e) { Debug.LogWarning($"⚠️ Could not fetch versions for {PackageName}: {e.Message}"); }
            finally { IsLocked = false; }
        }

        private static string GetRegistry(string name)
        {
            if (name.StartsWith("com.google") || name.StartsWith("com.thelegends") || name.StartsWith("appsflyer") || name.StartsWith("com.v0lt") || name.StartsWith("com.pimdewitte"))
                return "https://verdaccio.thelegends.io.vn";
            if (name.StartsWith("com.annulusgames"))
                return "https://package.openupm.com";
            return null;
        }

        public class VersionComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                string Clean(string v) => v.Split('-')[0];
                if (Version.TryParse(Clean(x), out var v1) && Version.TryParse(Clean(y), out var v2))
                    return v2.CompareTo(v1);
                return string.Compare(y, x, StringComparison.Ordinal);
            }
        }

        public static List<PackageInfo> DefaultList() => new()
    {
        new PackageInfo { DisplayName = "AppsFlyer Manager", PackageName = "com.thelegends.appsflyer.manager" },
        new PackageInfo { DisplayName = "EditorAttributes", PackageName = "com.v0lt.editor-attributes" },
        new PackageInfo { DisplayName = "Ads Manager", PackageName = "com.thelegends.ads.manager" },
        new PackageInfo { DisplayName = "Firebase Manager", PackageName = "com.thelegends.firebase.manager" },
        new PackageInfo { DisplayName = "Thread Disapatcher", PackageName = "com.pimdewitte.unitymainthreaddispatcher" },
        new PackageInfo { DisplayName = "Firebase Core", PackageName = "com.google.firebase.app" },
        new PackageInfo { DisplayName = "Firebase Analytics", PackageName = "com.google.firebase.analytics" },
        new PackageInfo { DisplayName = "Firebase Remote Config", PackageName = "com.google.firebase.remote-config" },
        new PackageInfo { DisplayName = "Firebase Crashlytics", PackageName = "com.google.firebase.crashlytics" },
        new PackageInfo { DisplayName = "Google Mobile Ads", PackageName = "com.google.ads.mobile" },
        new PackageInfo { DisplayName = "Google Mobile Ads AppLovin", PackageName = "com.google.ads.mobile.mediation.applovin" },
        new PackageInfo { DisplayName = "Google Mobile Ads IronSource", PackageName = "com.google.ads.mobile.mediation.ironsource" },
        new PackageInfo { DisplayName = "Google Mobile Ads Liftoff", PackageName = "com.google.ads.mobile.mediation.liftoffmonetize" },
        new PackageInfo { DisplayName = "Google Mobile Ads Meta", PackageName = "com.google.ads.mobile.mediation.metaaudiencenetwork" },
        new PackageInfo { DisplayName = "Google Mobile Ads Mintegral", PackageName = "com.google.ads.mobile.mediation.mintegral" },
        new PackageInfo { DisplayName = "Google Mobile Ads Pangle", PackageName = "com.google.ads.mobile.mediation.pangle" },
        new PackageInfo { DisplayName = "Google Mobile Ads Unity", PackageName = "com.google.ads.mobile.mediation.unity" },
        new PackageInfo { DisplayName = "Google Android App Bundle", PackageName = "com.google.android.appbundle" },
        new PackageInfo { DisplayName = "Google Play Common", PackageName = "com.google.play.common" },
        new PackageInfo { DisplayName = "Google Play Core", PackageName = "com.google.play.core" },
        new PackageInfo { DisplayName = "Google Play In-app Review", PackageName = "com.google.play.review" }
    };
    }
}
#endif
