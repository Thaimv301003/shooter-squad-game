#if USE_APPSFLYER
using System;
using System.Collections.Generic;
using TheLegends.Base.AppsFlyer;
using AppsFlyerSDK;

namespace TheLegends.Base.Ads.Tracking
{
    public class AppsFlyerImpressionTracker : IImpressionTracker
    {
        private List<AdsType> _trackedTypes;

        public void Initialize(AdsSettings settings)
        {
            _trackedTypes = settings.appsFlyerTrackedTypes ?? new List<AdsType>();
        }

        public bool CanTrack(AdsType adsType)
        {
            return _trackedTypes.Contains(adsType);
        }

        public void Track(ImpressionData data)
        {
            var dict = new Dictionary<string, string>
            {
                { "ad_platform", data.AdMediation.ToString() },
                { "ad_network", data.AdNetwork },
                { "ad_format", data.AdFormat },
                { "ad_unit_name", data.AdUnitName },
                { "country", data.Country },
                { "revenue", data.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "currency", data.Currency },
                { "placement", data.Placement }
            };

            AppsFlyerManager.Instance.LogImpression(dict);

            string mediationStr = "GoogleAdMob";
            if (data.AdMediation == AdsMediation.Max) mediationStr = "ApplovinMax";
            else if (data.AdMediation == AdsMediation.Iron) mediationStr = "IronSource";

            if (Enum.TryParse<MediationNetwork>(mediationStr, out var appsflyersMediation))
            {
                AppsFlyerManager.Instance.LogRevenue(data.AdNetwork, appsflyersMediation, data.Currency, data.Revenue, new Dictionary<string, string>()
                {
                    { AdRevenueScheme.AD_UNIT, data.AdUnitName },
                    { AdRevenueScheme.AD_TYPE, data.AdFormat },
                    { AdRevenueScheme.COUNTRY, data.Country },
                    { AdRevenueScheme.PLACEMENT, data.Placement }
                });
            }
        }
    }
}
#endif
