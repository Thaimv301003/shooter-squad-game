using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace TheLegends.Base.UI
{
    public class UILoadingController : MonoBehaviour
    {
        [SerializeField]
        private GameObject container;

        [SerializeField]
        private Image backgroundImg;

        [SerializeField]
        private Sprite[] backgroundSprites;

        [SerializeField]
        private Image fillImg;

        [SerializeField]
        private string statusString = "Loading... please wait!";
        [SerializeField]
        private Text statusTxt;

        private int spriteIndex = 0;
        private float eslapseTime = 0;
        private int statusSuffixIndex = 0;
        private string statusSuffixString = ".";

        private bool isStart = false;

        private MotionHandle _handle;

        protected static UILoadingController instance = null;

        private void Awake()
        {
            instance = this;
            instance.container.SetActive(false);

        }

        public static void Show(float time, Action onComplete = null)
        {
            instance.spriteIndex = instance.spriteIndex % instance.backgroundSprites.Length;
            instance.backgroundImg.sprite = instance.backgroundSprites[instance.spriteIndex];
            instance.spriteIndex++;

            instance.fillImg.fillAmount = 0;

            instance.container.SetActive(true);

            FillLoadingProgressBar(1, time, () =>
            {
                onComplete?.Invoke();
                Hide();
            });
        }

        public static void SetProgress(float percent, Action onComplete = null)
        {
            instance.container.SetActive(true);
            FillLoadingProgressBar(percent, 1, onComplete);
        }

        private static void FillLoadingProgressBar(float value, float time, Action onComplete = null)
        {

            if (instance._handle.IsActive())
            {
                instance._handle.Cancel();
            }

            instance.isStart = true;

            instance._handle = LMotion.Create(instance.fillImg.fillAmount, value, time)
                .WithEase(Ease.Linear)
                .WithScheduler(MotionScheduler.UpdateRealtime)
                .WithOnComplete(() =>
                {
                    onComplete?.Invoke();
                })
                .Bind(instance.fillImg, (i, image) =>
                {
                    image.fillAmount = i;

                });
        }

        private void Update()
        {
            if (instance.isStart)
            {
                instance.eslapseTime += Time.deltaTime;
                instance.statusSuffixIndex = Mathf.Clamp((int)instance.eslapseTime % 3, 0, 2);

                switch (instance.statusSuffixIndex)
                {
                    case 0:
                        instance.statusSuffixString = ".";
                        break;
                    case 1:
                        instance.statusSuffixString = "..";
                        break;
                    case 2:
                        instance.statusSuffixString = "...";
                        break;
                }

                instance.statusTxt.text = instance.statusString + " " + instance.statusSuffixString;
            }
        }

        public static void Hide()
        {
            if (instance._handle.IsActive())
            {
                instance._handle.Cancel();
            }
            instance.container.SetActive(false);
            instance.fillImg.fillAmount = 0;
            instance.isStart = false;
            instance.eslapseTime = 0;
        }
    }
}
