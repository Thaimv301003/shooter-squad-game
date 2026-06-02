#if USE_ADMOB && USE_ADMOB_NATIVE_UNITY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.UI;

namespace TheLegends.Base.Ads
{
    public class AdmobNativeController : AdsPlacementBase
    {
        private NativeAd _nativeAd;

        [SerializeField]
        private GameObject container;

        [SerializeField]
        private PlacementOrder _order = PlacementOrder.One;

        [SerializeField]
        private string positionNative = "default";

        private float timeAutoReload;
        private bool isCLosedByHide = false;

        [Space(10)]
        [Header("Native Components")]
        [SerializeField]
        private Image adImage;

        [SerializeField]
        private Sprite defaultAdImageSprite;

        [SerializeField]
        private BoxCollider2D adImageCollider;

        [SerializeField]
        private Image callToAction;

        [SerializeField]
        private Text callToActionText;

        [SerializeField]
        private Image adChoice;

        [SerializeField]
        private Sprite defaultAdChoiceSprite;

        [SerializeField]
        private Image adIcon;

        [SerializeField]
        private Sprite defaultAdIconSprite;

        [SerializeField]
        private Text advertiser;

        [SerializeField]
        private Text adHeadline;

        [SerializeField]
        private Text adBody;

        [SerializeField]
        private Text store;

        [SerializeField]
        private Image starFilling;

        [SerializeField]
        private Text price;

        [SerializeField]
        private AspectRatioFitter adImageAspectRatioFitter;

        [SerializeField]
        private Vector2Int iconMaxSize = new Vector2Int(50, 50);

        [SerializeField]
        private Vector2Int adImageMaxSize = new Vector2Int(200, 200);

        [SerializeField]
        private Vector2Int adChoiceMaxSize = new Vector2Int(50, 50);

        [Space(10)]
        [Header("Show On")]
        [SerializeField]
        private bool isShowOnLoaded = false;

        [SerializeField]
        private bool isShowOnLoadFailed = false;


        public Action onClick = null;

        protected AdLoader adLoader;



        private void Awake()
        {
            container.SetActive(false);

            position = positionNative;

            var platform = Application.platform;
            var isIOS = platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.OSXPlayer;
            var isTest = AdsManager.Instance.SettingsAds.isTest;

            var list = isTest
                ? (isIOS
                    ? AdsManager.Instance.SettingsAds.ADMOB_IOS_Test.nativeUnityIds
                    : AdsManager.Instance.SettingsAds.ADMOB_Android_Test.nativeUnityIds)
                : (isIOS
                    ? AdsManager.Instance.SettingsAds.ADMOB_IOS.nativeUnityIds
                    : AdsManager.Instance.SettingsAds.ADMOB_Android.nativeUnityIds);

            if (list.Count <= 0)
            {
                return;
            }

            var placementIndex = Mathf.Clamp((int)_order - 1, 0, list.Count - 1);
            placement = list[placementIndex];

            timeAutoReload = AdsManager.Instance.adsConfigs.adTimeReload;

            Init(placement, _order);

            AdsManager.Instance.OnCanShowAdsChanged += OnCanShowAdsChanged;
        }


        private void OnCanShowAdsChanged(bool isCanShowAds)
        {
            if (!isCanShowAds)
            {
                HideAds();
            }
        }

        public override void LoadAds()
        {
#if USE_ADMOB
            if (placement == null || placement.stringIDs.Count <= 0)
            {
                AdsManager.Instance.LogError("" + AdsMediation + "_" + AdsType + " " + "UnitId NULL or Empty --> return");
                return;
            }

            if (!CheckNativeAdAvailability())
            {
                return;
            }

            if (!IsCanLoadAds())
            {
                return;
            }

            if (IsAdsReady() && Status == AdsEvents.LoadAvailable)
            {
                return;
            }

            NativeDestroy();
            base.LoadAds();

            adLoader = new AdLoader.Builder(adsUnitID)
                .ForNativeAd()
                .Build();

            adLoader.OnNativeAdLoaded += OnNativeLoaded;
            adLoader.OnAdFailedToLoad += OnNativeLoadFailed;
            adLoader.OnNativeAdImpression += OnNativeImpression;
            adLoader.OnNativeAdClicked += OnNativeClick;
            adLoader.OnNativeAdClosed += OnNativeClose;
            adLoader.LoadAd(new AdRequest());

#if UNITY_EDITOR
            OnNativeLoaded(this, null);
#endif

#endif
        }

        public void ShowAds()
        {
            ShowAds(position);
        }


        public override void ShowAds(string showPosition)
        {
#if USE_ADMOB
            position = showPosition;

            if (!CheckNativeAdAvailability())
            {
                return;
            }

            if (Status == AdsEvents.ShowSuccess)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "is showing --> return");

                return;
            }

            base.ShowAds(showPosition);

#if UNITY_EDITOR
            if (IsAvailable)
            {
                OnAdsShowSuccess();
                container.SetActive(true);
                isCLosedByHide = false;

                if (adImageCollider && AdsManager.Instance.adsConfigs.adNativeBannerHeight > 0)
                {
                    adImageCollider.size = new Vector2(adImage.rectTransform.rect.width,
                        AdsManager.Instance.adsConfigs.adNativeBannerHeight);
                }

                CancelReloadAds();
                DelayReloadAd(timeAutoReload);
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }

#else
            if (IsReady && IsAvailable)
            {
                _nativeAd.OnPaidEvent += OnAdsPaid;
                OnAdsShowSuccess();
                isCLosedByHide = false;
                container.SetActive(true);
                FetchData();
                CancelReloadAds();
                DelayReloadAd(timeAutoReload);
            }
            else
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not ready --> Load Ads");
                reloadCount = 0;
                LoadAds();
            }
#endif

#endif
        }


        public override AdsMediation GetAdsMediation()
        {
#if USE_ADMOB
            return AdsMediation.Admob;
#else
            return AdsMediation.None;
#endif
        }

        public override AdsType GetAdsType()
        {
#if USE_ADMOB
            return AdsType.NativeUnity;
#else
            return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_ADMOB
            return _nativeAd != null;
#else
            return false;
#endif
        }

        #region Internal

        private bool CheckNativeAdAvailability()
        {
            if (!AdsManager.Instance.adsConfigs.isUseAdNative)
            {
                AdsManager.Instance.LogWarning($"{AdsMediation}_{AdsType} " + "is not use native --> return");
                NativeDestroy();
                OnAdsCancel();
                return false;
            }
            return true;
        }

        private void NativeDestroy()
        {
#if USE_ADMOB
            try
            {
                container.SetActive(false);

                CancelReloadAds();

                if (_nativeAd != null)
                {
                    _nativeAd.OnPaidEvent -= OnAdsPaid;
                    _nativeAd.Destroy();
                    _nativeAd = null;
                }

                if (adLoader != null)
                {
                    adLoader.OnNativeAdLoaded -= OnNativeLoaded;
                    adLoader.OnAdFailedToLoad -= OnNativeLoadFailed;
                    adLoader.OnNativeAdImpression -= OnNativeImpression;
                    adLoader.OnNativeAdClicked -= OnNativeClick;
                    adLoader.OnNativeAdClosed -= OnNativeClose;
                    adLoader = null;
                }

                // if (adImage != null)
                // {
                //     adImage.sprite = defaultAdImageSprite;
                // }

                // if (adIcon != null)
                // {
                //     adIcon.sprite = defaultAdIconSprite;
                // }

                // if (adChoice != null)
                // {
                //     adChoice.sprite = defaultAdChoiceSprite;
                // }

            }
            catch (Exception ex)
            {
                AdsManager.Instance.LogException(ex);
            }

#endif
        }

        private void OnNativeLoaded(object sender, NativeAdEventArgs args)
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_loadRequestId != _currentLoadRequestId)
                {
                    // If the load request ID does not match, this callback is from a previous request
                    return;
                }

                StopHandleTimeout();

                OnAdsLoadAvailable();

                if (args != null)
                {
                    _nativeAd = args.nativeAd;
                }

                networkName = _nativeAd.GetResponseInfo().GetMediationAdapterClassName();

                AdsManager.Instance.Log($"{AdsMediation}_{AdsType} " + "ad loaded with response : " + _nativeAd.GetResponseInfo());

                 FetchData();

                if (isCLosedByHide)
                {
                    AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + "last closed by Hide() --> return");

                    return;
                }

                adsUnitIDIndex = 0;

                if (isShowOnLoaded)
                {
                    ShowAds(position);
                }
            });
#endif
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private void OnNativeLoadFailed(object sender, AdFailedToLoadEventArgs error)
#pragma warning restore CS0618 // Type or member is obsolete
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_loadRequestId != _currentLoadRequestId)
                {
                    // If the load request ID does not match, this callback is from a previous request
                    return;
                }

                StopHandleTimeout();

                var errorDescription = error.LoadAdError.GetMessage();
                OnAdsLoadFailed(errorDescription);
            });
#endif
        }

        protected override void OnAdsLoadFailed(string message)
        {
            base.OnAdsLoadFailed(message);
            container.SetActive(isShowOnLoadFailed);

            if (Status == AdsEvents.LoadNotAvailable)
            {
                DelayReloadAd(timeAutoReload);
            }
        }

        private void OnNativeClose(object sender, EventArgs args)
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClosed();
                LoadAds();
            });
#endif
        }

        private void OnNativeClick(object sender, EventArgs args)
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAdsClick();
                CancelReloadAds();
                onClick?.Invoke();
            });
#endif
        }

        private void OnNativeImpression(object sender, EventArgs args)
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnImpression();
            });
#endif
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private void OnAdsPaid(object sender, AdValueEventArgs args)
#pragma warning restore CS0618 // Type or member is obsolete
        {
#if USE_ADMOB
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, args.AdValue);
            });
#endif
        }

        private void FetchData()
        {
            if (adImage)
            {
                List<Texture2D> images = _nativeAd.GetImageTextures();

                if (images.Count > 0)
                {
                    Texture2D image = images.FirstOrDefault();

                    if (image.width > adImageMaxSize.x || image.height > adImageMaxSize.y)
                    {
                        image = ResizeTexture(image, adImageMaxSize);
                    }


                    if (adImageAspectRatioFitter)
                    {
                        adImageAspectRatioFitter.aspectRatio = (float)image.width / image.height;
                    }

                    adImage.sprite = Sprite.Create(image, new Rect(0, 0, image.width, image.height),
                        new Vector2(0.5f, 0.5f));
                }

                if (adImageCollider && AdsManager.Instance.adsConfigs.adNativeBannerHeight > 0)
                {
                    adImageCollider.size = new Vector2(adImage.rectTransform.rect.width,
                        AdsManager.Instance.adsConfigs.adNativeBannerHeight);
                }

                _nativeAd.RegisterImageGameObjects(new List<GameObject> { adImage.gameObject });
            }


            if (adChoice)
            {
                Texture2D choice = _nativeAd.GetAdChoicesLogoTexture();

                if (choice != null)
                {
                    if (choice.width > adChoiceMaxSize.x || choice.height > adChoiceMaxSize.y)
                    {
                        choice = ResizeTexture(choice, adChoiceMaxSize);
                    }


                    adChoice.sprite = Sprite.Create(choice, new Rect(0, 0, choice.width, choice.height),
                        new Vector2(0.5f, 0.5f));
                }

                _nativeAd.RegisterAdChoicesLogoGameObject(adChoice.gameObject);
            }


            if (adIcon)
            {
                Texture2D icon = _nativeAd.GetIconTexture();

                if (icon != null)
                {

                    if (icon.width > iconMaxSize.x || icon.height > iconMaxSize.y)
                    {
                        icon = ResizeTexture(icon, iconMaxSize);
                    }


                    adIcon.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height),
                        new Vector2(0.5f, 0.5f));
                }

                _nativeAd.RegisterIconImageGameObject(adIcon.gameObject);
            }


            if (callToAction && callToActionText)
            {
                callToActionText.text = _nativeAd.GetCallToActionText().ToLower();

                _nativeAd.RegisterCallToActionGameObject(callToAction.gameObject);
            }


            if (advertiser)
            {
                advertiser.text = _nativeAd.GetAdvertiserText();

                _nativeAd.RegisterAdvertiserTextGameObject(advertiser.gameObject);
            }


            if (adHeadline)
            {
                adHeadline.text = _nativeAd.GetHeadlineText();

                _nativeAd.RegisterHeadlineTextGameObject(adHeadline.gameObject);
            }


            if (adBody)
            {
                adBody.text = _nativeAd.GetBodyText();

                _nativeAd.RegisterBodyTextGameObject(adBody.gameObject);
            }


            if (store)
            {
                store.text = _nativeAd.GetStore();

                _nativeAd.RegisterStoreGameObject(store.gameObject);
            }


            if (price)
            {
                price.text = _nativeAd.GetPrice();

                _nativeAd.RegisterPriceGameObject(price.gameObject);
            }


            if (starFilling)
            {
                double storeStarRating = _nativeAd.GetStarRating();

                if (storeStarRating is double.NaN || storeStarRating <= 0)
                {
                    storeStarRating = 4.25;
                }

                starFilling.fillAmount = (float)(storeStarRating * 0.2f);
            }

        }

        public void HideAds()
        {
            if (!CheckNativeAdAvailability())
            {
                return;
            }

            isCLosedByHide = true;
            NativeDestroy();
            OnAdsClosed();
            AdsManager.Instance.OnCanShowAdsChanged -= OnCanShowAdsChanged;
        }

        private void DelayReloadAd(float time)
        {
            Invoke(nameof(LoadAds), time);
        }

        private void CancelReloadAds()
        {
            CancelInvoke(nameof(LoadAds));
        }

        private Texture2D ResizeTexture(Texture2D sourceTexture, Vector2Int maxSize)
        {
            if (sourceTexture == null)
                return null;

            float ratioX = (float)maxSize.x / sourceTexture.width;
            float ratioY = (float)maxSize.y / sourceTexture.height;
            float ratio = Mathf.Min(ratioX, ratioY);

            int newWidth = Mathf.RoundToInt(sourceTexture.width * ratio);
            int newHeight = Mathf.RoundToInt(sourceTexture.height * ratio);

            // Tạo RenderTexture tạm thời
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;

            // Lưu RenderTexture hiện tại
            RenderTexture currentRT = RenderTexture.active;

            // Blit texture gốc vào RenderTexture mới
            Graphics.Blit(sourceTexture, rt);
            RenderTexture.active = rt;

            // Tạo texture mới với kích thước đã chỉ định
            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            resizedTexture.Apply();

            // Khôi phục RenderTexture và giải phóng tài nguyên
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(rt);

            return resizedTexture;
        }

        #endregion
    }
}

#endif
