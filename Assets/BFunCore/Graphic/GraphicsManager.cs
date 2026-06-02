using UnityEngine;
using Sirenix.OdinInspector;
using BFunCoreKit; // Namespace Singleton của bạn
#if UNITY_EDITOR
using UnityEditor;
#endif
using System; // Cần thiết cho 'Action'

#if UNITY_PIPELINE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Cần cho Volume
#endif

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

public class GraphicsManager : Singleton<GraphicsManager>
{
    // --- THÊM MỚI: Event để thông báo cho các hệ thống khác ---
    public static event Action OnGraphicsChanged;

    // --- THÊM MỚI: Property để các script khác truy cập giá trị density ---
    public float CurrentDetailDensity { get; private set; } = 1.0f;


#if UNITY_PIPELINE_URP
    [SceneObjectsOnly] public Volume globalVolume;
#elif UNITY_POST_PROCESSING_STACK_V2
    [SceneObjectsOnly] public PostProcessVolume postProcessVolumeV2;
#endif

    [SerializeField] private GraphicSettingsDatabaseSO settingsDatabase;

    public const string PrefsKey = "GraphicProfileTier";
    public const string FirstLaunchKey = "HasLaunchedBefore";

    private DeviceProfiler.PerformanceTier _currentTier;
    public DeviceProfiler.PerformanceTier CurrentTier => _currentTier;

    public override void Awake()
    {
        base.Awake();
        // Load asset từ một đường dẫn cố định, rất tốt cho việc quản lý tập trung!
#if UNITY_EDITOR
        if (settingsDatabase == null)
        {
            settingsDatabase = AssetDatabase.LoadAssetAtPath<GraphicSettingsDatabaseSO>(GlobalConst.SettingFolder + "/Graphics Setting.asset");
        }
#endif
    }

    void Start()
    {
        // Giả sử GameManager.Instance.cameraMain đã được gán ở đâu đó trước Start()
        // Nếu không, dòng này vẫn là một phương án dự phòng tốt:
        if (GameManager.Instance.cameraMain == null) GameManager.Instance.cameraMain = Camera.main;

#if UNITY_PIPELINE_URP
        if (globalVolume == null) globalVolume = FindObjectOfType<Volume>();
#elif UNITY_POST_PROCESSING_STACK_V2
        if (postProcessVolumeV2 == null) postProcessVolumeV2 = FindObjectOfType<PostProcessVolume>();
#endif

        DeviceProfiler.PerformanceTier tierToApply;
        if (PlayerPrefs.GetInt(FirstLaunchKey, 0) == 0)
        {
            tierToApply = DeviceProfiler.AutoDetectTier();
            PlayerPrefs.SetInt(FirstLaunchKey, 1);
        }
        else
        {
            tierToApply = (DeviceProfiler.PerformanceTier)PlayerPrefs.GetInt(PrefsKey, (int)DeviceProfiler.PerformanceTier.Medium);
        }
        ApplyAndSaveProfile(tierToApply);
    }

    // Hàm public cho UI, nhận vào tier để áp dụng
    public void SetGraphicsQuality(DeviceProfiler.PerformanceTier tier)
    {
        // Tránh áp dụng lại nếu không có gì thay đổi
        if (tier == _currentTier) return;

        ApplyAndSaveProfile(tier);
    }

    public void ApplyAndSaveProfile(DeviceProfiler.PerformanceTier tier)
    {
        if (settingsDatabase == null)
        {
            Debug.LogError("[GraphicsManager] Settings Database could not be loaded!");
            return;
        }

        _currentTier = tier;

        // --- THÊM MỚI: Cập nhật giá trị CurrentDetailDensity ---
        // Lấy profile tương ứng để đọc giá trị density.
        // Điều này yêu cầu bạn thêm một hàm helper nhỏ vào GraphicSettingsDatabaseSO.
        GraphicProfile currentProfile = settingsDatabase.GetProfile(tier);
        if (currentProfile != null)
        {
            CurrentDetailDensity = currentProfile.detailObjectDensity;
            //Debug.Log($"[GraphicsManager] Detail Object Density set to: {CurrentDetailDensity}");
        }
        // --- KẾT THÚC PHẦN THÊM MỚI ---

        Component ppComponent = null;
#if UNITY_PIPELINE_URP
        ppComponent = globalVolume;
#elif UNITY_POST_PROCESSING_STACK_V2
        ppComponent = postProcessVolumeV2;
#endif
        BFun.Log("[Graphic] Device Performance : " + tier);
        settingsDatabase.ApplyProfile(tier, GameManager.Instance.cameraMain, ppComponent);

        PlayerPrefs.SetInt(PrefsKey, (int)tier);
        PlayerPrefs.Save();

        // --- THÊM MỚI: Bắn ra sự kiện để thông báo cho các hệ thống khác ---
        OnGraphicsChanged?.Invoke();
    }
}