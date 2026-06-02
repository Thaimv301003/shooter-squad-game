#if USE_ADMOB

namespace TheLegends.Base.Ads
{
    public class AdmobNativeAppOpenController : AdmobNativePlatformController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeAppOpen;
#else
            return AdsType.None;
#endif
        }
    }
}
#endif
