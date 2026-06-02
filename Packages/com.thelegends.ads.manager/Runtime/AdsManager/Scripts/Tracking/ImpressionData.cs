using System.Collections.Generic;

namespace TheLegends.Base.Ads.Tracking
{
    public struct ImpressionData
    {
        public AdsMediation AdMediation;
        public AdsType AdsType;
        public string AdNetwork;
        public string AdUnitName;
        public string AdFormat;
        public string Placement;
        public string Country;
        public string Currency;
        public double Revenue;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "ad_platform", AdMediation.ToString() },
                { "ad_network", AdNetwork },
                { "ad_format", AdFormat },
                { "ad_unit_name", AdUnitName },
                { "country", Country },
                { "revenue", Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "currency", Currency },
                { "placement", Placement }
            };
        }
    }
}
