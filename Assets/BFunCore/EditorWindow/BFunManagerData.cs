using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // Cần thêm để sử dụng LINQ
using System;

namespace BFunCoreKit
{
    public class BFunManagerData : ScriptableObject
    {
        [Space(10)]
        [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 80)]
        [HideLabel]
        public Texture2D buildIcon;
        [Space(10)]

        string platform;
        [TitleGroup("$platform", alignment: TitleAlignments.Centered, horizontalLine: true, boldTitle: true, indent: false)]
        public string apkName;
        public string PackageName;
#if UNITY_EDITOR
        public UIOrientation Orientation;
        public AndroidSdkVersions minimumAPILevel, maximumAPILevel;
#endif
        public string GameVersion;
        public int BundleVersion;
        public ColorSpace colorSpace;

#if UNITY_EDITOR
#if UNITY_EDITOR
        [OnInspectorInit] private void InitSceneSearch() => UpdateSceneSearch();
        private void OnEnable() => UpdateSceneSearch();

        [HideInInspector] public string SceneSearchTerm;

        // List gốc lưu dữ liệu (Ẩn đi)
        [HideInInspector][SerializeField] public List<SceneEntry> allScenes = new List<SceneEntry>();

        // List hiển thị (Vẽ Toolbar thủ công giống Package Manager)
        [OnInspectorGUI("DrawSceneHeader", append: false)]
        [TableList(DrawScrollView = true, IsReadOnly = true)]
        [ListDrawerSettings(IsReadOnly = true, HideAddButton = true, HideRemoveButton = true, DraggableItems = false, Expanded = true, ShowPaging = false, NumberOfItemsPerPage = 20)]
        [HideReferenceObjectPicker]
        [HideLabel]
        [NonSerialized]
        public List<SceneEntry> displayScenes = new List<SceneEntry>();

        private void DrawSceneHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Scenes", EditorStyles.boldLabel);
            if (displayScenes != null) GUILayout.Label($"({displayScenes.Count})", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            try
            {
                // Code y hệt bên Package Manager
                string newTerm = Sirenix.Utilities.Editor.SirenixEditorGUI.SearchField(GUILayoutUtility.GetRect(200, 16), SceneSearchTerm);
                if (newTerm != SceneSearchTerm) { SceneSearchTerm = newTerm; UpdateSceneSearch(); }
            }
            catch { }
            GUILayout.EndHorizontal();
        }

        public void UpdateSceneSearch()
        {
            if (displayScenes == null) displayScenes = new List<SceneEntry>();
            displayScenes.Clear();
            if (allScenes == null) return;
            if (string.IsNullOrEmpty(SceneSearchTerm)) displayScenes.AddRange(allScenes);
            else
            {
                foreach (var s in allScenes)
                {
                    if ((s.SceneName != null && s.SceneName.IndexOf(SceneSearchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (s.Path != null && s.Path.IndexOf(SceneSearchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0))
                        displayScenes.Add(s);
                }
            }
        }

        // Hàm này trước bạn bảo không thấy, giờ nó nằm ngay đây
        public void OnSceneListChanged()
        {
            var scenesInBuildFromTable = allScenes.Where(s => s.InBuild).ToList();
            var newScenePaths = scenesInBuildFromTable.Select(s => s.Path).ToList();
            var currentBuildScenePaths = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            if (!newScenePaths.SequenceEqual(currentBuildScenePaths))
            {
                var newBuildScenes = new List<EditorBuildSettingsScene>();
                foreach (var scenePath in newScenePaths) newBuildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = newBuildScenes.ToArray();
            }
            UpdateSceneSearch();
        }

        [OnInspectorInit]
        private void RefreshScenes()
        {
            allScenes.Clear();
            var scenesInBuildPaths = new HashSet<string>();
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled)
                {
                    string path = buildScene.path;
                    allScenes.Add(new SceneEntry(System.IO.Path.GetFileNameWithoutExtension(path), path, true, this));
                    scenesInBuildPaths.Add(path);
                }
            }
            string[] guids = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (scenesInBuildPaths.Contains(path)) continue;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (path.Contains("Packages/") || path.Contains("Library/") || path.Contains("Editor/") || path.Contains("AddressableAssetSettings") || name.StartsWith("~")) continue;
                allScenes.Add(new SceneEntry(name, path, false, this));
            }
            UpdateSceneSearch();
        }
#endif

        [System.Serializable]
public class SceneEntry
{
    // Cột chứa các nút Lên/Xuống
    [TableColumnWidth(60, Resizable = false)]
    [ButtonGroup("Index")]
    [Button(SdfIconType.ArrowUp, "")]
    [EnableIf("InBuild")] // THÊM MỚI: Chỉ bật nút khi scene đã "In Build"
    private void MoveUp()
    {
        var list = owner.allScenes;
        int index = list.IndexOf(this);
        if (index > 0)
        {
            // Chỉ cho phép di chuyển trong phạm vi các scene "In Build"
            if(list[index - 1].InBuild)
            {
                var temp = list[index - 1];
                list[index - 1] = this;
                list[index] = temp;
                owner.OnSceneListChanged(); // Gọi hàm cập nhật Build Settings
            }
        }
    }

    [ButtonGroup("Index")]
    [Button(SdfIconType.ArrowDown, "")]
    [EnableIf("InBuild")] // THÊM MỚI: Chỉ bật nút khi scene đã "In Build"
    private void MoveDown()
    {
        var list = owner.allScenes;
        int index = list.IndexOf(this);
        if (index < list.Count - 1)
        {
            // Chỉ cho phép di chuyển trong phạm vi các scene "In Build"
            if(list[index + 1].InBuild)
            {
                var temp = list[index + 1];
                list[index + 1] = this;
                list[index] = temp;
                owner.OnSceneListChanged(); // Gọi hàm cập nhật Build Settings
            }
        }
    }

    [TableColumnWidth(150, Resizable = false)]
    [DisplayAsString(false)]
    [HideLabel]
    [ReadOnly] public string SceneName;


    [ReadOnly][DisplayAsString(false)] public string Path;

    [HideInInspector]
    public bool InBuild;

    [TableColumnWidth(85, Resizable = false)]
    [ShowInInspector, ReadOnly, HideLabel]
    [GUIColor(nameof(GetStatusColor))]
    [DisplayAsString(false)]
    private string Status => InBuild ? "✓ In Build" : "✗ Not Added";


    private BFunManagerData owner;

    private Color GetStatusColor() => InBuild ? Color.green : new Color(1f, 0.5f, 0.5f);

    public SceneEntry(string name, string path, bool inBuild, BFunManagerData owner)
    {
        SceneName = name;
        Path = path;
        InBuild = inBuild;
        this.owner = owner;
    }

    [TableColumnWidth(90, Resizable = false)]
    [Button("Add", ButtonHeight = 22)]
    [EnableIf("@!InBuild")]
    private void AddToBuild()
    {
        if (InBuild) return;
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == Path)) {
            scenes.Add(new EditorBuildSettingsScene(Path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            owner?.RefreshScenes(); // Sẽ tự gọi update search
        }
    }

    [TableColumnWidth(110, Resizable = false)]
    [Button("Remove", ButtonHeight = 22)]
    [EnableIf("@InBuild")]
    private void RemoveFromBuild()
    {
        if (!InBuild) return;
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == Path)) {
            scenes.RemoveAll(s => s.path == Path);
            EditorBuildSettings.scenes = scenes.ToArray();
            owner?.RefreshScenes(); // Sẽ tự gọi update search
        }
    }
}
#endif

#if UNITY_EDITOR


        [ValueDropdown("GetScenes"), OnValueChanged(nameof(OnLoadingSceneChanged))]
#endif
        public string LoadingScene = "Loading";
#if UNITY_EDITOR
        [ValueDropdown("GetScenes")]
#endif
        public string HomeScene = "Home";
#if UNITY_EDITOR
        private IEnumerable<ValueDropdownItem<string>> GetScenes()
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    string path = scene.path;
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    yield return new ValueDropdownItem<string>(name, path);
                }
            }
        }
        private void AddPrefabToLoadingScene()
        {
            if (string.IsNullOrEmpty(LoadingScene))
            {
                Debug.LogError("❌ No loading scene selected!");
                return;
            }

            string targetScenePath = LoadingScene;
            if (!targetScenePath.EndsWith(".unity"))
            {
                string[] found = AssetDatabase.FindAssets($"{LoadingScene} t:Scene");
                if (found.Length > 0)
                    targetScenePath = AssetDatabase.GUIDToAssetPath(found[0]);
            }

            string prefabPath = "Assets/BFunCore/Prefab/BFun.Core.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"❌ Prefab not found at {prefabPath}");
                return;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(targetScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = Vector3.zero;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);

            Debug.Log($"✅ '{prefab.name}' prefab added to scene '{scene.name}' and saved.");
        }
#endif

#if UNITY_EDITOR
        private string previousLoadingScene;

        private void OnLoadingSceneChanged()
        {
            if (!string.IsNullOrEmpty(previousLoadingScene))
            {
                string oldScenePath = previousLoadingScene;
                if (!oldScenePath.EndsWith(".unity"))
                {
                    string[] foundOld = AssetDatabase.FindAssets($"{previousLoadingScene} t:Scene");
                    if (foundOld.Length > 0)
                        oldScenePath = AssetDatabase.GUIDToAssetPath(foundOld[0]);
                }

                if (System.IO.File.Exists(oldScenePath))
                {
                    var oldScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(oldScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    var oldPrefab = GameObject.Find("BFun.Core");
                    if (oldPrefab != null)
                    {
                        UnityEngine.Object.DestroyImmediate(oldPrefab);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(oldScene);
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(oldScene);
                    }
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(oldScene, true);
                }
            }
            
            previousLoadingScene = LoadingScene;
            
            AddPrefabToLoadingScene();
        }

        Texture2D GetProjectIcon(Texture2D fallback = null)
        {
            var icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            if (icons != null && icons.Length > 0 && icons[0] != null)
            {
                return icons[0];
            }
            return fallback;
        }

        public void Init()
        {
            platform = "----- " + "Build Target : " + EditorUserBuildSettings.activeBuildTarget.ToString() + " -----";

            buildIcon = GetProjectIcon();
            apkName = PlayerSettings.productName;
            PackageName = PlayerSettings.applicationIdentifier;
            Orientation = PlayerSettings.defaultInterfaceOrientation;
            GameVersion = PlayerSettings.bundleVersion;
            BundleVersion = PlayerSettings.Android.bundleVersionCode;
            maximumAPILevel = PlayerSettings.Android.targetSdkVersion;
            minimumAPILevel = PlayerSettings.Android.minSdkVersion;
            colorSpace = PlayerSettings.colorSpace;
        }

        public void Save()
        {
            PlayerSettings.productName = apkName;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            PlayerSettings.defaultInterfaceOrientation = Orientation;
            PlayerSettings.bundleVersion = GameVersion;
            PlayerSettings.Android.bundleVersionCode = BundleVersion;
            PlayerSettings.Android.targetSdkVersion = maximumAPILevel;
            PlayerSettings.Android.minSdkVersion = minimumAPILevel;
            PlayerSettings.colorSpace = colorSpace;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { buildIcon });
        }
#endif
    }
}