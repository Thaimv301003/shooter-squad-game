using System;
using UnityEngine;

namespace Watermelon
{
    public static class AdsManager
    {
        // Sự kiện khi nhà cung cấp được khởi tạo (giữ lại để tránh lỗi biên dịch)
        public static event SimpleCallback OnAdsModuleInitialized;
        
        // Sự kiện khi tắt quảng cáo bắt buộc (No-Ads)
        public static event SimpleCallback ForcedAdDisabled;

        // Cấu hình Ads Settings
        public static AdsSettings Settings { get; private set; }

        private static bool isForcedAdEnabled = true;

        public static void Init(MonetizationSettings settings)
        {
            if (settings != null)
            {
                Settings = settings.AdsSettings;
            }
            
            // Bỏ qua khởi tạo cũ của Watermelon, kích hoạt sự kiện thành công giả lập
            OnAdsModuleInitialized?.Invoke();
        }

        public static void OnProviderInitialized(AdProvider providerType)
        {
            // Giữ lại hàm này để tương thích với AdProviderHandler
        }

        // --- Kiểm tra trạng thái quảng cáo bắt buộc ---
        public static bool IsForcedAdEnabled()
        {
            return isForcedAdEnabled;
        }

        public static void DisableForcedAd()
        {
            isForcedAdEnabled = false;
            ForcedAdDisabled?.Invoke();
            Debug.Log("[AdsManager]: Forced ads (Interstitial) disabled (No-Ads purchased)!");
        }

        // --- Quảng cáo xen kẽ (Interstitial Ad) ---
        public static void ShowInterstitial(AdProviderHandler.AdvertisementCallback callback, bool ignoreConditions = false)
        {
            if (!isForcedAdEnabled && !ignoreConditions)
            {
                callback?.Invoke(true);
                return;
            }

            Debug.Log("[AdsManager]: Requesting real interstitial from TheLegends Ads SDK...");

            if (TheLegends.Base.Ads.AdsManager.Instance != null)
            {
                if (IsInterstitialLoaded())
                {
                    TheLegends.Base.Ads.AdsManager.Instance.ShowInterstitial(
                        TheLegends.Base.Ads.AdsType.Interstitial,
                        TheLegends.Base.Ads.PlacementOrder.One, 
                        "watermelon_interstitial", 
                        () => 
                        {
                            callback?.Invoke(true);
                        }
                    );
                }
                else
                {
                    Debug.LogWarning("[AdsManager]: Interstitial not loaded yet. Requesting load...");
                    RequestInterstitial();
                    
                    callback?.Invoke(true); // Always callback true so game doesn't block
                }
            }
            else
            {
                Debug.LogWarning("[AdsManager]: TheLegends AdsManager Instance is null! Directly invoking callback.");
                callback?.Invoke(true);
            }
        }

        public static bool IsInterstitialLoaded()
        {
            if (TheLegends.Base.Ads.AdsManager.Instance != null)
            {
                return TheLegends.Base.Ads.AdsManager.Instance.GetAdsStatus(
                    TheLegends.Base.Ads.AdsType.Interstitial, 
                    TheLegends.Base.Ads.PlacementOrder.One
                ) == TheLegends.Base.Ads.AdsEvents.LoadAvailable;
            }
            return false;
        }

        public static void RequestInterstitial()
        {
            if (TheLegends.Base.Ads.AdsManager.Instance != null)
            {
                TheLegends.Base.Ads.AdsManager.Instance.LoadInterstitial(TheLegends.Base.Ads.AdsType.Interstitial, TheLegends.Base.Ads.PlacementOrder.One);
            }
        }

        // --- Quảng cáo nhận thưởng (Rewarded Ad) ---
        public static void ShowRewardBasedVideo(AdProviderHandler.AdvertisementCallback callback, bool showErrorMessage = true)
        {
            Debug.Log("[AdsManager]: Requesting real rewarded video from TheLegends Ads SDK...");

            if (TheLegends.Base.Ads.AdsManager.Instance != null)
            {
                if (IsRewardBasedVideoLoaded())
                {
                    TheLegends.Base.Ads.AdsManager.Instance.ShowRewarded(
                        TheLegends.Base.Ads.PlacementOrder.One, 
                        "watermelon_reward", 
                        () => 
                        {
                            callback?.Invoke(true);
                        }
                    );
                }
                else
                {
                    Debug.LogWarning("[AdsManager]: Rewarded video not loaded yet. Requesting load...");
                    RequestRewardBasedVideo();
                    
                    TheLegends.Base.UI.UIToatsController.Show("Ad not available. Try again!", 0.5f, TheLegends.Base.UI.ToastPosition.BottomCenter);
                    callback?.Invoke(false);
                }
            }
            else
            {
                Debug.LogWarning("[AdsManager]: TheLegends AdsManager Instance is null! Directly granting reward as fallback.");
                callback?.Invoke(true);
            }
        }

        public static bool IsRewardBasedVideoLoaded()
        {
            if (TheLegends.Base.Ads.AdsManager.Instance != null)
            {
                return TheLegends.Base.Ads.AdsManager.Instance.GetAdsStatus(
                    TheLegends.Base.Ads.AdsType.Rewarded, 
                    TheLegends.Base.Ads.PlacementOrder.One
                ) == TheLegends.Base.Ads.AdsEvents.LoadAvailable;
            }
            return false;
        }

        public static void RequestRewardBasedVideo()
        {
            if (TheLegends.Base.Ads.AdsManager.Instance != null)
            {
                TheLegends.Base.Ads.AdsManager.Instance.LoadRewarded(TheLegends.Base.Ads.PlacementOrder.One);
            }
        }

        // --- Quảng cáo Banner ---
        public static void ShowBanner()
        {
            // Nơi hiển thị banner mới của bạn
        }

        public static void HideBanner()
        {
            // Nơi ẩn banner mới của bạn
        }

        public static void DestroyBanner()
        {
            // Nơi hủy banner mới của bạn
        }
    }
}