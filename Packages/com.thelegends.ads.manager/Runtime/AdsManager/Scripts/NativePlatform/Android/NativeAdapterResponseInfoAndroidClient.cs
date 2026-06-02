#if USE_ADMOB
using System.Collections.Generic;
using GoogleMobileAds.Common;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    internal class NativeAdapterResponseInfoAndroidClient : IAdapterResponseInfoClient
    {
        private readonly AndroidJavaObject _javaObject;

        public NativeAdapterResponseInfoAndroidClient(AndroidJavaObject adapterResponseInfo)
        {
            _javaObject = adapterResponseInfo;
        }

        public string AdapterClassName
        {
            get { return _javaObject.Call<string>("getAdapterClassName"); }
        }

        public long LatencyMillis
        {
            get { return _javaObject.Call<long>("getLatencyMillis"); }
        }

        public string AdSourceName
        {
            get { return _javaObject.Call<string>("getAdSourceName"); }
        }

        public string AdSourceId
        {
            get { return _javaObject.Call<string>("getAdSourceId"); }
        }

        public string AdSourceInstanceName
        {
            get { return _javaObject.Call<string>("getAdSourceInstanceName"); }
        }

        public string AdSourceInstanceId
        {
            get { return _javaObject.Call<string>("getAdSourceInstanceId"); }
        }

        public IAdErrorClient AdError
        {
            get
            {
                AndroidJavaObject errorJO = _javaObject.Call<AndroidJavaObject>("getAdError");
                return errorJO != null ? new AdmobNativePlatformAndroidAdErrorClient(errorJO) : null;
            }
        }

        public Dictionary<string, string> AdUnitMapping
        {
            get
            {
                AndroidJavaObject bundle = _javaObject.Call<AndroidJavaObject>("getCredentials");
                if (bundle == null)
                {
                    return new Dictionary<string, string>();
                }
                return ConvertBundleToDictionary(bundle);
            }
        }

        private Dictionary<string, string> ConvertBundleToDictionary(AndroidJavaObject bundle)
        {
            var dictionary = new Dictionary<string, string>();
            AndroidJavaObject keySet = bundle.Call<AndroidJavaObject>("keySet");
            AndroidJavaObject[] keys = keySet.Call<AndroidJavaObject[]>("toArray");

            foreach (var key in keys)
            {
                string keyString = key.Call<string>("toString");
                string valueString = bundle.Call<string>("getString", keyString);
                if (valueString != null)
                {
                    dictionary[keyString] = valueString;
                }
            }
            return dictionary;
        }

        public override string ToString()
        {
            return _javaObject.Call<string>("toString");
        }
    }
}
#endif
