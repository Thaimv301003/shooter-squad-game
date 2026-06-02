#if USE_ADMOB
using System.Collections.Generic;
using GoogleMobileAds.Common;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// Một IAdapterResponseInfoClient "giả" để hoàn thiện cấu trúc dữ liệu trong Editor.
    /// </summary>
    internal class DummyAdapterResponseInfoClient : IAdapterResponseInfoClient
    {
        public string AdapterClassName => "DummyAdapter";

        public long LatencyMillis => 0;

        public string AdSourceName => "DummySource";

        public string AdSourceId => "dummy_source_id";

        public string AdSourceInstanceName => "DummyInstance";

        public string AdSourceInstanceId => "dummy_instance_id";

        public IAdErrorClient AdError => null;

        public Dictionary<string, string> AdUnitMapping => new Dictionary<string, string>();

        public override string ToString() => "DummyAdapterResponse (Editor)";
    }
}
#endif
