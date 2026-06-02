            #if BFUN_INSTALLED_TRUE
using UnityEngine;
using TMPro;
using BFunCoreKit;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace BFunCoreKit
{
    [ExecuteAlways]
    public class GUIColorBinder : MonoBehaviour
    {
        [SerializeField] GUIColorStyleSheet styleSheet;
#if BFUN_TEXT_TRUE
        [SerializeField] GUIColorStyleType colorType;
        [ShowIf("colorType", GUIColorStyleType.Default)][SerializeField] Color initColor;

        [OnValueChanged("OnColorChanged")]
        private Image img;
        private TextMeshProUGUI tmp;

        [OnInspectorInit]

        void Awake()
        {
            if (!styleSheet) styleSheet = Resources.Load<GUIColorStyleSheet>("Color Setting");
            Apply();
            GetComponents();
        }

        void OnColorChanged()
        {
            if (!img)
            {
                if (img && initColor != img.color)
                    initColor = img.color;
            }

            if (!tmp)
            {
                if (tmp && initColor != tmp.color)
                    initColor = tmp.color;
            }
        }

        private void GetComponents()
        {
            if (!img)
            {
                img = GetComponent<Image>();
                if (img && initColor != img.color)
                    initColor = img.color;
            }

            if (!tmp)
            {
                tmp = GetComponent<TextMeshProUGUI>();
                if (tmp && initColor != tmp.color)
                    initColor = tmp.color;
            }
        }
        void OnValidate()
        {
            Apply();
        }
        public void Apply()
        {
            if (styleSheet != null)
            {
                if (img)
                    styleSheet.ApplyTo(img, colorType, initColor);
                if (tmp)
                    styleSheet.ApplyTo(tmp, colorType, initColor);
            }
        }
        #endif
    }
}
#endif