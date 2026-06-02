using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheLegends.Base.Ads
{
    [Serializable] [Flags]
    public enum AdsMediation
    {
        None = -1,
        Iron = 1,
        Max = 2,
        Admob = 4,
    }
}

