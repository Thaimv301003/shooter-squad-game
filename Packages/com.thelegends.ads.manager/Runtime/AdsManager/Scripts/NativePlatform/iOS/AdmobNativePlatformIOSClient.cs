#if USE_ADMOB && UNITY_IOS && !UNITY_EDITOR

using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System.Collections.Generic;

namespace TheLegends.Base.Ads
{
    public class AdmobNativePlatformIOSClient : IAdmobNativePlatformClient
    {
        // === Events của Interface ===
        public event EventHandler<EventArgs> OnAdLoaded;
        public event EventHandler<LoadAdErrorClientEventArgs> OnAdFailedToLoad;
        public event Action OnAdClosed;
        public event Action OnAdShow;
        public event Action<AdValue> OnPaidEvent;
        public event EventHandler<EventArgs> OnAdDidRecordImpression;
        public event Action OnAdClicked;
        public event Action OnVideoStart;
        public event Action OnVideoEnd;
        public event EventHandler<bool> OnVideoMute;
        public event Action OnVideoPlay;
        public event Action OnVideoPause;
        public event Action OnAdShowedFullScreenContent;
        public event Action OnAdDismissedFullScreenContent;

        private IntPtr _nativeControllerPtr = IntPtr.Zero;

        // MARK: - DllImport Declarations

        [DllImport("__Internal")]
        private static extern IntPtr AdmobNative_Create();

        [DllImport("__Internal")]
        private static extern void AdmobNative_Destroy(IntPtr handle);

        [DllImport("__Internal")]
        private static extern void AdmobNative_RegisterCallbacks(
            IntPtr handle,
            VoidCallback onAdLoaded,
            ErrorCallback onAdFailedToLoad,
            VoidCallback onAdShow,
            VoidCallback onAdClosed,
            PaidEventCallback onPaidEvent,
            VoidCallback onAdDidRecordImpression,
            VoidCallback onAdClicked,
            VoidCallback onVideoStart,
            VoidCallback onVideoEnd,
            VideoMuteCallback onVideoMute,
            VoidCallback onVideoPlay,
            VoidCallback onVideoPause,
            VoidCallback onAdShowedFullScreenContent,
            VoidCallback onAdDismissedFullScreenContent
        );

        [DllImport("__Internal")]
        private static extern void AdmobNative_LoadAd(IntPtr handle, string adUnitId);

        [DllImport("__Internal")]
        private static extern void AdmobNative_ShowAd(IntPtr handle, string layoutName);

        [DllImport("__Internal")]
        private static extern void AdmobNative_DestroyAd(IntPtr handle);

        [DllImport("__Internal")]
        private static extern bool AdmobNative_IsAdAvailable(IntPtr handle);

        [DllImport("__Internal")]
        private static extern void AdmobNative_WithCountdown(IntPtr handle, float initial, float duration, float closeDelay);

        [DllImport("__Internal")]
        private static extern void AdmobNative_WithPosition(IntPtr handle, int x, int y);

        [DllImport("__Internal")]
        private static extern float AdmobNative_GetWidthInPixels(IntPtr handle);

        [DllImport("__Internal")]
        private static extern float AdmobNative_GetHeightInPixels(IntPtr handle);

        // MARK: - Callback Delegates

        // Delegate types for native callbacks
        private delegate void VoidCallback(IntPtr nativeClient);
        private delegate void ErrorCallback(IntPtr nativeClient, string errorMessage);
        private delegate void PaidEventCallback(IntPtr nativeClient, int precisionType, long valueMicros, string currencyCode);
        private delegate void VideoMuteCallback(IntPtr nativeClient, bool isMuted);

        private static readonly Dictionary<IntPtr, AdmobNativePlatformIOSClient> _instances = new Dictionary<IntPtr, AdmobNativePlatformIOSClient>();

        // MARK: - Constructor & Initialization
        public AdmobNativePlatformIOSClient()
        {
            _nativeControllerPtr = AdmobNative_Create();
            _instances[_nativeControllerPtr] = this;

            AdmobNative_RegisterCallbacks(
                _nativeControllerPtr,
                OnAdLoadedCallback,
                OnAdFailedToLoadCallback,
                OnAdShowCallback,
                OnAdClosedCallback,
                OnPaidEventCallback,
                OnAdDidRecordImpressionCallback,
                OnAdClickedCallback,
                OnVideoStartCallback,
                OnVideoEndCallback,
                OnVideoMuteCallback,
                OnVideoPlayCallback,
                OnVideoPauseCallback,
                OnAdShowedFullScreenContentCallback,
                OnAdDismissedFullScreenContentCallback
            );
        }

        ~AdmobNativePlatformIOSClient()
        {
            DestroyAd();
        }

        public void Initialize()
        {
            // The constructor now handles initialization.
        }

        // MARK: - Interface Implementation

        public void LoadAd(string adUnitId, AdRequest request)
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                Debug.LogError("AdmobNativePlatformIOSClient: Controller not initialized");
                return;
            }

            Debug.Log($"AdmobNativePlatformIOSClient: Loading ad with ID: {adUnitId}");
            AdmobNative_LoadAd(_nativeControllerPtr, adUnitId);
        }

        public void ShowAd(string layoutName)
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                Debug.LogError("AdmobNativePlatformIOSClient: Controller not initialized");
                return;
            }

            Debug.Log($"AdmobNativePlatformIOSClient: Showing ad with layout: {layoutName}");
            AdmobNative_ShowAd(_nativeControllerPtr, layoutName);
        }

        public void DestroyAd()
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                return;
            }

            Debug.Log("AdmobNativePlatformIOSClient: Destroying ad");
            AdmobNative_Destroy(_nativeControllerPtr);
            if (_instances.ContainsKey(_nativeControllerPtr))
            {
                _instances.Remove(_nativeControllerPtr);
            }
            _nativeControllerPtr = IntPtr.Zero;
        }

        public bool IsAdAvailable()
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                return false;
            }

            return AdmobNative_IsAdAvailable(_nativeControllerPtr);
        }

        public IResponseInfoClient GetResponseInfoClient()
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                return null;
            }

            return new NativeResponseInfoIOSClient(_nativeControllerPtr);
        }

        // MARK: - Builder Pattern Support

        public void WithCountdown(float initialDelaySeconds, float countdownDurationSeconds, float closeButtonDelaySeconds)
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                Debug.LogError("AdmobNativePlatformIOSClient: Controller not initialized");
                return;
            }

            Debug.Log($"AdmobNativePlatformIOSClient: WithCountdown({initialDelaySeconds}, {countdownDurationSeconds}, {closeButtonDelaySeconds})");
            AdmobNative_WithCountdown(_nativeControllerPtr, initialDelaySeconds, countdownDurationSeconds, closeButtonDelaySeconds);
        }

        public void WithPosition(int positionX, int positionY)
        {
            if (_nativeControllerPtr == IntPtr.Zero)
            {
                Debug.LogError("AdmobNativePlatformIOSClient: Controller not initialized");
                return;
            }

            Debug.Log($"AdmobNativePlatformIOSClient: WithPosition({positionX}, {positionY})");
            AdmobNative_WithPosition(_nativeControllerPtr, positionX, positionY);
        }

        // MARK: - MonoPInvokeCallback Methods (Static callbacks từ native)

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdLoadedCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdLoaded callback");
                    client.OnAdLoaded?.Invoke(client, EventArgs.Empty);
                }
            });
        }

        [MonoPInvokeCallback(typeof(ErrorCallback))]
        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void OnAdFailedToLoadCallback(IntPtr nativeClient, string errorMessage)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.LogError($"AdmobNativePlatformIOSClient: OnAdFailedToLoad - {errorMessage}");
                    var errorClient = new AdmobNativePlatformIOSAdErrorClient(errorMessage);
                    var args = new LoadAdErrorClientEventArgs { LoadAdErrorClient = errorClient };
                    client.OnAdFailedToLoad?.Invoke(client, args);
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdShowCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdShow callback");
                    client.OnAdShow?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdClosedCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdClosed callback");
                    client.OnAdClosed?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(PaidEventCallback))]
        [MonoPInvokeCallback(typeof(PaidEventCallback))]
        private static void OnPaidEventCallback(IntPtr nativeClient, int precisionType, long valueMicros, string currencyCode)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log($"AdmobNativePlatformIOSClient: OnPaidEvent - {valueMicros} {currencyCode} (precision: {precisionType})");
                    var adValue = new AdValue { Precision = (AdValue.PrecisionType)precisionType, Value = valueMicros, CurrencyCode = currencyCode };
                    client.OnPaidEvent?.Invoke(adValue);
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdDidRecordImpressionCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdDidRecordImpression callback");
                    client.OnAdDidRecordImpression?.Invoke(client, EventArgs.Empty);
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdClickedCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdClicked callback");
                    client.OnAdClicked?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnVideoStartCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnVideoStart callback");
                    client.OnVideoStart?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnVideoEndCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnVideoEnd callback");
                    client.OnVideoEnd?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VideoMuteCallback))]
        [MonoPInvokeCallback(typeof(VideoMuteCallback))]
        private static void OnVideoMuteCallback(IntPtr nativeClient, bool isMuted)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log($"AdmobNativePlatformIOSClient: OnVideoMute callback - {isMuted}");
                    client.OnVideoMute?.Invoke(client, isMuted);
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnVideoPlayCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnVideoPlay callback");
                    client.OnVideoPlay?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnVideoPauseCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnVideoPause callback");
                    client.OnVideoPause?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdShowedFullScreenContentCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdShowedFullScreenContent callback");
                    client.OnAdShowedFullScreenContent?.Invoke();
                }
            });
        }

        [MonoPInvokeCallback(typeof(VoidCallback))]
        [MonoPInvokeCallback(typeof(VoidCallback))]
        private static void OnAdDismissedFullScreenContentCallback(IntPtr nativeClient)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_instances.TryGetValue(nativeClient, out var client))
                {
                    Debug.Log("AdmobNativePlatformIOSClient: OnAdDismissedFullScreenContent callback");
                    client.OnAdDismissedFullScreenContent?.Invoke();
                }
            });
        }
    }
}

#endif
