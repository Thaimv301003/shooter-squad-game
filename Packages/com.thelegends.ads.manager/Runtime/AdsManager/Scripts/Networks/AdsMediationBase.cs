using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    public abstract class AdsMediationBase : MonoBehaviour
    {
        private AdsMediation mediationType;
        public AdsMediation MediationType { get => GetMediationType(); }
        public abstract IEnumerator DoInit();

        public abstract void LoadInterstitial(AdsType interType, PlacementOrder order);
        public abstract void ShowInterstitial(AdsType interType, PlacementOrder order, string position, Action OnClose = null);

        public abstract void LoadRewarded(PlacementOrder order);
        public abstract void ShowRewarded(PlacementOrder order, string position, Action OnRewarded = null);

        public abstract void LoadAppOpen(PlacementOrder order);
        public abstract void ShowAppOpen(PlacementOrder order, string position, Action OnClose = null);

        public abstract void LoadBanner(PlacementOrder order);
        public abstract void ShowBanner(PlacementOrder order, string position);
        public abstract void HideBanner(PlacementOrder order);

        public abstract void LoadMrec(AdsType mrecType, PlacementOrder order);
        public abstract void ShowMrec(AdsType mrecType, PlacementOrder order, AdsPos mrecPosition, Vector2Int offset, string position);
        public abstract void HideMrec(AdsType mrecType, PlacementOrder order);

        public abstract void HideAllBanner();
        public abstract void HideAllMrec();

        public abstract AdsEvents GetAdsStatus(AdsType adsType, PlacementOrder order);

        public virtual int GetPlacementInfo(AdsType adsType, out List<PlacementOrder> placementOrders)
        {
            placementOrders = new List<PlacementOrder>();

            foreach (PlacementOrder order in Enum.GetValues(typeof(PlacementOrder)))
            {
                if (IsAdsControllerExist(adsType, order))
                {
                    placementOrders.Add(order);
                }
            }

            return placementOrders.Count;
        }


        public abstract AdsMediation GetMediationType();
        public abstract void RemoveAds();
        public abstract bool IsAdsReady(AdsType adsType, PlacementOrder order);
        public abstract bool IsAdsControllerExist(AdsType adsType, PlacementOrder order);
    }
}

