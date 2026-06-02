using UnityEngine;
using TheLegends.Base.Ads;

public class NativeBanner : MonoBehaviour
{
    [Header("Banner Settings")]
    [SerializeField] private PlacementOrder order = PlacementOrder.One;
    [SerializeField] private string positionName = "NativeBannerPos";
    [SerializeField] private string layoutName = "native_banner";

    void OnEnable() 
    {
        if (AdsManager.Instance != null)
        {
            // Gọi lệnh Load
            AdsManager.Instance.LoadNativeBanner(order);
            
            // Gọi lệnh Show - Phải có .Execute() thì Ad mới thực sự hiển thị

                    AdsManager.Instance.ShowNativeBanner(order, positionName,
                     NativeName.Native_Banner, () =>
        {
            AdsManager.Instance.Log("NativeBannerPlatform show");
        }, () =>
        {
            AdsManager.Instance.Log("NativeBannerPlatform closed");
        }, () =>
        {
            AdsManager.Instance.Log("NativeBannerPlatform full screen content closed");
        })
        ?.WithAutoReload(AdsManager.Instance.adsConfigs.nativeBannerTimeReload)
        ?.WithShowOnLoaded(true)
        ?.Execute();
        }
    }

    void OnDisable() 
    {
        // Gọi lệnh Hide để dọn dẹp khi UI chứa nó đóng lại
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideNativeBanner(order);
        }
    }
}
