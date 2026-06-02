// FILE: SuggestionItemClick.cs
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// IPointerClickHandler là một giao diện siêu nhẹ, chỉ bắt sự kiện click hoàn chỉnh (down và up trên cùng một đối tượng)
public class SuggestionItemClick : MonoBehaviour, IPointerClickHandler
{
    // Chúng ta sẽ dùng một UnityAction để báo cho DebugConsole biết khi nào nó được click
    public UnityAction<string> OnClicked;
    #if BFUN_INSTALLED_TRUE
    private TMPro.TMP_Text textComponent;

    private void Awake()
    {
        // Tự lấy component Text để biết nội dung của mình là gì
        textComponent = GetComponentInChildren<TMPro.TMP_Text>();
    }
    #endif

    public void OnPointerClick(PointerEventData eventData)
    {
            #if BFUN_INSTALLED_TRUE
        // Khi được click, gọi action và truyền nội dung text của mình đi
        if (OnClicked != null && textComponent != null)
        {
            OnClicked.Invoke(textComponent.text);
        }
        #endif
    }
    
}