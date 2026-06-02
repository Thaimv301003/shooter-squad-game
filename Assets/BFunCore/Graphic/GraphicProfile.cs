using UnityEngine;
using Sirenix.OdinInspector;
using System;

#if UNITY_PIPELINE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

[Serializable]
public class GraphicProfile
{
    [HideInInspector]
    public string profileName = "Balanced";

    // --- Core Performance ---
    [InfoBox("@GetRenderScaleInfo()", InfoMessageType.None)]
    [Range(0.3f, 1f)] public float renderScale = 0.85f;
    [InfoBox("@GetTargetFrameRateInfo()", InfoMessageType.None)]
    [Range(30, 120)] public int targetFrameRate = 60;

    // --- Rendering Quality ---
    [InfoBox("@GetTextureQualityInfo()", InfoMessageType.Info)]
    [Range(0, 3)] public int textureQuality = 1;
    [InfoBox("@GetAnisotropicFilteringInfo()", InfoMessageType.None)]
    public AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.Disable;

    [InfoBox("Hệ số ưu tiên LOD. > 1: Ưu tiên model ít chi tiết (tăng hiệu năng). < 1: Ưu tiên model chi tiết hơn.")]
    [Range(0.5f, 1.5f)] public float lodBias = 1.0f;

    // --- Shadows ---
    [InfoBox("@GetShadowQualityInfo()", InfoMessageType.Warning)]
    public ShadowQuality shadowQuality = ShadowQuality.HardOnly;

    [InfoBox("@GetShadowDistanceInfo()", InfoMessageType.None)]
    public float shadowDistance = 50f;

    [InfoBox("@GetShadowCascadesInfo()", InfoMessageType.None)]
    [Range(0, 2)] public int shadowCascades = 2;

    // --- Environment & Effects ---
    [InfoBox("Khoảng cách nhìn xa. Giảm mạnh ở mức Low để giảm draw calls.")]
    public float cameraFarClip = 300f;

    [InfoBox("Giá trị (0-1) để game điều khiển mật độ chi tiết (cỏ, đá...). Gợi ý: Low (0.2), Medium (0.6), High (1.0).")]
    [Range(0f, 1f)] public float detailObjectDensity = 0.75f;

#if UNITY_PIPELINE_URP
    [InfoBox("Gán Volume Profile tương ứng. Profile Low nên tắt các hiệu ứng nặng như SSAO, Depth of Field.")]
    public VolumeProfile postProcessingProfile;
#elif UNITY_POST_PROCESSING_STACK_V2
    [InfoBox("Gán Post Process Profile tương ứng. Profile Low nên tắt các hiệu ứng nặng như Ambient Occlusion.")]
    public PostProcessProfile postProcessProfileV2;
#endif

#if UNITY_PIPELINE_URP
    [InfoBox("Khử răng cưa. 'None' cho hiệu năng cao nhất. 'FXAA' là lựa chọn cân bằng tốt cho mobile.")]
    public AntialiasingMode antiAliasingMode = AntialiasingMode.FastApproximateAntialiasing;
#endif


    // -------------------------------------------------------------------------------------
    // --- CÁC HÀM HELPER CHO INFOBOX (CHỈ TỒN TẠI TRONG EDITOR) ---
    // -------------------------------------------------------------------------------------
#if UNITY_EDITOR
    private string GetRenderScaleInfo()
    {
        if (this.profileName == "Low") return "Gợi ý: 0.7 - 0.75. Tăng FPS đáng kể, hy sinh độ nét.";
        if (this.profileName == "Medium") return "Gợi ý: 0.85 - 0.9. Cân bằng tốt giữa hiệu năng và chất lượng.";
        if (this.profileName == "High") return "Gợi ý: 1.0. Chất lượng gốc, sắc nét nhất. Chỉ dành cho máy mạnh.";
        return "Điều chỉnh độ phân giải render nội bộ. Yếu tố quan trọng nhất cho hiệu năng.";
    }

    private string GetTargetFrameRateInfo()
    {
        if (this.profileName == "Low") return "Gợi ý: 30. Tiết kiệm pin tối đa, ổn định trên máy yếu.";
        return "Gợi ý: 60. Trải nghiệm mượt mà tiêu chuẩn. Có thể set cao hơn cho màn hình 90/120Hz.";
    }

    private string GetTextureQualityInfo()
    {
        string baseInfo = "0=Full, 1=Half, 2=Quarter. Giảm để tiết kiệm VRAM, tránh crash.";
        if (this.profileName == "Low") return baseInfo + "\nGợi ý: 2. Rất quan trọng cho máy < 4GB RAM.";
        if (this.profileName == "Medium") return baseInfo + "\nGợi ý: 1. Lựa chọn an toàn cho hầu hết các máy.";
        if (this.profileName == "High") return baseInfo + "\nGợi ý: 0. Chỉ dành cho máy có > 6GB RAM.";
        return baseInfo;
    }

    private string GetAnisotropicFilteringInfo()
    {
        if (this.profileName == "High") return "Cải thiện độ nét của texture khi nhìn ở góc nghiêng. Gợi ý: Enable.";
        return "Cải thiện độ nét của texture khi nhìn ở góc nghiêng. Gợi ý: Disable để tăng một chút hiệu năng.";
    }

    private string GetShadowQualityInfo()
    {
        if (this.profileName == "Low") return "Gợi ý: Disable. Tăng FPS nhiều nhất.";
        if (this.profileName == "Medium") return "Gợi ý: Hard Only. Hiệu quả về mặt hiệu năng.";
        if (this.profileName == "High") return "Gợi ý: All. Bóng mềm, đẹp nhất nhưng nặng nhất.";
        return "Chất lượng tổng thể của bóng đổ.";
    }

    private string GetShadowDistanceInfo()
    {
        if (this.profileName == "Low") return "Khoảng cách đổ bóng. Gợi ý: 15-25. Giảm mạnh để tăng FPS.";
        if (this.profileName == "Medium") return "Khoảng cách đổ bóng. Gợi ý: 40-50.";
        if (this.profileName == "High") return "Khoảng cách đổ bóng. Gợi ý: 70-80.";
        return "Khoảng cách đổ bóng.";
    }

    private string GetShadowCascadesInfo()
    {
        if (this.profileName == "Low") return "Số tầng đổ bóng. Gợi ý: 0 (No Cascades) để nhanh nhất.";
        if (this.profileName == "Medium") return "Số tầng đổ bóng. Gợi ý: 2. Cân bằng giữa chất lượng và hiệu năng.";
        if (this.profileName == "High") return "Số tầng đổ bóng. Gợi ý: 2. Không nên dùng 4 trên mobile.";
        return "Số tầng đổ bóng.";
    }
#endif
}