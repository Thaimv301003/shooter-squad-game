#if USE_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public interface IAdmobNativePlatformClient
    {
        // === EVENTS ===
        event EventHandler<EventArgs> OnAdLoaded;
        event EventHandler<LoadAdErrorClientEventArgs> OnAdFailedToLoad;
        event Action OnAdClosed;
        event Action OnAdShow;
        event Action<AdValue> OnPaidEvent;
        event EventHandler<EventArgs> OnAdDidRecordImpression;
        event Action OnAdClicked;
        event Action OnVideoStart;
        event Action OnVideoEnd;
        event EventHandler<bool> OnVideoMute;
        event Action OnVideoPlay;
        event Action OnVideoPause;
        event Action OnAdShowedFullScreenContent;
        event Action OnAdDismissedFullScreenContent;

        // === METHODS ===
        void Initialize();
        void LoadAd(string adUnitId, AdRequest request);
        void ShowAd(string layoutName);
        void DestroyAd();
        bool IsAdAvailable();
        IResponseInfoClient GetResponseInfoClient();

        // === BUILDER PATTERN SUPPORT ===
        void WithCountdown(float initialDelaySeconds, float countdownDurationSeconds, float closeButtonDelaySeconds);
        void WithPosition(int positionX, int positionY);
    }
}

#endif