using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TheLegends.Base.Ads
{
    public class BrandItemController : MonoBehaviour
    {
        public Image itemBG;
        public Sprite unselectSprite;
        public Sprite selectedSprite;
        public GameObject tickGO;
        public Button btn;
        [FormerlySerializedAs("adsSplashController")]
        public BrandScreenController brandScreenController;

        private void OnEnable()
        {
            btn.onClick.AddListener(OnItemSelected);
        }

        private void OnDisable()
        {
            btn.onClick.RemoveListener(OnItemSelected);
        }


        public void SetSelected(bool isSelected)
        {
            itemBG.sprite = isSelected ? selectedSprite : unselectSprite;
            tickGO.SetActive(isSelected);
        }

        public void OnItemSelected()
        {
            brandScreenController.OnItemSelected(this);
        }
    }
}

