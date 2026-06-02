#if USE_ADMOB
using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace TheLegends.Base.Ads
{
    public class AdmobNativePlatform
    {
        private readonly IAdmobNativePlatformClient _client;

        private AdmobNativePlatform(IAdmobNativePlatformClient client)
        {
            _client = client;
            RegisterAdEvents();
        }

        public event Action<AdValue> OnAdPaid;
        public event Action OnAdClicked;
        public event EventHandler<EventArgs> OnAdDidRecordImpression;
        public event Action OnVideoStart;
        public event Action OnVideoEnd;
        public event EventHandler<bool> OnVideoMute;
        public event Action OnVideoPlay;
        public event Action OnVideoPause;
        public event Action OnAdClosed;
        public event Action OnAdShow;
        public event Action OnAdShowedFullScreenContent;
        public event Action OnAdDismissedFullScreenContent;

        public static void Load(string adUnitId, AdRequest request, Action<AdmobNativePlatform, LoadAdError> adLoadCallback)
        {
            if (adLoadCallback == null)
            {
                Debug.LogError("adLoadCallback cannot be null.");
                return;
            }

            IAdmobNativePlatformClient client;

#if UNITY_ANDROID && !UNITY_EDITOR
            client = new AdmobNativePlatformAndroidClient();
#elif UNITY_IOS && !UNITY_EDITOR
            client = new AdmobNativePlatformIOSClient();
#else
            client = new DummyNativeClient();
#endif


            client.Initialize();

            client.OnAdLoaded += (sender, args) =>
            {
                adLoadCallback(new AdmobNativePlatform(client), null);
            };

            client.OnAdFailedToLoad += (sender, args) =>
            {
                var nativeError = new LoadAdError(args.LoadAdErrorClient);
                adLoadCallback(null, nativeError);
            };

            client.LoadAd(adUnitId, request);
        }

        public void Show(string layoutName) => _client?.ShowAd(layoutName);
        public void Destroy() => _client?.DestroyAd();
        public bool IsAdAvailable() => _client != null && _client.IsAdAvailable();

        #region Builder Pattern Support - Forward to Native

        public void WithCountdown(float initialDelaySeconds, float countdownDurationSeconds, float closeButtonDelaySeconds)
        {
            _client?.WithCountdown(initialDelaySeconds, countdownDurationSeconds, closeButtonDelaySeconds);
        }

        public void WithPosition(int positionX, int positionY)
        {
            _client?.WithPosition(positionX, positionY);
        }

        #endregion

        public IResponseInfoClient GetResponseInfo()
        {
            return _client.GetResponseInfoClient();
        }

        private void RegisterAdEvents()
        {
            if (_client == null) return;
            _client.OnPaidEvent += (adValue) => OnAdPaid?.Invoke(adValue);
            _client.OnAdClicked += () => OnAdClicked?.Invoke();
            _client.OnAdDidRecordImpression += (sender, args) => OnAdDidRecordImpression?.Invoke(this, args);
            _client.OnVideoStart += () => OnVideoStart?.Invoke();
            _client.OnVideoEnd += () => OnVideoEnd?.Invoke();
            _client.OnVideoMute += (sender, isMuted) => OnVideoMute?.Invoke(this, isMuted);
            _client.OnVideoPlay += () => OnVideoPlay?.Invoke();
            _client.OnVideoPause += () => OnVideoPause?.Invoke();
            _client.OnAdClosed += () => OnAdClosed?.Invoke();
            _client.OnAdShow += () => OnAdShow?.Invoke();
            _client.OnAdShowedFullScreenContent += () => OnAdShowedFullScreenContent?.Invoke();
            _client.OnAdDismissedFullScreenContent += () => OnAdDismissedFullScreenContent?.Invoke();
        }
    }
}

#endif