#if BFUN_INSTALLED_TRUE
using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion.Animation;
using UnityEngine;

[Serializable]
[LitMotionAnimationComponentMenu("UI/Slide/BelowOut")]
public sealed class SlideBelowOutComponent : SlideVector2PropertyAnimationComponent<RectTransform>
{
    protected override Vector2 GetValue(RectTransform target) => new Vector2(target.localPosition.x, -((Screen.currentResolution.height / 2) + (target.rect.height / 2) + 100));
    protected override void SetValue(RectTransform target, in Vector2 value) => target.localPosition = value;

    protected override Vector2 GetInitValue(RectTransform target) => target.localPosition;

    protected override bool GetSlideMode() => false;

}
#endif