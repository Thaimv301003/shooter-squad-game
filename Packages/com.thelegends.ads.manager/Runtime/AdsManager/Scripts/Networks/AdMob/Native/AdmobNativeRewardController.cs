#if USE_ADMOB
namespace TheLegends.Base.Ads
{
    public class AdmobNativeRewardController : AdmobNativePlatformController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeReward;
#else
            return AdsType.None;
#endif
        }
    }
}

#endif
