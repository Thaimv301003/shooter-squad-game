#if BFUN_INSTALLED_TRUE
using LitMotion;
using LitMotion.Animation;
using UnityEngine;

public abstract class SlidePropertyAnimationComponent<TObject, TValue, TOptions, TAdapter> : LitMotionAnimationComponent
    where TObject : Object
    where TValue : unmanaged
    where TOptions : unmanaged, IMotionOptions
    where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
{
    [SerializeField] protected TObject target;
    [SerializeField] private SerializableMotionSettings<TValue, TOptions> settings;

    protected TValue originalValue;
    protected bool hasCapturedOriginal;

    protected virtual TValue GetCurrentValue(TObject target) { return GetValue(target); }

    public override void RecordState()
    {
        if (!hasCapturedOriginal && target != null)
        {
            originalValue = GetCurrentValue(target);
            hasCapturedOriginal = true;
        }
    }

    public override void RestoreState()
    {
        if (target != null && hasCapturedOriginal)
        {
            SetValue(target, originalValue);
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(target);
#endif
        }
    }

    public override void ClearCache() { hasCapturedOriginal = false; }
    public override void OnStop() { RestoreState(); }

    public override bool OnInitialize()
    {
        if (target != null)
        {
            if (!hasCapturedOriginal) { originalValue = GetCurrentValue(target); hasCapturedOriginal = true; }
            if (GetSlideMode()) SetValue(target, GetInitValue(target));
            else SetValue(target, originalValue);
            return true;
        }
        return false;
    }

    public override MotionHandle Play()
    {
        if (!hasCapturedOriginal) { originalValue = GetCurrentValue(target); hasCapturedOriginal = true; }

        TValue finalStart;
        TValue finalEnd;

        if (GetSlideMode()) { finalStart = GetInitValue(target); finalEnd = originalValue; }
        else { finalStart = originalValue; finalEnd = GetValue(target); }

        SetValue(target, finalStart);

        onPlay?.Invoke();

        return LMotion.Create<TValue, TOptions, TAdapter>(finalStart, finalEnd, settings.Duration)
            .WithEase(settings.Ease)
            .WithDelay(settings.Delay)
            .WithLoops(settings.Loops, settings.LoopType)
            .WithOnComplete(() => onComplete?.Invoke()) // <--- Thêm Event OnComplete
            .Bind(this, (x, state) => state.SetValue(target, x));
    }

    protected abstract TValue GetValue(TObject target);
    protected abstract TValue GetInitValue(TObject target);
    protected abstract void SetValue(TObject target, in TValue value);
    protected abstract bool GetSlideMode();
}
#endif