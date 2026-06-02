
#if BFUN_INSTALLED_TRUE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LitMotion.Animation;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace BFunCoreKit
{
    public enum SPAWNTYPE { Yes, No}
    public enum PANELDELAY { Sequence, Parallel}
    public enum USEEVENT { No, Yes}

    [System.Serializable]
    public struct PanelAnimation
    {
        public SoundName sound;
        public PANELDELAY panelDelay;
        public float delayTime;
        public LitMotionAnimation litMotionAnimation;
    }

    [System.Serializable]
    public struct GUI
    {
        [HideInInspector] public string GUIName;  
        [LabelText("@GUIName")] 
        public Panel panel;
    }

    [System.Serializable]
    public struct EffectGroup
    {
        public SoundName sound;
        [HideInInspector] public bool isShowGroup;
        public string showOption;
        public LitMotionAnimation effect;
        public PanelAnimation[] effectGroup;
        [EnumToggleButtons]
        public USEEVENT useEvent;

        [ShowIf("@this.useEvent == USEEVENT.Yes")]
        public UnityEvent onInitEffect, onDoneEffect;

        [NonSerialized, HideInInspector]
        public Panel panel;
#if UNITY_EDITOR

        [HorizontalGroup("Acces")]
        [Button(Icon = SdfIconType.PlayFill, ButtonHeight = 30)]
        void Play()
        {
            panel.StopEditor();
            panel.PlayEditor(showOption, isShowGroup);
        }

        [HorizontalGroup("Acces")]
        [Button(Icon = SdfIconType.StopFill, ButtonHeight = 30)]
        void Stop()
        {
            panel.StopEditor();
        }
        #endif
    }

    [System.Serializable]
    public struct PopupStruct
    {
        [TitleGroup("SHOW")]
        public RectTransform content;
        [TitleGroup("SHOW")]
        [EnumToggleButtons]
        public SPAWNTYPE autoSpawn;

        [TitleGroup("SHOW")]
        [HideInInspector] public Transform initParent;
        [HideInInspector] public Vector2 initPos;
        [TitleGroup("SHOW")] public int overrideOrder;

        //[LabelText("Preset")]
        //[TitleGroup("SHOW")]
        //[AssetSelector()]
        // [SerializeField] LitMotionAnimation startPresetEffect;

        [TitleGroup("SHOW")]
        public EffectGroup[] showEffects;

        [PropertySpace(20)]

        //[TitleGroup("CLOSE")]
        //[LabelText("Preset")]
        //[AssetSelector()]
        //[SerializeField] LitMotionAnimation endPresetEffect;

        [TitleGroup("SHOW")]
        public EffectGroup[] closeEffects;
  
    }
}
#endif
