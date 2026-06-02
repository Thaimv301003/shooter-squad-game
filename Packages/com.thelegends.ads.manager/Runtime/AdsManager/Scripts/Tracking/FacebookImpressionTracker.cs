#if USE_FACEBOOK
using System.Collections.Generic;
using TheLegends.Base.Facebook;

namespace TheLegends.Base.Ads.Tracking
{
    public class FacebookImpressionTracker : IImpressionTracker
    {
        private List<AdsType> _trackedTypes;

        public void Initialize(AdsSettings settings)
        {
            _trackedTypes = settings.facebookTrackedTypes ?? new List<AdsType>();
        }

        public bool CanTrack(AdsType adsType)
        {
            return _trackedTypes.Contains(adsType);
        }

        public void Track(ImpressionData data)
        {
            FacebookManager.Instance.LogEvent("AdImpression", (float)data.Revenue, new Dictionary<string, object>()
            {
                { "ad_platform", data.AdMediation.ToString() },
                { "ad_network", data.AdNetwork },
                { "ad_format", data.AdFormat },
                { "ad_unit_name", data.AdUnitName },
                { "country", data.Country },
                { "revenue", data.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "currency", "USD" },
                { "placement", data.Placement }
            });
        }
    }
}
#endif
