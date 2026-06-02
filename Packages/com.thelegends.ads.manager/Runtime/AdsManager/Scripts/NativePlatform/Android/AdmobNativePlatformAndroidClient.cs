#if USE_ADMOB

using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace TheLegends.Base.Ads
{
    public class AdmobNativePlatformAndroidClient : AndroidJavaProxy, IAdmobNativePlatformClient
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

        private AndroidJavaObject _kotlinController;

        public AdmobNativePlatformAndroidClient() : base("com.thelegends.admob_native_unity.NativeAdCallbacks") { }

        public void Initialize() { } 

        public void LoadAd(string adUnitId, AdRequest request)
        {
            var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            var adRequestJava = new AndroidJavaObject("com.google.android.gms.ads.AdRequest$Builder").Call<AndroidJavaObject>("build");

            _kotlinController = new AndroidJavaObject(
                "com.thelegends.admob_native_unity.AdmobNativeController",
                activity,
                this 
            );

            _kotlinController.Call("loadAd", adUnitId, adRequestJava);
        }

        public void ShowAd(string layoutName) => _kotlinController?.Call("showAd", layoutName);
        public void DestroyAd() => _kotlinController?.Call("destroyAd");
        public bool IsAdAvailable() => _kotlinController?.Call<bool>("isAdAvailable") ?? false;

        #region Builder Pattern Support - Direct Native Calls

        public void WithCountdown(float initialDelaySeconds, float countdownDurationSeconds, float closeButtonDelaySeconds)
        {
            _kotlinController?.Call<AndroidJavaObject>("withCountdown", initialDelaySeconds, countdownDurationSeconds, closeButtonDelaySeconds);
        }
        
        public void WithPosition(int positionX, int positionY)
        {
            _kotlinController?.Call<AndroidJavaObject>("withPosition", positionX, positionY);
        }


        #endregion

        public IResponseInfoClient GetResponseInfoClient()
        {
            return _kotlinController != null ? new AdmobNativePlatformAndroidResponseInfoClient (_kotlinController) : null;
        }

        #region Kotlin Callbacks Implementation

        void onAdLoaded()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdLoaded?.Invoke(this, EventArgs.Empty);
            });
        }


        void onAdFailedToLoad(AndroidJavaObject errorJO)
        {
            var args = new LoadAdErrorClientEventArgs()
            {
                LoadAdErrorClient = new AdmobNativePlatformAndroidAdErrorClient(errorJO)
            };
            
            OnAdFailedToLoad?.Invoke(this, args);
        }

        void onAdClosed()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdClosed?.Invoke();
            });
        }
        void onAdShow()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdShow?.Invoke();
            });
        }

        void onPaidEvent(int precision, long valueInMicros, string currencyCode)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdValue adValue = new AdValue()
                {
                    Precision = (AdValue.PrecisionType)precision,
                    Value = valueInMicros,
                    CurrencyCode = currencyCode
                };

                OnPaidEvent?.Invoke(adValue);
            });
        }
        void onAdDidRecordImpression()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                    OnAdDidRecordImpression?.Invoke(this, EventArgs.Empty);
            });
        }
        void onAdClicked()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdClicked?.Invoke();
            });
        }
        void onVideoStart()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnVideoStart?.Invoke();
            });
        }
        void onVideoEnd()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnVideoEnd?.Invoke();
            });
        }
        void onVideoMute(bool isMuted)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnVideoMute?.Invoke(this, isMuted);
            });
        }
        void onVideoPlay()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnVideoPlay?.Invoke();
            });
        }
        void onVideoPause()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnVideoPause?.Invoke();
            });
        }
        void onAdShowedFullScreenContent()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdShowedFullScreenContent?.Invoke();
            });
        }
        void onAdDismissedFullScreenContent()
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdDismissedFullScreenContent?.Invoke();
            });
        }

        #endregion
    }
}

#endif