#if USE_ADMOB

namespace TheLegends.Base.Ads
{
    public class AdmobNativeVideoController : AdmobNativePlatformController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeVideo;
#else
            return AdsType.None;
#endif
        }
    }
}

#endif
