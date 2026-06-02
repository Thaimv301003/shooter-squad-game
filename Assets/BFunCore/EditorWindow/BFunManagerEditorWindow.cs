#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using LitMotion.Animation;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace BFunCoreKit
{
    public class BFunManagerEditorWindow : OdinMenuEditorWindow
    {
        // Cấu hình chiều cao cho phần Header (Ảnh + Nút bấm)
        private const float HEADER_HEIGHT = 150f;

        private List<ScriptableObject> loadedAssets = new List<ScriptableObject>();

        // -----------------------------------------------------------------------
        // 1. SETUP KÍCH THƯỚC MENU TẠI ĐÂY (TRÁNH LỖI GHOSTING)
        // -----------------------------------------------------------------------
        protected override void OnEnable()
        {
            base.OnEnable();

            // Thiết lập chiều rộng menu 1 lần duy nhất khi bật cửa sổ
            this.MenuWidth = 180;
            this.ResizableMenuWidth = false; // Khuyên dùng false để giao diện ổn định
        }

        private static void OpenWindow()
        {
            var window = GetWindow<BFunManagerEditorWindow>(true, "BFun Manager", true);
            window.minSize = new Vector2(900, 600);
            window.maxSize = new Vector2(900, 600);
            window.Show();
        }

        [MenuItem("BFun/Manager _`")]
        private static void ToggleWindow()
        {
            if (EditorApplication.isPlaying) return;

            const string toggleKey = "BFunManager_ToggleLock";
            if (SessionState.GetBool(toggleKey, false)) return;

            SessionState.SetBool(toggleKey, true);
            EditorApplication.delayCall += () => SessionState.SetBool(toggleKey, false);

            var window = Resources.FindObjectsOfTypeAll<BFunManagerEditorWindow>().FirstOrDefault();
            if (window != null) window.Close();
            else OpenWindow();
        }

        // -----------------------------------------------------------------------
        // 2. VẼ HEADER RIÊNG, VẼ ODIN MENU RIÊNG
        // -----------------------------------------------------------------------
        protected override void OnImGUI()
        {
            // --- PHẦN A: VẼ HEADER CỦA BẠN ---
            // Tạo một vùng cố định ở trên cùng cho Header
            Rect headerRect = new Rect(0, 0, position.width, HEADER_HEIGHT);

            GUILayout.BeginArea(headerRect);
            {
                DrawCustomHeader();
            }
            GUILayout.EndArea();

            // --- PHẦN B: VẼ ODIN MENU & EDITOR ---
            // Tính toán vùng còn lại bên dưới Header
            Rect odinContentRect = new Rect(0, HEADER_HEIGHT, position.width, position.height - HEADER_HEIGHT);

            // Ép Odin chỉ được vẽ trong vùng này -> Hết lỗi đè chữ
            GUILayout.BeginArea(odinContentRect);
            {
                base.OnImGUI(); // Gọi hàm vẽ chuẩn của Odin 4.0
            }
            GUILayout.EndArea();

            HandleHotkeys();
        }

        // Tách hàm vẽ Header ra cho gọn code
        private void DrawCustomHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Load Texture (Thêm check null để tránh lỗi đỏ Console nếu ảnh mất)
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BFunCore/Texture2D/Unity.jpg");
            if (icon != null)
            {
                Rect rect = GUILayoutUtility.GetRect(position.width, 120);
                UnityEngine.GUI.DrawTexture(rect, icon, ScaleMode.ScaleAndCrop);
            }
            else
            {
                // Placeholder nếu chưa có ảnh
                GUILayout.Space(120);
                GUILayout.Label("IMAGE NOT FOUND", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(-32);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("", EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Dòng Button Toolbar
            GUILayout.BeginHorizontal(EditorStyles.boldLabel);
            GUILayout.Label($"Version {GameManager.BFUN_VERSION}", EditorStyles.miniLabel, GUILayout.Width(150));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Wiki", EditorGUIUtility.IconContent("_Help").image), EditorStyles.toolbarButton))
            {
                EditorApplication.ExecuteMenuItem("BFun/Documentation (Wiki)");
            }

            if (GUILayout.Button(new GUIContent("Addressable", EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image), EditorStyles.toolbarButton))
            {
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
            }

            if (GUILayout.Button(new GUIContent("Build", EditorGUIUtility.IconContent("BuildSettings.Android.Small").image), EditorStyles.toolbarButton))
            {
                SmartBuild.ShowSmartBuildWindow();
            }

            if (GUILayout.Button(new GUIContent("Save", EditorGUIUtility.IconContent("SaveActive").image), EditorStyles.toolbarButton))
            {
                SaveAllAssets(true);
            }

            GUILayout.EndHorizontal();
        }

        protected override void DrawEditors()
        {
            // Không set MenuWidth ở đây nữa!

            base.DrawEditors();

            // Vẽ Divider chặn chuột (giữ nguyên logic của bạn)
            var dividerRect = new Rect(this.MenuWidth - 4, 0, 8, position.height);
            EditorGUIUtility.AddCursorRect(dividerRect, MouseCursor.Arrow);
            if (Event.current.type == EventType.MouseDown && dividerRect.Contains(Event.current.mousePosition))
                Event.current.Use();
        }

        private void HandleHotkeys()
        {
            if (Event.current.type != EventType.KeyDown) return;

            int index = -1;
            switch (Event.current.keyCode)
            {
                case KeyCode.Alpha1: index = 0; break;
                case KeyCode.Alpha2: index = 1; break;
                case KeyCode.Alpha3: index = 2; break;
                case KeyCode.Alpha4: index = 3; break;
                case KeyCode.Alpha5: index = 4; break;
                case KeyCode.Alpha6: index = 5; break;
                case KeyCode.Alpha7: index = 6; break;
                case KeyCode.Alpha8: index = 7; break;
                case KeyCode.Alpha9: index = 8; break;
                case KeyCode.Alpha0: index = 9; break;
                case KeyCode.Minus: index = 10; break;
                case KeyCode.Equals: index = 11; break;
            }

            var items = MenuTree.MenuItems;
            if (index >= 0 && index < items.Count)
            {
                MenuTree.Selection.Clear();
                MenuTree.Selection.Add(items[index]);
                Event.current.Use();
            }
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Selection.SupportsMultiSelect = false;

            // Load Data logic
            BFunManagerData bFunManagerData = AssetDatabase.LoadAssetAtPath<BFunManagerData>(GlobalConst.SettingFolder + "/Project.asset");
            if (bFunManagerData) bFunManagerData.Init();

            const string initKey = "BFunManagerEditorWindow_InitCalled";
            if (!SessionState.GetBool(initKey, false))
            {
                AdsData adsData = AssetDatabase.LoadAssetAtPath<AdsData>(GlobalConst.SettingFolder + "/Ads Setting.asset");
                if (adsData != null) adsData.InitializeAndRefresh();
                SessionState.SetBool(initKey, true);
            }

            loadedAssets.Clear();
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { GlobalConst.SettingFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && !loadedAssets.Contains(asset))
                {
                    loadedAssets.Add(asset);
                }
            }

            loadedAssets = loadedAssets
                .OrderBy(x => x.name != "Project")
                .ThenBy(x => x.name)
                .ToList();

            // Duyệt asset và add vào Tree
            foreach (var asset in loadedAssets)
            {
                Texture icon = null;
                string assetName = asset.name.Replace(" Setting", "");

#if BFUN_INSTALLED_TRUE
                if (asset is LitMotionAnimationData || asset is BFunSetting) continue;
#endif

                // Logic switch case của bạn
                // Lưu ý: Tôi đã sửa tree.Add(asset.name -> assetName) để tên trên menu đẹp hơn (bỏ chữ Setting)
                switch (assetName)
                {
                    case "Project":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/ProjectIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Ads":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/AdsIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Binding":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/BindingIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Color":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/ColorIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Console":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/ConsoleIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Font":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/FontIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Localization":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/LocalizationIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Sound":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/SoundIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "UI":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/UIIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Package":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/PackageICon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "Graphics":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/GraphicIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    case "BugReport":
                        icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/BFunCore/Icons/BugReportIcon.png");
                        tree.Add(assetName, asset, icon);
                        break;
                    default:
                        break;
                }
            }

            return tree;
        }

        private void SaveAllAssets(bool log)
        {
            foreach (var so in loadedAssets)
            {
                if (so == null) continue;
                var method = so.GetType().GetMethod("Save");
                if (method != null) method.Invoke(so, null);
                EditorUtility.SetDirty(so);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (log) Debug.Log("All assets saved!");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SaveAllAssets(false);
        }
    }
}
#endif