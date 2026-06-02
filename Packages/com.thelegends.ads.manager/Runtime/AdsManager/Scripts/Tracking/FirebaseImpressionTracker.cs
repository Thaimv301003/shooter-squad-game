#if USE_FIREBASE
using System.Collections.Generic;
using TheLegends.Base.Firebase;

namespace TheLegends.Base.Ads.Tracking
{
    public class FirebaseImpressionTracker : IImpressionTracker
    {
        private List<AdsType> _trackedTypes;

        public void Initialize(AdsSettings settings)
        {
            _trackedTypes = settings.firebaseTrackedTypes ?? new List<AdsType>();
        }

        public bool CanTrack(AdsType adsType)
        {
            return _trackedTypes.Contains(adsType);
        }

        public void Track(ImpressionData data)
        {
            var dict = data.ToDictionary();

            if (data.AdMediation != AdsMediation.Admob)
            {
                FirebaseManager.Instance.LogEvent("ad_impression", dict);
            }

            FirebaseManager.Instance.LogEvent("taichi_ad_impression", dict);
        }
    }
}
#endif
