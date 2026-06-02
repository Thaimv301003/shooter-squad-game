#if BFUN_INSTALLED_TRUE
using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion.Animation;
using UnityEngine;

[Serializable]
[LitMotionAnimationComponentMenu("UI/Slide/RightIn")]
public sealed class SlideRightInComponent : SlideVector2PropertyAnimationComponent<RectTransform>
{
    protected override Vector2 GetValue(RectTransform target) => target.localPosition;
    protected override void SetValue(RectTransform target, in Vector2 value) => target.localPosition = value;

    protected override Vector2 GetInitValue(RectTransform target) => new Vector2((Screen.currentResolution.width / 2) + (target.rect.width / 2) + 100, target.localPosition.y);

    protected override bool GetSlideMode() => true;

}
#endif