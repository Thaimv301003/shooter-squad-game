#if USE_DATABUCKETS
using System.Collections.Generic;
using TheLegends.Base.Databuckets;

namespace TheLegends.Base.Ads.Tracking
{
    public class DatabucketsImpressionTracker : IImpressionTracker
    {
        private List<AdsType> _trackedTypes;

        public void Initialize(AdsSettings settings)
        {
            _trackedTypes = settings.databucketsTrackedTypes ?? new List<AdsType>();
        }

        public bool CanTrack(AdsType adsType)
        {
            return _trackedTypes.Contains(adsType);
        }

        public void Track(ImpressionData data)
        {
            DatabucketsManager.Instance.RecordEvent("ad_impression", new Dictionary<string, object>
            {
                { "ad_format", data.AdFormat },
                { "ad_platform", data.AdMediation.ToString() },
                { "ad_network", data.AdNetwork },
                { "ad_unit_id", data.AdUnitName },
                { "placement", data.Placement },
                { "is_show", 1 },
                { "value", data.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            });
        }
    }
}
#endif
