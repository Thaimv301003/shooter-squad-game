#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Text.RegularExpressions;
using Google.Apis.Download;
using Sirenix.Utilities.Editor;

[CreateAssetMenu(fileName = "PackageManagerSettings", menuName = "BFun/Package Manager Settings")]
public class PackageManagerSettings : ScriptableObject
{
    // ==========================================================================================
    // CÁC LỚP LỒNG (NESTED CLASSES)
    // ==========================================================================================
    [System.Serializable]
    public class UnityPackageInfo
    {
        [PropertyOrder(0)]
        [TableColumnWidth(325)]
        [DisplayAsString(FontSize = 12), GUIColor("GetPackageNameColor"), HideLabel]
        public string packageName;

        [PropertyOrder(1)]
        [TableColumnWidth(100)]
        [DisplayAsString(FontSize = 11), GUIColor("GetInfoColor"), HideLabel]
        public string packageSize;

        [PropertyOrder(2)]
        [TableColumnWidth(150)]
        [DisplayAsString(FontSize = 11), GUIColor("GetInfoColor"), HideLabel]
        public string uploaderName;

        [PropertyOrder(3)]
        [TableColumnWidth(120, Resizable = false)]
        [VerticalGroup("ActionsGroup")]
        [HideIf("isInstalled")]
        [Button("Install", ButtonSizes.Medium)]
        private async void Install() => await InstallOrUpdate();

        [PropertyOrder(3)]
        [VerticalGroup("ActionsGroup")]
        [ShowIf("isInstalled")]
        [DisplayAsString(FontSize = 12, Alignment = TextAlignment.Center), GUIColor(0.4f, 1f, 0.4f)]
        [HideLabel]
        public string InstalledText = "✓ Installed";

        [HideInInspector] public string fileId;
        [HideInInspector] public bool isInstalled;
        [HideInInspector] public string uploadDate;

        private PackageManagerSettings _owner;

        // --- FIX LỖI "Owner settings are missing" ---
        // Hàm này giúp gán lại chủ sở hữu sau khi Unity Reload Scripts
        public void SetOwner(PackageManagerSettings owner) => _owner = owner;

        private Color GetPackageNameColor() => this.isInstalled ? new Color(0.4f, 1f, 0.4f) : Color.white;
        private Color GetInfoColor() => isInstalled ? new Color(0.7f, 0.7f, 0.7f, 1f) : new Color(0.7f, 0.7f, 0.7f, 0.6f);

        public void RefreshState(PackageManagerSettings owner, string basePackageName, HashSet<string> normalizedProjectFolders)
        {
            _owner = owner;
            string normalizedPackageName = GetNormalizedName(basePackageName);
            this.isInstalled = normalizedProjectFolders.Contains(normalizedPackageName);
        }

        private async Task InstallOrUpdate()
        {
            if (string.IsNullOrEmpty(fileId) || _owner == null)
            {
                // Thử tự cứu nếu _owner null (dù đã fix ở UpdateSearch nhưng phòng hờ)
                Debug.LogError("Download failed: Owner settings are missing. Please try clicking 'Refresh' first if this persists.");
                return;
            }

            await _owner.EnsureDriveServiceAsync();

            if (_owner._driveService == null)
            {
                Debug.LogError("Download failed: Could not initialize Google Drive Service. Please check credentials and internet connection.");
                return;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), packageName + ".unitypackage");
            try
            {
                EditorUtility.DisplayProgressBar($"Downloading {packageName}", "Starting download...", 0f);
                var request = _owner._driveService.Files.Get(fileId);
                long bytesDownloaded = 0;
                DownloadStatus downloadStatus = DownloadStatus.NotStarted;
                Exception downloadException = null;

                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                    {
                        bytesDownloaded = progress.BytesDownloaded;
                        downloadStatus = progress.Status;
                        downloadException = progress.Exception;
                    };
                    var downloadTask = request.DownloadAsync(fileStream);
                    await downloadTask;
                }
                if (downloadStatus == DownloadStatus.Failed) { throw downloadException ?? new Exception("Download failed."); }

                SessionState.SetBool("BFunPackageManager_ShouldRefresh", true);
                AssetDatabase.ImportPackage(tempPath, true);
            }
            catch (Exception e) { Debug.LogError($"An error occurred: {e}"); }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (System.IO.File.Exists(tempPath)) { try { System.IO.File.Delete(tempPath); } catch { } }
            }
        }

        public static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            if (bytes == 0) return "0 " + Suffix[0];
            int i = (int)Mathf.Floor((float)System.Math.Log(bytes, 1024));
            return string.Format("{0:0.#} {1}", bytes / System.Math.Pow(1024, i), Suffix[i]);
        }
    }

    [HideInInspector]
    private GoogleDriveSettings _driveApiSettings;
    [NonSerialized, HideInInspector]
    public DriveService _driveService;

    private void AutoLoadApiSettings()
    {
        const string settingsPath = "Assets/BFunCore/BFunPackage/GoogleDriveSettings.asset";
        _driveApiSettings = AssetDatabase.LoadAssetAtPath<GoogleDriveSettings>(settingsPath);
    }

    public async Task EnsureDriveServiceAsync()
    {
        if (_driveService != null) return;

        if (_driveApiSettings == null) AutoLoadApiSettings();
        if (_driveApiSettings == null) { Debug.LogError("Cannot initialize Drive Service: Missing GoogleDriveSetting.asset."); return; }
        if (string.IsNullOrEmpty(_driveApiSettings.googleClientId) || string.IsNullOrEmpty(_driveApiSettings.googleClientSecret)) { Debug.LogError("Cannot initialize Drive Service: Client ID or Secret is missing in GoogleDriveSetting.asset."); return; }

        try
        {
            EditorUtility.DisplayProgressBar("Google Drive", "Authenticating...", 0.1f);
            string credPath = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)), "GoogleDriveAPICredentials.json");
            ClientSecrets clientSecrets = new ClientSecrets { ClientId = _driveApiSettings.googleClientId, ClientSecret = _driveApiSettings.googleClientSecret };
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets, new[] { DriveService.Scope.Drive }, "user", System.Threading.CancellationToken.None, new FileDataStore(credPath, true));
            _driveService = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = "BFun Package Manager" });
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to re-initialize Google Drive Service: " + ex.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public async void RefreshDrivePackages()
    {
        await EnsureDriveServiceAsync();
        if (_driveService == null) { Debug.LogError("Could not fetch from drive, service is not available."); return; }

        bfunPackageLibary.Clear();
        EditorUtility.DisplayProgressBar("Google Drive", "Scanning Project...", 0.05f);

        var normalizedProjectFolders = new HashSet<string>(AssetDatabase.GetAllAssetPaths()
            .Where(path => AssetDatabase.IsValidFolder(path))
            .Select(path => GetNormalizedName(Path.GetFileName(path))));

        EditorUtility.DisplayProgressBar("Google Drive", "Fetching file list...", 0.5f);
        try
        {
            List<string> allFolderIds = await GetAllFolderIdsRecursivelyAsync(_driveService, _driveApiSettings.googleDriveFolderId);
            string parentQueries = string.Join(" or ", allFolderIds.Select(id => $"'{id}' in parents"));
            string finalQuery = $"({parentQueries}) and name contains '.unitypackage' and trashed = false";

            var request = _driveService.Files.List();
            request.Q = finalQuery;
            request.PageSize = 1000;
            request.Fields = "files(id, name, size, createdTime, modifiedTime, lastModifyingUser(displayName))";
            request.Corpora = "user";
            request.IncludeItemsFromAllDrives = true;
            request.SupportsAllDrives = true;
            FileList fileList = await request.ExecuteAsync();

            if (fileList?.Files != null)
            {
                foreach (var file in fileList.Files.OrderBy(f => f.Name))
                {
                    string baseName = GetBasePackageName(Path.GetFileNameWithoutExtension(file.Name));
                    var newPackage = new UnityPackageInfo();
                    newPackage.RefreshState(this, baseName, normalizedProjectFolders);
                    newPackage.packageName = Path.GetFileNameWithoutExtension(file.Name);
                    newPackage.fileId = file.Id;
                    newPackage.packageSize = UnityPackageInfo.FormatBytes(file.Size ?? 0);
                    newPackage.uploadDate = file.CreatedTime?.ToShortDateString();
                    string nameFile = file.LastModifyingUser?.DisplayName ?? "Unknown";
                    newPackage.uploaderName = string.IsNullOrEmpty(nameFile) ? "Unknown" : nameFile;
                    bfunPackageLibary.Add(newPackage);
                }
            }

            // Cập nhật list hiển thị
            UpdateSearch();
        }
        catch (System.Exception ex) { Debug.LogError("Lỗi khi kết nối Google Drive: " + ex.ToString()); }
        finally { EditorUtility.ClearProgressBar(); }
    }

    public void RefreshInstallationStatus()
    {
        var normalizedProjectFolders = new HashSet<string>(AssetDatabase.GetAllAssetPaths()
            .Where(path => AssetDatabase.IsValidFolder(path))
            .Select(path => GetNormalizedName(Path.GetFileName(path))));

        foreach (var package in bfunPackageLibary)
        {
            string baseName = GetBasePackageName(package.packageName);
            package.RefreshState(this, baseName, normalizedProjectFolders);
        }

        UpdateSearch();
    }

    [OnInspectorInit] private void CreateData() => UpdateSearch();
    private void OnEnable() => UpdateSearch();

    [HideInInspector]
    public string SearchTerm;

    [HideInInspector]
    public List<UnityPackageInfo> bfunPackageLibary = new List<UnityPackageInfo>();

    [ShowInInspector]
    [PropertyOrder(5)]
    [OnInspectorGUI("DrawCustomHeader", append: false)]
    [TableList(DrawScrollView = true, IsReadOnly = true)]
    [ListDrawerSettings(
         IsReadOnly = true,
         HideAddButton = true,
         HideRemoveButton = true,
         DraggableItems = false,
         Expanded = true,
         ShowPaging = false,
         NumberOfItemsPerPage = 20
     )]
    [HideReferenceObjectPicker]
    [HideLabel]
    [NonSerialized]
    public List<UnityPackageInfo> displayPackageLibrary = new List<UnityPackageInfo>();

    private void DrawCustomHeader()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 1. Tên bảng
        GUILayout.Label("Bfun Package Library", EditorStyles.boldLabel);

        // 2. Số lượng items (Đã thêm như yêu cầu)
        if (displayPackageLibrary != null)
        {
            GUILayout.Label($"({displayPackageLibrary.Count})", EditorStyles.centeredGreyMiniLabel);
        }

        // 3. Đẩy sang phải
        GUILayout.FlexibleSpace();

        // 4. Vẽ ô Search
        try
        {
            string newTerm = SirenixEditorGUI.SearchField(GUILayoutUtility.GetRect(200, 16), SearchTerm);
            if (newTerm != SearchTerm)
            {
                SearchTerm = newTerm;
                UpdateSearch();
            }
        }
        catch { /* Bỏ qua lỗi layout */ }

        GUILayout.EndHorizontal();
    }

    private void UpdateSearch()
    {
        if (displayPackageLibrary == null) displayPackageLibrary = new List<UnityPackageInfo>();
        displayPackageLibrary.Clear();
        if (bfunPackageLibary == null) return;

        // --- QUAN TRỌNG: Re-link Owner để fix lỗi "Missing Owner" ---
        foreach (var pkg in bfunPackageLibary) pkg.SetOwner(this);

        if (string.IsNullOrEmpty(SearchTerm))
        {
            displayPackageLibrary.AddRange(bfunPackageLibary);
        }
        else
        {
            foreach (var pkg in bfunPackageLibary)
            {
                bool matchName = pkg.packageName != null && pkg.packageName.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchUploader = pkg.uploaderName != null && pkg.uploaderName.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                if (matchName || matchUploader) displayPackageLibrary.Add(pkg);
            }
        }
    }

    private static string GetNormalizedName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return name.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
    }

    private string GetBasePackageName(string fullPackageName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(fullPackageName, @"^(.*?)\s*v\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : fullPackageName;
    }

    private async Task<List<string>> GetAllFolderIdsRecursivelyAsync(DriveService service, string rootFolderId)
    {
        var folderIds = new List<string> { rootFolderId };
        var foldersToScan = new Queue<string>();
        foldersToScan.Enqueue(rootFolderId);
        while (foldersToScan.Count > 0)
        {
            string currentFolderId = foldersToScan.Dequeue();
            var request = service.Files.List();
            request.Q = $"'{currentFolderId}' in parents and trashed = false";
            request.Fields = "files(id, mimeType)";
            request.Corpora = "user";
            request.IncludeItemsFromAllDrives = true;
            request.SupportsAllDrives = true;
            FileList result = await request.ExecuteAsync();
            if (result.Files != null)
            {
                foreach (var item in result.Files)
                {
                    if (item.MimeType == "application/vnd.google-apps.folder")
                    {
                        folderIds.Add(item.Id);
                        foldersToScan.Enqueue(item.Id);
                    }
                }
            }
        }
        return folderIds;
    }

    public async Task<string> GetCurrentUserEmailAsync()
    {
        try
        {
            await EnsureDriveServiceAsync();
            if (_driveService == null) return null;
            var request = _driveService.About.Get();
            request.Fields = "user(emailAddress, displayName)";
            var about = await request.ExecuteAsync();
            if (about != null && about.User != null)
            {
                return about.User.EmailAddress;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PackageManager] Không lấy được email user: {ex.Message}");
        }

        return null;
    }
}
#endif