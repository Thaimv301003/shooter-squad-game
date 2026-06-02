#if USE_MAX

namespace TheLegends.Base.Ads
{
    public class MaxMrecOpenController : MaxMrecController
    {
        public override AdsType GetAdsType()
        {
#if USE_MAX
            return AdsType.MrecOpen;
#else
            return AdsType.None;
#endif
        }

        public override void HideAds()
        {
#if USE_MAX
            MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnMRecLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnMRecLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent -= OnMRecClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecRevenuePaidEvent;

            MaxSdk.HideBanner(adsUnitID);
            MRecDestroy();

            Status = AdsEvents.Close;
            isReady = false;

#endif
        }

        protected override void OnMRecLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {     
            if (Status == AdsEvents.Close)
            {
                return;
            }
            base.OnMRecLoadedEvent(adUnitId, adInfo);
        }

        protected override void OnMRecLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (Status == AdsEvents.Close)
            {
                return;
            }
            base.OnMRecLoadFailedEvent(adUnitId, errorInfo);
        }
    }
}

#endif
