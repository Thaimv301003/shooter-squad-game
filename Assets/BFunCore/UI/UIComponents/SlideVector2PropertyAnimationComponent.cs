#if BFUN_INSTALLED_TRUE
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

public abstract class SlideVector2PropertyAnimationComponent<TObject> : SlidePropertyAnimationComponent<TObject, Vector2, NoOptions, Vector2MotionAdapter> where TObject : Object
{
    // Override hàm này để lấy đúng vị trí LOCAL hiện tại làm gốc
    protected override Vector2 GetCurrentValue(TObject target)
    {
        if (target is RectTransform rect) return rect.localPosition;
        if (target is Transform trans) return trans.localPosition;
        return base.GetCurrentValue(target);
    }
}
#endif