#if USE_ADMOB

using GoogleMobileAds.Common;
using UnityEngine;

namespace TheLegends.Base.Ads
{

    internal class AdmobNativePlatformAndroidAdErrorClient : ILoadAdErrorClient
    {
        private readonly AndroidJavaObject _javaObject;

        public AdmobNativePlatformAndroidAdErrorClient(AndroidJavaObject javaLoadAdError)
        {
            _javaObject = javaLoadAdError;
        }

        public int GetCode() => _javaObject.Call<int>("getCode");
        public string GetDomain() => _javaObject.Call<string>("getDomain");
        public string GetMessage() => _javaObject.Call<string>("getMessage");
        public IAdErrorClient GetCause() => null;
        public IResponseInfoClient GetResponseInfoClient() => null;
        public override string ToString() => GetMessage();
    }
}

#endif