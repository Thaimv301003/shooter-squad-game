using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using Sirenix.OdinInspector;
using UnityEngine;
#if BFUN_INSTALLED_TRUE
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using UnityEngine.UI;
using LitMotion.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if USE_ADMOB
using TheLegends.Base.Ads;
#endif

namespace BFunCoreKit
{
    public class GUIManager : Singleton<GUIManager>
    {
        public Camera UICamera;
#if BFUN_INSTALLED_TRUE
        Panel loadingScreenPanel;
#endif
        [SerializeField] Transform loadingSpawn, canvasSpawn;
        [SerializeField] SystemMonitor fps;
        [SerializeField] DebugConsole debugConsole;

        private List<GameObject> goCanvases = new List<GameObject>();
#if BFUN_INSTALLED_TRUE
        private readonly List<AsyncOperationHandle<GameObject>> _instantiatedCanvasHandles = new List<AsyncOperationHandle<GameObject>>();
        private AsyncOperationHandle<GameObject> _globalCanvasHandle;
#endif

        [SerializeField] CanvasGroup mainCanvasGroup;
        CanvasScaler canvasScaler;
        public Canvas canvas;

        [SerializeField, ReadOnly] List<string> panelsShowing = new List<string>();
        [SerializeField, ReadOnly] List<string> lastPanels = new List<string>();

        public GameObject buttonPrefab, textPrefab;

#if USE_ADMOB
        //[SerializeField] AdmobNativeController admobNativeController;
        [SerializeField] RectTransform nativeRect;
#endif

        bool isMainCanvasShow;
#if BFUN_INSTALLED_TRUE
        [ReadOnly] public GUISetting uiSetting;
#endif

        // CORE LOGIC: Chỉ cho bấm khi không có panel nào đang chạy hiệu ứng
        public bool AllowToPressButton
        {
            get { return panelsShowing.Count == 0; }
        }

        public override void Awake()
        {
            base.Awake();
#if BFUN_INSTALLED_TRUE
#if UNITY_EDITOR
            if (uiSetting == null)
            {
                uiSetting = AssetDatabase.LoadAssetAtPath<GUISetting>(GlobalConst.SettingFolder + "/UI Setting.asset");
            }
#endif
#endif
            canvasScaler = GetComponentInChildren<CanvasScaler>();
            SetMatchScaleByScreen();
            InitLoadingScreen();
            StartCoroutine(SetupCanvasGlobal());
            isMainCanvasShow = true;

#if USE_ADMOB
            //admobNativeController.LoadAds();
#endif
        }

        #if USE_ADMOB
        public void SetMainCanvasBannerHeight(int height)
        {
            BFunHelper.SetRectBottom(mainCanvasGroup.GetComponent<RectTransform>(), height);
            if (nativeRect)
            {
                BFunHelper.SetRectHeight(nativeRect, height);
            }
        }
#endif

        void InitLoadingScreen()
        {
#if BFUN_INSTALLED_TRUE
            loadingScreenPanel = Instantiate(uiSetting.loadingScreenPanel, loadingSpawn).GetComponent<Panel>();
#endif
        }

        public void AddPanelShowing(string panelName)
        {
            if (!panelsShowing.Contains(panelName))
                panelsShowing.Add(panelName);
        }

        public void ShowCanvas()
        {
            isMainCanvasShow = !isMainCanvasShow;
            mainCanvasGroup.alpha = isMainCanvasShow ? 1 : 0;
        }

        public void RemovePanelShowing(string panelName)
        {
            if (panelsShowing.Contains(panelName))
                panelsShowing.Remove(panelName);
        }

        public void AddLastPanel(string panelName)
        {
            lastPanels.Add(panelName);
        }

        public void ClearLastPanel()
        {
            lastPanels.Clear();
        }

        public void ShowDebugConsole()
        {
            debugConsole.gameObject.SetActive(!debugConsole.gameObject.activeInHierarchy);
            // Cách 1: Dùng biến đã reference
            if (debugConsole != null)
            {
                debugConsole.ToggleConsole();
            }
            // Cách 2: Fallback dùng Singleton (nếu lỡ quên kéo vào Inspector)
            else if (DebugConsole.Instance != null)
            {
                DebugConsole.Instance.ToggleConsole();
            }
            else
            {
                Debug.LogWarning("GUIManager: Chưa gán DebugConsole hoặc DebugConsole chưa khởi tạo!");
            }
        }

        public void ShowConsole()
        {
            if (debugConsole != null)
            {
                debugConsole.ShowConsole();
            }
        }

        public void CloseConsole()
        {
            if (debugConsole != null)
            {
                debugConsole.HideConsole();
            }
        }

        [DebugCommand("show_fps", "Hiển thị thông số FPS")]
        private static void ShowFps()
        {
            Instance.fps.gameObject.SetActive(!Instance.fps.gameObject.activeInHierarchy);
            Instance.fps.enabled = !Instance.fps.enabled;
        }

        public IEnumerator SetupCanvasGlobal()
        {
#if BFUN_INSTALLED_TRUE
            if (_globalCanvasHandle.IsValid()) yield break;
            var opGlobal = Addressables.InstantiateAsync("CanvasGlobal", canvasSpawn);
            _globalCanvasHandle = opGlobal;
            yield return opGlobal;
#else
            yield break;
#endif
        }

        public IEnumerator SwitchCanvas(CanvasName canvasName)
        {
            ClearLastCanvas();
            ClearLastPanel();
            yield return null;
            var opScene = Addressables.InstantiateAsync(canvasName.ToString(), canvasSpawn);
            _instantiatedCanvasHandles.Add(opScene);
            Application.backgroundLoadingPriority = ThreadPriority.High;
            yield return opScene;
            Application.backgroundLoadingPriority = ThreadPriority.Low;
        }

        private void ClearLastCanvas()
        {
#if BFUN_INSTALLED_TRUE
            foreach (var handle in _instantiatedCanvasHandles)
            {
                if (handle.IsValid()) Addressables.ReleaseInstance(handle);
            }
            _instantiatedCanvasHandles.Clear();
#endif
        }

        void SetMatchScaleByScreen()
        {
            Rect safe = Screen.safeArea;
            float aspect = (safe.width > 0 && safe.height > 0) ? safe.width / safe.height : (float)Screen.width / Screen.height;
            aspect = Mathf.Clamp(aspect, 1.3f, 2.4f);
            float match = Mathf.InverseLerp(1.33f, 2.2f, aspect);
            canvasScaler.matchWidthOrHeight = match;
            BFun.Log("Set Screen Scale : " + canvasScaler.matchWidthOrHeight);
        }

#if BFUN_INSTALLED_TRUE
        public IEnumerator ShowBackground(Action onDoneLoad = null)
        {
            yield return loadingScreenPanel.Show();
            onDoneLoad?.Invoke();
        }
        public IEnumerator CloseBackGround(Action onDoneLoad = null)
        {
            yield return loadingScreenPanel.Close();
            onDoneLoad?.Invoke();
        }
#endif
    }
}