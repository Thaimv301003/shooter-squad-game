using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAdsEvents
{
    public void OnAdsLoadRequest();
    public void OnAdsLoadAvailable();
    public void OnAdsLoadFail();
    public void OnAdsShowSuccess();
    public void OnAdsShowFail();
    public void OnAdsShowComplete();
    public void OnAdsClose();
    public void OnAdsClick();
}
