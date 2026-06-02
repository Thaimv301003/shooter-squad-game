#if USE_ADMOB

namespace TheLegends.Base.Ads
{
    public class AdmobNativeMrecOpenController : AdmobNativePlatformController
    {
        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeMrecOpen;
#else
            return AdsType.None;
#endif
        }

        public override void OnAdsClosed()
        {
            Status = AdsEvents.Close;
            adsUnitIDIndex = 0;
        }
    }
}

#endif
