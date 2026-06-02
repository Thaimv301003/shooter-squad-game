#if USE_ADMOB
using GoogleMobileAds.Common;

namespace TheLegends.Base.Ads
{
    /// <summary>
    /// Một ILoadAdErrorClient "giả" để đi kèm với DummyNativeClient.
    /// </summary>
    internal class DummyLoadAdErrorClient : ILoadAdErrorClient
    {
        private readonly string _message;
        public DummyLoadAdErrorClient(string message) { _message = message; }
        public int GetCode() => -1;
        public string GetDomain() => "com.thelegends.base.ads.dummy";
        public string GetMessage() => _message;
        public IAdErrorClient GetCause() => null;
        public IResponseInfoClient GetResponseInfoClient() => new DummyResponseInfoClient();
    }
}
#endif
