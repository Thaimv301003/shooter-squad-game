#if USE_MAX

using System;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public class MaxMrecController : AdsPlacementBase
    {
        protected bool isReady = false;
        private Vector2Int adsOffset = Vector2Int.zero;
        private AdsPos adsPos = AdsPos.Bottom;
        public override AdsMediation GetAdsMediation()
        {
#if USE_MAX
            return AdsMediation.Max;
#else
            return AdsMediation.None;
#endif
        }

        public override AdsType GetAdsType()
        {
#if USE_MAX
            return AdsType.Mrec;
#else
            return AdsType.None;
#endif
        }

        public override bool IsAdsReady()
        {
#if USE_MAX
            return isReady;
#else
            return false;
#endif
        }

        public void ShowAds(AdsPos position, Vector2Int offset, string showPosition)
        {
#if USE_MAX
            this.adsOffset = offset;
            this.adsPos = position;
            LoadAds();
#endif
        }

        public override void ShowAds(string showPosition)
        {
            base.ShowAds(showPosition);
#if USE_MAX
            if (IsReady && IsAvailable)
            {
                OnAdsShowSuccess();
                MaxSdk.ShowMRec(adsUnitID);
            }
#endif
        }

        public override void LoadAds()
        {
#if USE_MAX
            if (!IsCanLoadAds())
            {
                return;
            }

            if (IsReady)
            {
                return;
            }

            base.LoadAds();

            CreateMRec(adsPos, adsOffset);

            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecRevenuePaidEvent;

            MaxSdk.LoadMRec(adsUnitID);
#endif
        }

        private void CreateMRec(AdsPos position, Vector2Int offset)
        {
#if USE_MAX
            var adPosition = SetAdCustomPosition(position, offset);
            MaxSdk.CreateMRec(adsUnitID, adPosition.x, adPosition.y);
#endif
        }

        #region Internal

        protected virtual void OnMRecLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {

            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                if (_loadRequestId != _currentLoadRequestId) return;

                StopHandleTimeout();

                isReady = true;
                OnAdsLoadAvailable();
                ShowAds(position);
            });
        }

        protected virtual void OnMRecLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                if (_loadRequestId != _currentLoadRequestId) return;

                StopHandleTimeout();

                MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnMRecLoadedEvent;
                MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnMRecLoadFailedEvent;
                MaxSdkCallbacks.MRec.OnAdClickedEvent -= OnMRecClickedEvent;
                MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecRevenuePaidEvent;

                OnAdsLoadFailed(errorInfo.Message);
            });
        }

        protected void OnMRecClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                OnAdsClick();
            });
        }

        protected void OnMRecRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            PimDeWitte.UnityMainThreadDispatcher.UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (adUnitId != adsUnitID) return;

                AdsManager.Instance.LogImpressionData(AdsMediation, AdsType, adsUnitID, networkName, position, adInfo);
            });
        }

        protected void MRecDestroy()
        {
#if USE_MAX
            if (IsReady)
            {
                try
                {
                    MaxSdk.DestroyMRec(adsUnitID);
                }
                catch (Exception ex)
                {
                    AdsManager.Instance.LogException(ex);
                }
            }
#endif
        }

        #endregion

        public virtual void HideAds()
        {
#if USE_MAX
            if (Status != AdsEvents.ShowSuccess && Status != AdsEvents.Click)
            {
                AdsManager.Instance.LogError($"{AdsMediation}_{AdsType} " + " is not showing --> return");
                return;
            }

            if (IsReady)
            {
                MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnMRecLoadedEvent;
                MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnMRecLoadFailedEvent;
                MaxSdkCallbacks.MRec.OnAdClickedEvent -= OnMRecClickedEvent;
                MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecRevenuePaidEvent;

                MaxSdk.HideBanner(adsUnitID);
                MRecDestroy();
                Status = AdsEvents.Close;
                adsUnitIDIndex = 0;

                isReady = false;
            }
#endif
        }

        public Vector2Int SetAdCustomPosition(AdsPos position, Vector2Int offset)
        {
            var density = MaxSdkUtils.GetScreenDensity();

            float adWidth = 300;
            float adHeight = 250;

            Debug.Log("AAAAA " + "adWidthMrec: " + adWidth + " adHeightMrec: " + adHeight);

            var safeAreaWidth = Screen.width / density;
            var safeAreaHeight = Screen.height / density;

            int xMax = (int)(safeAreaWidth - adWidth);
            int yMax = (int)(safeAreaHeight - adHeight);
            int xCenter = xMax / 2;
            int yCenter = yMax / 2;

            Vector2Int newPos = Vector2Int.zero;

            switch (position)
            {
                case AdsPos.Top:
                    newPos = new Vector2Int(xCenter + offset.x, offset.y);

                    break;
                case AdsPos.TopLeft:
                    newPos = new Vector2Int(offset.x, offset.y);

                    break;
                case AdsPos.TopRight:
                    newPos = new Vector2Int(xMax + offset.x, offset.y);

                    break;
                case AdsPos.Center:
                    newPos = new Vector2Int(xCenter + offset.x, yCenter + offset.y);

                    break;
                case AdsPos.CenterLeft:
                    newPos = new Vector2Int(offset.x, yCenter + offset.y);

                    break;
                case AdsPos.CenterRight:
                    newPos = new Vector2Int(xMax + offset.x, yCenter + offset.y);

                    break;
                case AdsPos.Bottom:
                    newPos = new Vector2Int(xCenter + offset.x, yMax + offset.y);

                    break;
                case AdsPos.BottomLeft:
                    newPos = new Vector2Int(offset.x, yMax + offset.y);

                    break;
                case AdsPos.BottomRight:
                    newPos = new Vector2Int(xMax + offset.x, yMax + offset.y);
                    break;
            }


            return newPos;
        }
    }
}

#endif