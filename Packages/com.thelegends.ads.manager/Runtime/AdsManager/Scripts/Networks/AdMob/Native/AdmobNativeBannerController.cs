#if USE_ADMOB

namespace TheLegends.Base.Ads
{
    public class AdmobNativeBannerController : AdmobNativePlatformController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeBanner;
#else
            return AdsType.None;
#endif
        }

        protected override void OnAdsLoadFailed(string message)
        {
#if USE_ADMOB
            base.OnAdsLoadFailed(message);

            if (Status == AdsEvents.LoadNotAvailable)
            {
                DelayReloadAd(AdsManager.Instance.adsConfigs.adTimeReload);
            }
#endif
        }

        protected override void OnNativePlatformShow()
        {
#if USE_ADMOB
            OnShow += () => RegisterConfig();
            base.OnNativePlatformShow();
#endif
        }

        protected virtual void RegisterConfig()
        {
            AdsManager.Instance.RegisterNativeBannerConfig(new NativeShowedConfig
            {
                order = this.Order,
                position = position,
                layoutName = _layoutName,
                countdown = _storedCountdown,
                adsPos = _storedPosition,
                reloadTime = _autoReloadTime,
                showOnLoaded = _isShowOnLoaded
            });
        }
    }
}
#endif