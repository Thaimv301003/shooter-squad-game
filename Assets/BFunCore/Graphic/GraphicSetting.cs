using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;
using BFunCoreKit;
using UnityEngine.UIElements;


#if UNITY_PIPELINE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

[CreateAssetMenu(fileName = "GraphicSettingsDatabase", menuName = "Settings/Graphic Settings Database (Fixed Tiers)")]
public class GraphicSettingsDatabaseSO : ScriptableObject
{

#if UNITY_PIPELINE_URP
        [Title("Graphic Profiles")]
    [ReadOnly, LabelText("Detected Pipeline")] [DisplayAsString(false)]  public string detectedPipeline = "Universal Render Pipeline (URP)";
#else
    [Title("Graphic Profiles")]
    [LabelText("Detected Pipeline")] [DisplayAsString(false)] public string detectedPipeline = "Built-in Render Pipeline";
#endif

#if UNITY_PIPELINE_URP
    [Title("URP Assets")]
    [InfoBox("Gán URP Asset cho từng bậc. Low -> [0], Medium -> [1], High -> [2]")]
    public UniversalRenderPipelineAsset[] urpAssets = new UniversalRenderPipelineAsset[3];
#endif

    [InlineProperty] [HideLabel] [FoldoutGroup("Low Graphic")] public GraphicProfile LowProfile;
    [InlineProperty][HideLabel][FoldoutGroup("Medium Graphic")] public GraphicProfile MediumProfile;
    [InlineProperty][HideLabel][FoldoutGroup("High Graphic")] public GraphicProfile HighProfile;

    public GraphicProfile GetProfile(DeviceProfiler.PerformanceTier tier)
    {
        switch (tier)
        {
            case DeviceProfiler.PerformanceTier.Low: return LowProfile;
            case DeviceProfiler.PerformanceTier.Medium: return MediumProfile;
            case DeviceProfiler.PerformanceTier.High: return HighProfile;
            default: return null;
        }
    }

public void ApplyProfile(DeviceProfiler.PerformanceTier tier, Camera mainCamera, Component postProcessComponent)
    {
        GraphicProfile profileToApply = GetProfile(tier); // Sửa từ switch-case thành gọi hàm GetProfile
        if (profileToApply == null) 
        { 
            Debug.LogError($"Profile for tier {tier} is not assigned! Fallback to Medium."); 
            profileToApply = MediumProfile; // Thêm fallback nếu profile không được gán
        }

        // --- CÀI ĐẶT CHUNG (Native Unity Settings) ---
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = profileToApply.targetFrameRate;
        
        // Texture & LOD
        QualitySettings.globalTextureMipmapLimit = profileToApply.textureQuality;
        QualitySettings.anisotropicFiltering = profileToApply.anisotropicFiltering;
        QualitySettings.lodBias = profileToApply.lodBias;
        
        // Shadows
        QualitySettings.shadows = profileToApply.shadowQuality;
        QualitySettings.shadowDistance = profileToApply.shadowDistance;
        QualitySettings.shadowCascades = profileToApply.shadowCascades;

        if (mainCamera != null) 
            mainCamera.farClipPlane = profileToApply.cameraFarClip;

        // --- XỬ LÝ ĐỘ PHÂN GIẢI & PIPELINE ---
        // Phần còn lại sẽ được thay đổi bên dưới
        #if UNITY_PIPELINE_URP
        // === LOGIC URP ===
        // 1. Swap Asset nếu cần (để đổi setting sâu bên trong)
        var targetUrpAsset = (urpAssets != null && urpAssets.Length > (int)tier) ? urpAssets[(int)tier] : null;
        if (targetUrpAsset != null && GraphicsSettings.renderPipelineAsset != targetUrpAsset)
        {
            GraphicsSettings.renderPipelineAsset = targetUrpAsset;
        }

        // 2. Gán RenderScale vào asset hiện tại (quan trọng để scale hoạt động ngay)
        if (UniversalRenderPipeline.asset != null) // Đảm bảo asset tồn tại
        {
            UniversalRenderPipeline.asset.renderScale = profileToApply.renderScale;
            UniversalRenderPipeline.asset.msaaSampleCount = (int)profileToApply.antiAliasingMode;
        }

        if (postProcessComponent is Volume volume) 
            volume.profile = profileToApply.postProcessingProfile;

#else
      // 1. Xác định độ phân giải gốc (Native Resolution)
        // Lưu ý: Display.main.systemWidth đôi khi không cập nhật kịp trên một số máy Android cũ
        // Cách an toàn nhất là lấy Screen.currentResolution nếu chưa từng scale, 
        // nhưng Display.main.systemWidth thường là chuẩn nhất cho Hardware pixel.
        
        int nativeWidth = Display.main.systemWidth;
        int nativeHeight = Display.main.systemHeight;

        // Fallback: Nếu không lấy được systemWidth (trả về 0), dùng currentResolution
        if (nativeWidth == 0 || nativeHeight == 0)
        {
            nativeWidth = Screen.currentResolution.width;
            nativeHeight = Screen.currentResolution.height;
        }

        // 2. Tính toán Target
        // Kẹp renderScale tối thiểu là 0.3f để tránh màn hình đen hoặc lỗi driver trên một số máy
        float effectiveScale = Mathf.Max(profileToApply.renderScale, 0.3f); 
        
        int targetWidth = Mathf.RoundToInt(nativeWidth * effectiveScale);
        int targetHeight = Mathf.RoundToInt(nativeHeight * effectiveScale);

        // 3. Apply
        if (Screen.width != targetWidth || Screen.height != targetHeight)
        {
            Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.FullScreenWindow);
            BFun.Log($"[Graphic] Changed Resolution: {nativeWidth}x{nativeHeight} -> {targetWidth}x{targetHeight} (Scale: {effectiveScale})");
        }
        else
        {
            BFun.Log($"[Graphic] Resolution already matches target: {targetWidth}x{targetHeight}");
        }

        GraphicsSettings.defaultRenderPipeline = null;

#if UNITY_POST_PROCESSING_STACK_V2
        if (postProcessComponent is PostProcessVolume ppVolume) 
            ppVolume.profile = profileToApply.postProcessProfileV2;
#endif

#endif
}

#if UNITY_EDITOR
    private void Reset()
    {
        LowProfile = new GraphicProfile
        {
            profileName = "Low",
            renderScale = 0.7f,
            targetFrameRate = 30,
            textureQuality = 2,
            lodBias = 1.5f,
            shadowQuality = ShadowQuality.Disable,
            shadowDistance = 15f,
            shadowCascades = 0,
            cameraFarClip = 150f,
            detailObjectDensity = 0.2f
        };
        MediumProfile = new GraphicProfile
        {
            profileName = "Medium",
            renderScale = 0.85f,
            targetFrameRate = 60,
            textureQuality = 1,
            lodBias = 1.0f,
            shadowQuality = ShadowQuality.HardOnly,
            shadowDistance = 40f,
            shadowCascades = 2,
            cameraFarClip = 300f,
            detailObjectDensity = 0.6f
        };
        HighProfile = new GraphicProfile
        {
            profileName = "High",
            renderScale = 1.0f,
            targetFrameRate = 60,
            textureQuality = 0,
            lodBias = 1.0f,
            shadowQuality = ShadowQuality.All,
            shadowDistance = 80f,
            shadowCascades = 2,
            cameraFarClip = 500f,
            detailObjectDensity = 1.0f
        };
    }
#endif
}