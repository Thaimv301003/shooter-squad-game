#if BFUN_INSTALLED_TRUE
using System;
using TMPro;
using LitMotion;
using LitMotion.Adapters;
using LitMotion.Animation;
using UnityEngine;

[Serializable]
[LitMotionAnimationComponentMenu("UI/TextMesh Pro/Typewriter")]
public sealed class TMPTypewriterAnimationComponent : SlidePropertyAnimationComponent<TMP_Text, int, IntegerOptions, IntMotionAdapter>
{
    // 1. Ép giá trị "Gốc" luôn là 0 để mỗi lần Play nó luôn bắt đầu từ chuỗi rỗng
    protected override int GetCurrentValue(TMP_Text target) => 0;

    // 2. Lấy giá trị kết thúc là độ dài thực tế của Text tại thời điểm Play
    protected override int GetValue(TMP_Text target)
    {
        if (target == null) return 0;

        // QUAN TRỌNG: Buộc TMP cập nhật thông số ngay lập tức để lấy đúng characterCount
        // nếu bạn vừa mới gán text ở dòng code ngay phía trên.
        target.ForceMeshUpdate();

        return target.textInfo.characterCount;
    }

    protected override int GetInitValue(TMP_Text target) => 0;

    protected override void SetValue(TMP_Text target, in int value)
    {
        if (target == null) return;
        target.maxVisibleCharacters = value;
    }

    // 3. Đổi SlideMode về FALSE
    // Theo logic Base Class: 
    // finalStart = originalValue (luôn là 0 nhờ override GetCurrentValue)
    // finalEnd = GetValue(target) (độ dài text hiện tại)
    protected override bool GetSlideMode() => false;
}
#endif