using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BFunCoreKit
{
    [RequireComponent(typeof(Button))]
    public class GUIButton : MonoBehaviour
    {
        Button button;
        [SerializeField] SoundName sound;
        [HideInInspector] public UnityEvent onClick;

        void Awake()
        {
            button = GetComponent<Button>();
        }

        void OnEnable()
        {
            button.onClick.AddListener(() => OnClick());
        }

        void OnDisable()
        {
            button.onClick.RemoveAllListeners();
        }

        public void OnClick()
        {
            if (GUIManager.Instance.AllowToPressButton)
            {
                onClick?.Invoke();
                SoundManager.Instance.PlaySound(sound);
            }
        }
    }
}
