using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    [Serializable]
    public class AdmobUnitID
    {
        public List<Placement> bannerIds = new List<Placement>();
        public List<Placement> interIds = new List<Placement>();
        public List<Placement> rewardIds = new List<Placement>();
        public List<Placement> mrecIds = new List<Placement>();
        public List<Placement> appOpenIds = new List<Placement>();
        public List<Placement> interOpenIds = new List<Placement>();
        public List<Placement> mrecOpenIds = new List<Placement>();
        public List<Placement> nativeUnityIds = new List<Placement>();
        public List<Placement> nativeOverlayIds = new List<Placement>();
        public List<Placement> nativeBannerIds = new List<Placement>();
        public List<Placement> nativeInterIds = new List<Placement>();
        public List<Placement> nativeRewardIds = new List<Placement>();
        public List<Placement> nativeMrecIds = new List<Placement>();
        public List<Placement> nativeAppOpenIds = new List<Placement>();
        public List<Placement> nativeInterOpenIds = new List<Placement>();
        public List<Placement> nativeMrecOpenIds = new List<Placement>();
        public List<Placement> nativeVideoIds = new List<Placement>();
    }

    [Serializable]
    public class MaxUnitID
    {
        public List<Placement> bannerIds = new List<Placement>();
        public List<Placement> interIds = new List<Placement>();
        public List<Placement> interOpenIds = new List<Placement>();
        public List<Placement> rewardIds = new List<Placement>();
        public List<Placement> mrecIds = new List<Placement>();
        public List<Placement> mrecOpenIds = new List<Placement>();
        public List<Placement> appOpenIds = new List<Placement>();
    }



    [Serializable]
    public class Placement
    {
        public List<string> stringIDs = new List<string>();
    }
}
