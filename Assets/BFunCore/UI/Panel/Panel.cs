#if BFUN_INSTALLED_TRUE
using LitMotion.Animation;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace BFunCoreKit
{
    [ExecuteAlways]
    public class Panel : MonoBehaviour
    {
        [HideLabel][SerializeField] PopupStruct effectPopup;
        public Transform Content => effectPopup.content;
        [ReadOnly] public bool isPlaying;
        [SerializeField] Canvas canvas;
        Dictionary<string, EffectGroup> showEffectDics = new Dictionary<string, EffectGroup>();
        Dictionary<string, EffectGroup> closeEffectDics = new Dictionary<string, EffectGroup>();

        UIPanel uiPanel;

        // Session ID: Để xử lý khi user bấm Show/Close liên tục
        private int _animSessionId = 0;

#if UNITY_EDITOR
        private EditorCoroutine currentCoroutine;
        [OnInspectorInit] void OnInspectorInit() { GetDatasEditor(); }
        void GetDatasEditor()
        {
            AddDics();
            if (effectPopup.showEffects != null) for (int i = 0; i < effectPopup.showEffects.Length; i++) { effectPopup.showEffects[i].isShowGroup = true; effectPopup.showEffects[i].panel = this; }
            if (effectPopup.closeEffects != null) for (int i = 0; i < effectPopup.closeEffects.Length; i++) { effectPopup.closeEffects[i].isShowGroup = false; effectPopup.closeEffects[i].panel = this; }
        }
        void OnValidate() { GetDatasEditor(); }
        private void OnEnable()
        {
#if UNITY_EDITOR
            PrefabStage.prefabSaving += OnPrefabSaving; PrefabStage.prefabStageClosing += OnPrefabStageClosing; AssemblyReloadEvents.beforeAssemblyReload += StopEditor;
#endif
        }
        private void OnDisable()
        {
#if UNITY_EDITOR
            PrefabStage.prefabSaving -= OnPrefabSaving; PrefabStage.prefabStageClosing -= OnPrefabStageClosing; AssemblyReloadEvents.beforeAssemblyReload -= StopEditor; if (!Application.isPlaying) StopEditor();
#endif
        }
#endif

        private void Awake()
        {
            uiPanel = GetComponentInParent<UIPanel>();
            AddDics();
            if (Application.isPlaying && effectPopup.showEffects != null)
            {
                RecordAllStates();
                foreach (var group in effectPopup.showEffects)
                {
                    if (group.effect != null) group.effect.Initialize();
                    if (group.effectGroup != null) foreach (var anim in group.effectGroup) if (anim.litMotionAnimation != null) anim.litMotionAnimation.Initialize();
                }
                UnityEngine.Canvas.ForceUpdateCanvases();
            }
        }

        void AddDics() { showEffectDics.Clear(); closeEffectDics.Clear(); if (effectPopup.showEffects != null) foreach (var g in effectPopup.showEffects) if (!showEffectDics.ContainsKey(g.showOption)) showEffectDics.Add(g.showOption, g); if (effectPopup.closeEffects != null) foreach (var g in effectPopup.closeEffects) if (!closeEffectDics.ContainsKey(g.showOption)) closeEffectDics.Add(g.showOption, g); }
        string CleanName(string n) => n.Replace("-", "").Replace("<", "").Replace(">", "").Replace(" ", "");

#if UNITY_EDITOR
        private void OnPrefabSaving(GameObject go) { if (!Application.isPlaying) StopEditor(); }
        private void OnPrefabStageClosing(PrefabStage stage) { if (!Application.isPlaying) StopEditor(); }
        public void SavePanelEffectGroupName()
        {
#if UNITY_EDITOR
            if (!Directory.Exists(GlobalConst.PanelOptionFolder)) { Directory.CreateDirectory(GlobalConst.PanelOptionFolder); AssetDatabase.Refresh(); }
            string fileNameShow = CleanName(name) + "PanelShowOption";
            string filePathAndNameShow = Path.Combine(GlobalConst.PanelOptionFolder, fileNameShow + ".cs");
            string newShowContent = GenerateOptionClass(fileNameShow, effectPopup.showEffects);
            string fileNameClose = CleanName(name) + "PanelCloseOption";
            string filePathAndNameClose = Path.Combine(GlobalConst.PanelOptionFolder, fileNameClose + ".cs");
            string newCloseContent = GenerateOptionClass(fileNameClose, effectPopup.closeEffects);
            bool savedShow = SaveFileIfChanged(filePathAndNameShow, newShowContent);
            bool savedClose = SaveFileIfChanged(filePathAndNameClose, newCloseContent);
            if (savedShow || savedClose) AssetDatabase.Refresh();
#endif
        }
        private string GenerateOptionClass(string className, IEnumerable<EffectGroup> groups)
        {
            var sb = new StringBuilder(); sb.AppendLine("namespace BFunCoreKit"); sb.AppendLine("{"); sb.AppendLine($"    public class {className}"); sb.AppendLine("    {");
            if (groups != null) foreach (EffectGroup effectGroup in groups) sb.AppendLine($"        public static readonly string {effectGroup.showOption} = \"{effectGroup.showOption}\";");
            sb.AppendLine("    }"); sb.AppendLine("}"); return sb.ToString().Trim();
        }
        private bool SaveFileIfChanged(string filePath, string newContent)
        {
            string Normalize(string s) => s.Replace("\r\n", "\n").Trim();
            if (File.Exists(filePath)) { string oldContent = File.ReadAllText(filePath); if (Normalize(oldContent) == Normalize(newContent)) return false; }
            File.WriteAllText(filePath, newContent); return true;
        }
        public void PlayEditor(string nameShow = "Default", bool isShowGroup = false) { StopEditor(); if (isShowGroup) currentCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(Show(nameShow)); else currentCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(Close(nameShow)); }
        IEnumerator WaitForAnimEditor(LitMotionAnimation anim) { if (anim == null) yield break; yield return null; while (anim.IsPlaying) { UnityEngine.Canvas.ForceUpdateCanvases(); EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); yield return null; } UnityEngine.Canvas.ForceUpdateCanvases(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
#endif

        void StopAllAnim(bool reset = true)
        {
            if (effectPopup.showEffects != null) foreach (var g in effectPopup.showEffects) { if (g.effect) g.effect.Stop(reset); if (g.effectGroup != null) foreach (var a in g.effectGroup) if (a.litMotionAnimation) a.litMotionAnimation.Stop(reset); }
            if (effectPopup.closeEffects != null) foreach (var g in effectPopup.closeEffects) { if (g.effect) g.effect.Stop(reset); if (g.effectGroup != null) foreach (var a in g.effectGroup) if (a.litMotionAnimation) a.litMotionAnimation.Stop(reset); }
        }

#if UNITY_EDITOR
        [OnInspectorDispose] void OnDispose() { if (!Application.isPlaying) StopEditor(); }
        public void StopEditor() { if (currentCoroutine != null) { EditorCoroutineUtility.StopCoroutine(currentCoroutine); currentCoroutine = null; } StopAllAnim(true); }
#endif

        void RecordAllStates()
        {
            if (effectPopup.showEffects != null) { foreach (var g in effectPopup.showEffects) { if (g.effect) g.effect.RecordAllStates(); if (g.effectGroup != null) foreach (var anim in g.effectGroup) if (anim.litMotionAnimation) anim.litMotionAnimation.RecordAllStates(); } }
            if (effectPopup.closeEffects != null) { foreach (var g in effectPopup.closeEffects) { if (g.effect) g.effect.RecordAllStates(); if (g.effectGroup != null) foreach (var anim in g.effectGroup) if (anim.litMotionAnimation) anim.litMotionAnimation.RecordAllStates(); } }
        }

        // --- HÀM SHOW (Đã sửa logic: Show xong thì Remove list) ---
        public IEnumerator Show(string nameShow = "Default")
        {
            if (!showEffectDics.ContainsKey(nameShow)) yield break;

            int myId = ++_animSessionId;

            if (isPlaying)
            {
                StopAllAnim(true);
                // Nếu đang chạy dở mà bị ngắt, phải remove khỏi list cũ để tránh kẹt
                GUIManager.Instance?.RemovePanelShowing(CleanName(name));
            }
            else
            {
                RecordAllStates();
            }

            effectPopup.initPos = effectPopup.content.localPosition;
            effectPopup.initParent = effectPopup.content.parent;
            if (canvas != null) canvas.sortingOrder = effectPopup.overrideOrder;

            LitMotionAnimation startEffect = showEffectDics[nameShow].effect;
            PanelAnimation[] startEffectGroup = showEffectDics[nameShow].effectGroup;

            if (startEffect != null) startEffect.Initialize();
            if (startEffectGroup != null) foreach (var item in startEffectGroup) if (item.litMotionAnimation != null) item.litMotionAnimation.Initialize();
            UnityEngine.Canvas.ForceUpdateCanvases();

            if (effectPopup.autoSpawn == SPAWNTYPE.Yes && Application.isPlaying)
            {
                effectPopup.content.name = name;
                effectPopup.content.SetParent(uiPanel.screen);
                yield return new WaitUntil(() => effectPopup.content.parent == uiPanel.screen);
            }
            effectPopup.content.localPosition = effectPopup.initPos;

            if (startEffect != null)
            {
                if(Application.isPlaying)
                    SoundManager.Instance.PlaySound(showEffectDics[nameShow].sound);
                startEffect.Restart();
            }

            // 1. ADD: Bắt đầu chạy thì Add vào list để chặn click
            GUIManager.Instance?.AddPanelShowing(CleanName(name));
            GUIManager.Instance?.AddLastPanel(CleanName(name));

            yield return null;
            if (myId != _animSessionId) yield break;

            if (Application.isPlaying) effectPopup.content.gameObject.SetActive(true);
            isPlaying = true;

            // ... (Logic Animation giữ nguyên) ...
            if (startEffectGroup != null && startEffectGroup.Length > 0)
            {
                for (int i = 0; i < startEffectGroup.Length; i++)
                {
                    if (myId != _animSessionId) yield break;
                    if (!startEffectGroup[i].litMotionAnimation) continue;
                    switch (startEffectGroup[i].panelDelay)
                    {
                        case PANELDELAY.Sequence:
                            var prevAnim = (i == 0) ? startEffect : startEffectGroup[i - 1].litMotionAnimation;
#if UNITY_EDITOR
                            if (!Application.isPlaying) yield return WaitForAnimEditor(prevAnim); else yield return new WaitUntil(() => !prevAnim.IsPlaying);
#else
                            yield return new WaitUntil(() => !prevAnim.IsPlaying);
#endif
                            if (startEffectGroup[i].delayTime > 0)
                            {
#if UNITY_EDITOR
                                if (!Application.isPlaying) yield return new EditorWaitForSeconds(startEffectGroup[i].delayTime); else yield return WaitCache.Get(startEffectGroup[i].delayTime);
#else
                                yield return WaitCache.Get(startEffectGroup[i].delayTime);
#endif
                            }
                            break;
                        case PANELDELAY.Parallel:
                            if (startEffectGroup[i].delayTime > 0)
                            {
#if UNITY_EDITOR
                                if (!Application.isPlaying) yield return new EditorWaitForSeconds(startEffectGroup[i].delayTime); else yield return WaitCache.Get(startEffectGroup[i].delayTime);
#else
                                yield return WaitCache.Get(startEffectGroup[i].delayTime);
#endif
                            }
                            break;
                    }
                    startEffectGroup[i].litMotionAnimation.Restart();
                }
                if (startEffectGroup[startEffectGroup.Length - 1].litMotionAnimation)
                {
                    var lastAnim = startEffectGroup[startEffectGroup.Length - 1].litMotionAnimation;
#if UNITY_EDITOR
                    if (!Application.isPlaying) yield return WaitForAnimEditor(lastAnim); else yield return new WaitUntil(() => !lastAnim.IsPlaying);
#else
                    yield return new WaitUntil(() => !lastAnim.IsPlaying);
#endif
                }
            }
            else
            {
                if (startEffect != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) yield return WaitForAnimEditor(startEffect); else yield return new WaitUntil(() => !startEffect.IsPlaying);
#else
                    yield return new WaitUntil(() => !startEffect.IsPlaying);
#endif
                }
            }

            if (myId == _animSessionId)
            {
                isPlaying = false;
                // 2. REMOVE: Chạy xong animation thì xóa khỏi list để mở khóa nút bấm
                GUIManager.Instance?.RemovePanelShowing(CleanName(name));
            }

            if (closeEffectDics != null) foreach (var kvp in closeEffectDics) { if (kvp.Value.effect != null) kvp.Value.effect.Initialize(); if (kvp.Value.effectGroup != null) foreach (var anim in kvp.Value.effectGroup) if (anim.litMotionAnimation != null) anim.litMotionAnimation.Initialize(); }

#if UNITY_EDITOR
            if (!Application.isPlaying) { StopAllAnim(true); currentCoroutine = null; }
#endif
        }

        // --- HÀM CLOSE ---
        public IEnumerator Close(string nameClose = "Default")
        {
            if (!closeEffectDics.ContainsKey(nameClose)) yield break;

            int myId = ++_animSessionId;

            if (isPlaying)
            {
                StopAllAnim(false);
                GUIManager.Instance?.RemovePanelShowing(CleanName(name));
            }
            else
            {
                RecordAllStates();
            }

            // Bắt đầu đóng cũng phải chặn input
            GUIManager.Instance?.AddPanelShowing(CleanName(name));

            LitMotionAnimation closeEffect = closeEffectDics[nameClose].effect;
            PanelAnimation[] closeEffectGroup = closeEffectDics[nameClose].effectGroup;

            if (closeEffect != null) closeEffect.Initialize();
            if (closeEffectGroup != null) foreach (var item in closeEffectGroup) if (item.litMotionAnimation != null) item.litMotionAnimation.Initialize();
            UnityEngine.Canvas.ForceUpdateCanvases();

            if (closeEffect != null)
            {
                if (Application.isPlaying)
                    SoundManager.Instance.PlaySound(closeEffectDics[nameClose].sound);
                closeEffect.Restart();
            }
            isPlaying = true;

            // ... (Logic Animation giữ nguyên) ...
            if (closeEffectGroup != null && closeEffectGroup.Length > 0)
            {
                for (int i = 0; i < closeEffectGroup.Length; i++)
                {
                    if (myId != _animSessionId) yield break;
                    if (!closeEffectGroup[i].litMotionAnimation) continue;
                    switch (closeEffectGroup[i].panelDelay)
                    {
                        case PANELDELAY.Sequence:
                            var prevAnim = (i == 0) ? closeEffect : closeEffectGroup[i - 1].litMotionAnimation;
#if UNITY_EDITOR
                            if (!Application.isPlaying) yield return WaitForAnimEditor(prevAnim); else yield return new WaitUntil(() => !prevAnim.IsPlaying);
#else
                            yield return new WaitUntil(() => !prevAnim.IsPlaying);
#endif
                            if (closeEffectGroup[i].delayTime > 0)
                            {
#if UNITY_EDITOR
                                if (!Application.isPlaying) yield return new EditorWaitForSeconds(closeEffectGroup[i].delayTime); else yield return WaitCache.Get(closeEffectGroup[i].delayTime);
#else
                                yield return WaitCache.Get(closeEffectGroup[i].delayTime);
#endif
                            }
                            break;
                        case PANELDELAY.Parallel:
                            if (closeEffectGroup[i].delayTime > 0)
                            {
#if UNITY_EDITOR
                                if (!Application.isPlaying) yield return new EditorWaitForSeconds(closeEffectGroup[i].delayTime); else yield return WaitCache.Get(closeEffectGroup[i].delayTime);
#else
                                yield return WaitCache.Get(closeEffectGroup[i].delayTime);
#endif
                            }
                            break;
                    }
                    closeEffectGroup[i].litMotionAnimation.Restart();
                }
                if (closeEffectGroup[closeEffectGroup.Length - 1].litMotionAnimation)
                {
                    var lastAnim = closeEffectGroup[closeEffectGroup.Length - 1].litMotionAnimation;
#if UNITY_EDITOR
                    if (!Application.isPlaying) yield return WaitForAnimEditor(lastAnim); else yield return new WaitUntil(() => !lastAnim.IsPlaying);
#else
                    yield return new WaitUntil(() => !lastAnim.IsPlaying);
#endif
                }
            }
            else
            {
                if (closeEffect != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) yield return WaitForAnimEditor(closeEffect); else yield return new WaitUntil(() => !closeEffect.IsPlaying);
#else
                    yield return new WaitUntil(() => !closeEffect.IsPlaying);
#endif
                }
            }

            if (myId == _animSessionId)
            {
                isPlaying = false;

                // Đóng xong -> Xóa khỏi list -> Mở khóa nút
                GUIManager.Instance?.RemovePanelShowing(CleanName(name));

                if (Application.isPlaying)
                {
                    if (effectPopup.autoSpawn == SPAWNTYPE.Yes)
                    {
                        effectPopup.content.name = "Canvas";
                        effectPopup.content.SetParent(effectPopup.initParent);
                        effectPopup.content.localPosition = effectPopup.initPos;
                    }
                    effectPopup.content.gameObject.SetActive(false);
                }
                StopAllAnim(true);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) { currentCoroutine = null; }
#endif
        }
    }
}
#endif