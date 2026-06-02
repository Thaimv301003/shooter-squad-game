#if BFUN_INSTALLED_TRUE
using BFunCoreKit;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace BFunCoreKit
{
    public abstract class CanvasBase<T> : Singleton<T> where T : CanvasBase<T>
    {
        [Header("Canvas Base")]
        [HideInInspector] public Canvas canvas; // Canvas chính
        public UIPanel uiPanel;
        CanvasGroup canvasGroup;

        public override void Awake()
        {
            base.Awake();
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            canvasGroup = GetComponent<CanvasGroup>();
            uiPanel = GetComponentInChildren<UIPanel>();
        }

        public bool IsCanvasShow
        {
            get { return canvasGroup.alpha == 1; }
        }

        public void ShowCanvas(bool show, bool fade = true)
        {
            LMotion.Create(show ? 0 : 1,show ? 1 : 0, fade ? 0.5f : 0).Bind(x => canvasGroup.alpha = x);
        }
    }
}
#endif