using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace TheLegends.Base.UI
{
    public class UIToatsController : MonoBehaviour
    {
        protected static UIToatsController instance = null;

        [SerializeField]
        private VerticalLayoutGroup _uiContentVerticalLayoutGroup;

        [SerializeField]
        private int _bottomPadding = 20;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private Text _toastText;

        private int maxTextLength = 20;

        private MotionHandle _handle;


        private void Awake()
        {
            instance = this;
            Dismiss();

        }

        private void Start()
        {
            SetBottomPadding();
        }

        private void SetBottomPadding()
        {
            var tempPadding = _uiContentVerticalLayoutGroup.padding;
            tempPadding.bottom = _bottomPadding;
            _uiContentVerticalLayoutGroup.padding = tempPadding;
        }

        public static void Show(string text, float duration, ToastPosition position)
        {
            Dismiss();

            instance._toastText.text = (text.Length > instance.maxTextLength) ? text.Substring (0, instance.maxTextLength) + "..." : text ;

            instance._uiContentVerticalLayoutGroup.CalculateLayoutInputHorizontal () ;
            instance._uiContentVerticalLayoutGroup.CalculateLayoutInputVertical () ;
            instance._uiContentVerticalLayoutGroup.SetLayoutHorizontal () ;
            instance._uiContentVerticalLayoutGroup.SetLayoutVertical () ;
            instance._uiContentVerticalLayoutGroup.childAlignment = (TextAnchor)((int)position) ;

            FadeGroup(0, 1, duration, 0f, () =>
            {
                FadeGroup(1, 0, duration, 1f,() =>
                {
                    Dismiss();
                });
            });

        }

        public static void Dismiss()
        {
            if (instance._handle.IsActive())
            {
                instance._handle.Cancel();
            }

            instance._canvasGroup.alpha = 0f;
        }

        private static void FadeGroup(float start, float end, float duration, float delay = 0f, Action onComplete = null)
        {
            if (instance._handle.IsActive())
            {
                instance._handle.Cancel();
            }

            instance._handle = LMotion.Create(start, end, duration)
                .WithEase(Ease.Linear)
                .WithDelay(delay)
                .WithOnComplete(() =>
                {
                    onComplete?.Invoke();
                })
                .Bind((value) =>
                {
                    instance._canvasGroup.alpha = value;
                });
        }



    }

    public enum ToastPosition {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}
