#if USE_ADMOB && UNITY_IOS && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GoogleMobileAds.Common;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class NativeAdapterResponseInfoIOSClient : IAdapterResponseInfoClient
    {
        private readonly IntPtr _adapterHandle;

        public NativeAdapterResponseInfoIOSClient(IntPtr adapterHandle)
        {
            _adapterHandle = adapterHandle;
        }

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_AdapterResponse_GetAdapterClassName(IntPtr handle);

        [DllImport("__Internal")]
        private static extern long AdmobNative_AdapterResponse_GetLatencyMillis(IntPtr handle);

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_AdapterResponse_GetAdSourceName(IntPtr handle);

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_AdapterResponse_GetAdSourceId(IntPtr handle);

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_AdapterResponse_GetAdSourceInstanceName(IntPtr handle);

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_AdapterResponse_GetAdSourceInstanceId(IntPtr handle);

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_AdapterResponse_GetAdError(IntPtr handle);

        public string AdapterClassName
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return null;
                IntPtr ptr = AdmobNative_AdapterResponse_GetAdapterClassName(_adapterHandle);
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
            }
        }

        public long LatencyMillis
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return 0;
                return AdmobNative_AdapterResponse_GetLatencyMillis(_adapterHandle);
            }
        }

        public string AdSourceName
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return null;
                IntPtr ptr = AdmobNative_AdapterResponse_GetAdSourceName(_adapterHandle);
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
            }
        }

        public string AdSourceId
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return null;
                IntPtr ptr = AdmobNative_AdapterResponse_GetAdSourceId(_adapterHandle);
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
            }
        }

        public string AdSourceInstanceName
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return null;
                IntPtr ptr = AdmobNative_AdapterResponse_GetAdSourceInstanceName(_adapterHandle);
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
            }
        }

        public string AdSourceInstanceId
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return null;
                IntPtr ptr = AdmobNative_AdapterResponse_GetAdSourceInstanceId(_adapterHandle);
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
            }
        }

        public IAdErrorClient AdError
        {
            get
            {
                if (_adapterHandle == IntPtr.Zero) return null;
                IntPtr ptr = AdmobNative_AdapterResponse_GetAdError(_adapterHandle);
                if (ptr == IntPtr.Zero) return null;
                
                string errorMsg = Marshal.PtrToStringAnsi(ptr);
                return new AdmobNativePlatformIOSAdErrorClient(errorMsg);
            }
        }

        public Dictionary<string, string> AdUnitMapping
        {
            get
            {
                // iOS: Currently not implementing detailed credentials mapping in simplified bridge
                return new Dictionary<string, string>();
            }
        }

        public override string ToString()
        {
            return $"NativeAdapterResponseInfoIOSClient [Adapter: {AdapterClassName}, Latency: {LatencyMillis}ms]";
        }
    }
}
#endif
