#if USE_ADMOB
using System.Collections.Generic;
using GoogleMobileAds.Common;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// Một IResponseInfoClient "giả" để tránh lỗi NullReferenceException trong Editor.
    /// </summary>
    internal class DummyResponseInfoClient : IResponseInfoClient
    {
        public string GetMediationAdapterClassName() => "DummyAdapter";

        public string GetResponseId() => "dummy_response_id";

        public IAdapterResponseInfoClient GetLoadedAdapterResponseInfo() => new DummyAdapterResponseInfoClient();

        public List<IAdapterResponseInfoClient> GetAdapterResponses()
        {
            return new List<IAdapterResponseInfoClient> { new DummyAdapterResponseInfoClient() };
        }

        public Dictionary<string, string> GetResponseExtras() => new Dictionary<string, string>();

        public override string ToString() => "DummyResponseInfo (Editor)";
    }
}
#endif
