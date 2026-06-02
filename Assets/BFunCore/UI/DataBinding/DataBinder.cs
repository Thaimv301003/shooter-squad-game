#if BFUN_INSTALLED_TRUE
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Reflection;
using Sirenix.OdinInspector;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#endif

namespace BFunCoreKit
{
    public class DataBinder : MonoBehaviour
    {
        // =================================================================================
        // SECTION: FIELDS (Runtime & Editor)
        // =================================================================================

        private enum DetectedBindingMode { Undetermined, CallMethod, UpdateValue }

        public enum UpdateFrequency { EveryFrame, OnEvent }

#if UNITY_EDITOR
        [ShowIf("IsBindingModeDetermined")]
        [ValueDropdown("GetAllBindableMembers", IsUniqueList = true, DropdownTitle = "Chọn một Binding Path")]
        [LabelText("Binding Path")]
#endif
        [SerializeField]
        private string _selectedMethodPath;

#if UNITY_EDITOR
        [ShowIf("IsUpdateValueMode")]
        [InfoBox("Mode : Text / Value Update", InfoMessageType.None)]
#endif
        [SerializeField] private UpdateFrequency _updateFrequency = UpdateFrequency.EveryFrame;

#if UNITY_EDITOR
        [ShowIf("IsUpdateValueMode")]
        [LabelText("Init Update (On Start)")]
#endif
        [SerializeField] private bool _initUpdate = true;

#if UNITY_EDITOR
        [ShowIf("IsUpdateValueMode")]
        [ShowIf("@this._updateFrequency == UpdateFrequency.OnEvent")]
        [InfoBox("Để chế độ OnEvent hoạt động, Property cần bind (ví dụ 'Health') phải có một event tương ứng tên là 'OnHealthChanged' trong cùng class.", InfoMessageType.Info)]
#endif

        [SerializeField, HideLabel, DisplayAsString(false)] string hide = "";

#if UNITY_EDITOR
        [ShowIf("IsCallMethodMode")]
        [InfoBox("Param : Boolean", InfoMessageType.None)]
        [ShowIf("IsMethodWithParameterSelected")]
        [Indent]
        [ShowIf("ShowBoolParam")]
#endif
        [SerializeField][LabelText("Giá trị (bool)")] private bool _boolParam;

#if UNITY_EDITOR
        [ShowIf("IsMethodWithParameterSelected")]
        [InfoBox("Param : Int", InfoMessageType.None)]
        [Indent]
        [ShowIf("ShowIntParam")]
#endif
        [SerializeField][LabelText("Giá trị (int)")] private int _intParam;

#if UNITY_EDITOR
        [ShowIf("IsMethodWithParameterSelected")]
        [InfoBox("Param : Float", InfoMessageType.None)]
        [Indent]
        [ShowIf("ShowFloatParam")]
#endif
        [SerializeField][LabelText("Giá trị (float)")] private float _floatParam;

#if UNITY_EDITOR
        [ShowIf("IsMethodWithParameterSelected")]
        [InfoBox("Param : String", InfoMessageType.None)]
        [Indent]
        [ShowIf("ShowStringParam")]
#endif
        [SerializeField][LabelText("Giá trị (string)")] private string _stringParam;

#if UNITY_EDITOR
        [InfoBox("Mode : Call Method", InfoMessageType.None)]
        [ShowIf("IsBindingModeDetermined")]
#endif
        [SerializeField]
        [Tooltip("Các hàm trong danh sách này sẽ được gọi mỗi khi giá trị được cập nhật hoặc hàm được thực thi thành công.")]
        private UnityEvent OnAction;

        private DetectedBindingMode _detectedMode = DetectedBindingMode.Undetermined;
        private Component _uiComponent;
        private MonoBehaviour _cachedTargetInstance;
        private MethodInfo _cachedMethodInfo;
        private PropertyInfo _cachedPropertyInfo;
        private FieldInfo _cachedFieldInfo;
        private Type _expectedParameterType;
        private bool _isInitialized = false;
        private EventInfo _cachedEventInfo;
        private Action _updateValueAction;

        private GUIButton _guiButton;
        private Type _cachedTargetType;

        private UnityAction<bool> _toggleAction;
        private UnityAction<string> _inputFieldAction;

        private object _lastKnownValue = null;

        // =================================================================================
        // SECTION: RUNTIME LOGIC
        // =================================================================================

        void Start()
        {
        }

        void OnEnable()
        {
            if (!_isInitialized)
            {
                InitializeBinding();
            }

            if (_isInitialized)
            {
                AttachUIComponentHandlers();
                SubscribeToEvent();
            }

            if (_isInitialized && _detectedMode == DetectedBindingMode.UpdateValue && _initUpdate)
            {
                EnsureTargetInstance();
                UpdateBoundValue();
            }
        }

        void OnDisable()
        {
            if (_isInitialized)
            {
                RemoveUIComponentHandlers();
                UnsubscribeFromEvent();
            }
        }

        void Update()
        {
            if (_detectedMode != DetectedBindingMode.UpdateValue || _updateFrequency != UpdateFrequency.EveryFrame || !_isInitialized) return;
            UpdateBoundValue();
        }

        private void DetectBindingModeAndComponent()
        {
            if (_detectedMode != DetectedBindingMode.Undetermined) return;
            _uiComponent = GetComponent<TextMeshProUGUI>() ?? (Component)GetComponent<Text>() ?? (Component)GetComponent<Slider>();
            if (_uiComponent != null) _detectedMode = DetectedBindingMode.UpdateValue;

            _guiButton = GetComponent<GUIButton>();

            var interactiveComponent = GetComponent<Button>() ?? GetComponent<Toggle>() ?? (Component)GetComponent<TMP_InputField>() ?? (Component)GetComponent<InputField>();
            if (_uiComponent == null && interactiveComponent != null)
            {
                _uiComponent = interactiveComponent;
                _detectedMode = DetectedBindingMode.CallMethod;
            }
        }

        private bool EnsureTargetInstance()
        {
            if (_cachedTargetInstance != null) return true;
            if (_cachedTargetType == null) return false;

            _cachedTargetInstance = BindingManager.Instance?.GetRef(_cachedTargetType);

            if (_cachedTargetInstance == null)
            {
                var instanceProp = _cachedTargetType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp != null && typeof(MonoBehaviour).IsAssignableFrom(instanceProp.PropertyType))
                {
                    _cachedTargetInstance = instanceProp.GetValue(null) as MonoBehaviour;
                }
            }

            if (_cachedTargetInstance == null)
            {
                _cachedTargetInstance = FindObjectOfType(_cachedTargetType) as MonoBehaviour;
            }

            return _cachedTargetInstance != null;
        }

        private void InitializeBinding()
        {
            if (_isInitialized) return;

            DetectBindingModeAndComponent();
            if (string.IsNullOrEmpty(_selectedMethodPath)) return;

            int lastDotIndex = _selectedMethodPath.LastIndexOf('.');
            if (lastDotIndex == -1) return;
            string assemblyQualifiedTypeName = _selectedMethodPath.Substring(0, lastDotIndex);
            string memberName = _selectedMethodPath.Substring(lastDotIndex + 1);

            _cachedTargetType = Type.GetType(assemblyQualifiedTypeName);
            if (_cachedTargetType == null) return;

            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            _cachedMethodInfo = _cachedTargetType.GetMethod(memberName, bindingFlags);
            _cachedPropertyInfo = _cachedTargetType.GetProperty(memberName, bindingFlags);
            _cachedFieldInfo = _cachedTargetType.GetField(memberName, bindingFlags);

            if (_cachedMethodInfo != null)
            {
                var parameters = _cachedMethodInfo.GetParameters();
                if (parameters.Length == 1) _expectedParameterType = parameters[0].ParameterType;
            }

            _toggleAction = (_) => InvokeBoundMethod();
            _inputFieldAction = (_) => InvokeBoundMethod();

            if (_updateFrequency == UpdateFrequency.OnEvent && _detectedMode == DetectedBindingMode.UpdateValue)
            {
                string eventName = "On" + memberName + "Changed";
                _cachedEventInfo = _cachedTargetType.GetEvent(eventName, bindingFlags);

                if (_cachedEventInfo != null)
                {
                    _updateValueAction = UpdateBoundValue;
                }
                else
                {
                    Debug.LogWarning($"[DataBinder] Chế độ OnEvent: Không tìm thấy event '{eventName}' trong class '{_cachedTargetType.Name}'.", this);
                }
            }

            _isInitialized = true;
            EnsureTargetInstance();
        }

        private void SubscribeToEvent()
        {
            if (_cachedEventInfo != null && _updateValueAction != null && EnsureTargetInstance())
            {
                try { _cachedEventInfo.RemoveEventHandler(_cachedTargetInstance, _updateValueAction); } catch { }
                _cachedEventInfo.AddEventHandler(_cachedTargetInstance, _updateValueAction);
            }
        }

        private void UnsubscribeFromEvent()
        {
            if (_cachedEventInfo != null && _updateValueAction != null && _cachedTargetInstance != null)
            {
                _cachedEventInfo.RemoveEventHandler(_cachedTargetInstance, _updateValueAction);
            }
        }

        private void AttachUIComponentHandlers()
        {
            if (_detectedMode == DetectedBindingMode.CallMethod || _uiComponent is Slider)
            {
                if (_guiButton != null)
                {
                    _guiButton.onClick.AddListener(InvokeBoundMethod);
                }
                else if (_uiComponent is Button btn)
                {
                    btn.onClick.AddListener(InvokeBoundMethod);
                }
                else if (_uiComponent is Toggle tgl) { tgl.onValueChanged.AddListener(_toggleAction); }
                else if (_uiComponent is Slider sld) { sld.onValueChanged.AddListener(InvokeBoundMethodWithSliderValue); }
                else if (_uiComponent is TMP_InputField tmpInput) { tmpInput.onEndEdit.AddListener(_inputFieldAction); }
                else if (_uiComponent is InputField input) { input.onEndEdit.AddListener(_inputFieldAction); }
            }
        }

        private void RemoveUIComponentHandlers()
        {
            if (_uiComponent == null) return;

            if (_detectedMode == DetectedBindingMode.CallMethod || _uiComponent is Slider)
            {
                if (_guiButton != null)
                {
                    _guiButton.onClick.RemoveListener(InvokeBoundMethod);
                }
                else if (_uiComponent is Button btn)
                {
                    btn.onClick.RemoveListener(InvokeBoundMethod);
                }
                else if (_uiComponent is Toggle tgl) { tgl.onValueChanged.RemoveListener(_toggleAction); }
                else if (_uiComponent is Slider sld) { sld.onValueChanged.RemoveListener(InvokeBoundMethodWithSliderValue); }
                else if (_uiComponent is TMP_InputField tmpInput) { tmpInput.onEndEdit.RemoveListener(_inputFieldAction); }
                else if (_uiComponent is InputField input) { input.onEndEdit.RemoveListener(_inputFieldAction); }
            }
        }

        // [SỬA ĐỔI]: Hàm này đã được viết lại để hỗ trợ Set Method, Property và Field
        public void InvokeBoundMethodWithSliderValue(float value)
        {
            // Bỏ kiểm tra _cachedMethodInfo == null ở đây vì có thể bind vào Property
            if (!_isInitialized) return;
            if (!EnsureTargetInstance()) return;

            try
            {
                // 1. Nếu là Method (Hàm)
                if (_cachedMethodInfo != null)
                {
                    var parameters = _cachedMethodInfo.GetParameters();
                    if (parameters.Length == 1)
                    {
                        // Hỗ trợ hàm nhận float
                        if (_expectedParameterType == typeof(float))
                        {
                            _cachedMethodInfo.Invoke(_cachedTargetInstance, new object[] { value });
                        }
                        // Hỗ trợ hàm nhận int (ép kiểu)
                        else if (_expectedParameterType == typeof(int))
                        {
                            _cachedMethodInfo.Invoke(_cachedTargetInstance, new object[] { (int)value });
                        }
                        else
                        {
                            // Nếu kiểu không khớp, thử gọi hàm không tham số (fallback)
                            InvokeBoundMethod();
                        }
                    }
                    else
                    {
                        InvokeBoundMethod();
                    }
                    OnAction?.Invoke();
                }
                // 2. Nếu là Property (Thuộc tính)
                else if (_cachedPropertyInfo != null && _cachedPropertyInfo.CanWrite)
                {
                    // Chuyển đổi value sang kiểu của Property (int/float)
                    object val = Convert.ChangeType(value, _cachedPropertyInfo.PropertyType);
                    _cachedPropertyInfo.SetValue(_cachedTargetInstance, val);
                    OnAction?.Invoke();
                }
                // 3. Nếu là Field (Biến)
                else if (_cachedFieldInfo != null)
                {
                    object val = Convert.ChangeType(value, _cachedFieldInfo.FieldType);
                    _cachedFieldInfo.SetValue(_cachedTargetInstance, val);
                    OnAction?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataBinder] Lỗi khi gọi binding Slider '{_selectedMethodPath}': {ex.InnerException?.Message ?? ex.Message}", this);
            }
        }

        public void InvokeBoundMethod()
        {
            if (!_isInitialized || _cachedMethodInfo == null) return;

            if (!EnsureTargetInstance())
            {
                Debug.LogError($"[DataBinder] Không thể gọi hàm '{_cachedMethodInfo.Name}' vì chưa tìm thấy Instance của '{_cachedTargetType?.Name}' (Check Singleton/Awake).", this);
                return;
            }

            try
            {
                var parameters = _cachedMethodInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    _cachedMethodInfo.Invoke(_cachedTargetInstance, null);
                }
                else if (parameters.Length == 1)
                {
                    object valueToPass = null;
                    if (_expectedParameterType == typeof(bool)) valueToPass = _boolParam;
                    else if (_expectedParameterType == typeof(int)) valueToPass = _intParam;
                    else if (_expectedParameterType == typeof(float)) valueToPass = _floatParam;
                    else if (_expectedParameterType == typeof(string)) valueToPass = _stringParam;
                    _cachedMethodInfo.Invoke(_cachedTargetInstance, new object[] { valueToPass });
                }

                OnAction?.Invoke();
            }
            catch (Exception ex) { Debug.LogError($"[DataBinder] Lỗi khi gọi hàm '{_selectedMethodPath}': {ex.InnerException?.Message ?? ex.Message}", this); }
        }

        private void UpdateBoundValue()
        {
            if (!_isInitialized || !EnsureTargetInstance()) return;

            object currentValue = null;
            try
            {
                if (_cachedMethodInfo != null && _cachedMethodInfo.GetParameters().Length == 0 && _cachedMethodInfo.ReturnType != typeof(void)) currentValue = _cachedMethodInfo.Invoke(_cachedTargetInstance, null);
                else if (_cachedPropertyInfo != null) currentValue = _cachedPropertyInfo.GetValue(_cachedTargetInstance);
                else if (_cachedFieldInfo != null) currentValue = _cachedFieldInfo.GetValue(_cachedTargetInstance);
            }
            catch (Exception ex) { return; }

            if (currentValue == null) return;

            if (Equals(_lastKnownValue, currentValue))
            {
                return;
            }

            if (_uiComponent is TextMeshProUGUI tmpText) tmpText.text = currentValue.ToString();
            else if (_uiComponent is Text legacyText) legacyText.text = currentValue.ToString();
            else if (_uiComponent is Slider sld)
            {
                try { sld.value = Convert.ToSingle(currentValue); }
                catch (Exception ex) { Debug.LogError($"[DataBinder] Lỗi khi chuyển đổi giá trị '{currentValue}' thành float cho Slider: {ex.Message}", this); }
            }

            _lastKnownValue = currentValue;

            OnAction?.Invoke();
        }

        // =================================================================================
        // SECTION: EDITOR-ONLY LOGIC
        // =================================================================================
#if UNITY_EDITOR
        private Type _editorCachedParameterType = null;
        private void OnValidate()
        {
            var previousMode = _detectedMode;
            _detectedMode = DetectedBindingMode.Undetermined;
            DetectBindingModeAndComponent();
            if (previousMode != DetectedBindingMode.Undetermined && previousMode != _detectedMode) _selectedMethodPath = "";
            UpdateEditorParameterInfo();
        }
        private void UpdateEditorParameterInfo()
        {
            _editorCachedParameterType = null;
            if (string.IsNullOrEmpty(_selectedMethodPath)) return;
            int lastDotIndex = _selectedMethodPath.LastIndexOf('.');
            if (lastDotIndex == -1) return;
            string assemblyQualifiedTypeName = _selectedMethodPath.Substring(0, lastDotIndex);
            string memberName = _selectedMethodPath.Substring(lastDotIndex + 1);
            Type targetType = Type.GetType(assemblyQualifiedTypeName);
            if (targetType == null) return;
            MethodInfo methodInfo = targetType.GetMethod(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo != null && methodInfo.GetParameters().Length == 1) _editorCachedParameterType = methodInfo.GetParameters()[0].ParameterType;
        }
        private bool IsUpdateValueMode() => _detectedMode == DetectedBindingMode.UpdateValue;
        private bool IsCallMethodMode() => _detectedMode == DetectedBindingMode.CallMethod;
        private bool IsBindingModeDetermined() => _detectedMode != DetectedBindingMode.Undetermined;
        private bool IsMethodWithParameterSelected()
        {
            if (UnityEditor.Selection.activeGameObject == this.gameObject) { UpdateEditorParameterInfo(); }
            return (_detectedMode == DetectedBindingMode.CallMethod || _uiComponent is Slider) && !string.IsNullOrEmpty(_selectedMethodPath) && _editorCachedParameterType != null;
        }
        private bool ShowBoolParam() => IsMethodWithParameterSelected() && _editorCachedParameterType == typeof(bool);
        private bool ShowIntParam() => IsMethodWithParameterSelected() && _editorCachedParameterType == typeof(int);
        private bool ShowFloatParam() => IsMethodWithParameterSelected() && _editorCachedParameterType == typeof(float) && !(_uiComponent is Slider);
        private bool ShowStringParam() => IsMethodWithParameterSelected() && _editorCachedParameterType == typeof(string);
        private IEnumerable<ValueDropdownItem<string>> GetAllBindableMembers()
        {
            var list = new List<ValueDropdownItem<string>>();
            list.Add(new ValueDropdownItem<string>("(Chưa chọn)", ""));
            _detectedMode = DetectedBindingMode.Undetermined;
            DetectBindingModeAndComponent();
            if (_detectedMode == DetectedBindingMode.Undetermined)
            {
                list.Add(new ValueDropdownItem<string>("Lỗi: Không tìm thấy component UI hợp lệ (Text, Button,...)", ""));
                return list;
            }
            var bindingData = AssetDatabase.LoadAssetAtPath<BindingData>(GlobalConst.SettingFolder + "/Binding Setting.asset");
            if (bindingData == null || bindingData.GetBindableTypes() == null) return list;
            var monoScripts = bindingData.GetBindableTypes();
            if (monoScripts == null || monoScripts?.ToList().Count == 0) return list;
            foreach (var scriptType in monoScripts.Where(t => t != null && t.IsSubclassOf(typeof(MonoBehaviour))))
            {
                string groupName = scriptType.Name;
                var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                bool isSlider = _uiComponent is Slider;
                if (_detectedMode == DetectedBindingMode.CallMethod || isSlider)
                {
                    foreach (var method in scriptType.GetMethods(bindingFlags).Where(m => !m.IsSpecialName && m.ReturnType == typeof(void)))
                    {
                        var parameters = method.GetParameters();
                        string path = $"{scriptType.AssemblyQualifiedName}.{method.Name}";
                        if (parameters.Length == 0) list.Add(new ValueDropdownItem<string>($"{groupName}/CALL {method.Name}()", path));
                        else if (parameters.Length == 1) list.Add(new ValueDropdownItem<string>($"{groupName}/CALL {method.Name}({parameters[0].ParameterType.Name})", path));
                    }
                }
                if (_detectedMode == DetectedBindingMode.UpdateValue)
                {
                    foreach (var method in scriptType.GetMethods(bindingFlags).Where(m => !m.IsSpecialName && m.ReturnType != typeof(void) && m.GetParameters().Length == 0)) list.Add(new ValueDropdownItem<string>($"{groupName}/GET {method.Name}() : {method.ReturnType.Name}", $"{scriptType.AssemblyQualifiedName}.{method.Name}"));
                    foreach (var prop in scriptType.GetProperties(bindingFlags).Where(p => p.CanRead)) list.Add(new ValueDropdownItem<string>($"{groupName}/PROP {prop.Name} : {prop.PropertyType.Name}", $"{scriptType.AssemblyQualifiedName}.{prop.Name}"));
                    foreach (var f in scriptType.GetFields(bindingFlags)) list.Add(new ValueDropdownItem<string>($"{groupName}/FIELD {f.Name} : {f.FieldType.Name}", $"{scriptType.AssemblyQualifiedName}.{f.Name}"));
                }
            }
            return list.Distinct();
        }
#endif
    }
}
#endif