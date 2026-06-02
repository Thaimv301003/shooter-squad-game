#if USE_ADMOB

namespace TheLegends.Base.Ads
{
    public class AdmobNativeMrecController : AdmobNativeBannerController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeMrec;
#else
            return AdsType.None;
#endif
        }

        protected override void OnNativePlatformShow()
        {
#if USE_ADMOB
            OnShow += () => RegisterConfig();
            base.OnNativePlatformShow();
#endif
        }

        protected override void RegisterConfig()
        {

        }
    }
}

#endif
