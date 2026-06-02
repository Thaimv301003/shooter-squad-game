// FILE: CustomCommandEntry.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BFunCoreKit
{
    [Serializable]
    public class CustomCommandEntry
    {
        // =================================================================================
        // SECTION: DATA FIELDS
        // =================================================================================

        [HorizontalGroup("CommandInfo")]
        [VerticalGroup("CommandInfo/Left")]
        public string CommandID;

        [VerticalGroup("CommandInfo/Left")]
        public string Description;

        [VerticalGroup("CommandInfo/Right")]
        [ValueDropdown("GetAllBindableMethods", IsUniqueList = true, DropdownTitle = "Chọn một hàm để gọi")]
        [LabelText("Binding Path")]
        [OnValueChanged("OnBindingPathChanged")]
        public string _selectedMethodPath;

        // --- MỚI: Checkbox tùy chọn nhập Dynamic ---
        [ShowIf("IsMethodWithParameterSelected")]
        [Indent]
        [LabelText("Nhập giá trị khi gõ lệnh?")]
        [Tooltip("Tích: Gõ lệnh kèm số trong game (vd: add_money 100).\nBỏ tích: Set cứng giá trị tại đây.")]
        public bool UseDynamicInput = true;

        // Các trường tham số (Cập nhật ShowIf)
        [ShowIf("ShowBoolParam")]
        [Indent]
        [LabelText("Giá trị (bool)")] public bool BoolParam;

        [ShowIf("ShowIntParam")]
        [Indent]
        [LabelText("Giá trị (int)")] public int IntParam;

        [ShowIf("ShowFloatParam")]
        [Indent]
        [LabelText("Giá trị (float)")] public float FloatParam;

        [ShowIf("ShowStringParam")]
        [Indent]
        [LabelText("Giá trị (string)")] public string StringParam;

        // =================================================================================
        // SECTION: EDITOR-ONLY LOGIC (FOR ODIN INSPECTOR)
        // =================================================================================
#if UNITY_EDITOR

        // Biến tạm để lưu trữ loại tham số, giúp các hàm ShowIf hoạt động
        [HideInInspector]
        private Type _editorCachedParameterType = null;

        private void OnBindingPathChanged()
        {
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

            // Tìm kiếm tất cả các method có tên đó để xử lý overload (nếu có)
            var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == memberName);

            foreach (var methodInfo in methods)
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 1)
                {
                    Type paramType = parameters[0].ParameterType;
                    if (paramType == typeof(bool) || paramType == typeof(int) || paramType == typeof(float) || paramType == typeof(string))
                    {
                        _editorCachedParameterType = paramType;
                        return; // Tìm thấy method hợp lệ đầu tiên thì dừng
                    }
                }
            }
        }

        // --- Các hàm điều kiện cho Odin [ShowIf] ---
        private bool IsMethodWithParameterSelected()
        {
            // Phải gọi hàm update ở đây để đảm bảo UI luôn đúng
            UpdateEditorParameterInfo();
            return _editorCachedParameterType != null;
        }

        // SỬA: Chỉ hiện ô nhập cố định nếu UseDynamicInput == false
        private bool ShowBoolParam() => _editorCachedParameterType == typeof(bool) && !UseDynamicInput;
        private bool ShowIntParam() => _editorCachedParameterType == typeof(int) && !UseDynamicInput;
        private bool ShowFloatParam() => _editorCachedParameterType == typeof(float) && !UseDynamicInput;
        private bool ShowStringParam() => _editorCachedParameterType == typeof(string) && !UseDynamicInput;

        // --- Hàm cung cấp danh sách cho ValueDropdown ---
        private IEnumerable<ValueDropdownItem<string>> GetAllBindableMethods()
        {
            var list = new List<ValueDropdownItem<string>>();
            list.Add(new ValueDropdownItem<string>("(Chưa chọn)", ""));

            var bindingData = AssetDatabase.LoadAssetAtPath<BindingData>(GlobalConst.SettingFolder + "/Binding Setting.asset");

            if (bindingData == null || bindingData.GetBindableTypes() == null)
            {
                // Thử load lại nếu null (đề phòng) hoặc trả về list báo lỗi
                // Giữ nguyên logic cũ của bạn
                if (bindingData == null) return list;
            }

            foreach (var scriptType in bindingData.GetBindableTypes().Where(t => t != null && t.IsSubclassOf(typeof(MonoBehaviour))))
            {
                string groupName = scriptType.Name;
                var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                // Chỉ lấy các hàm có thể gọi (trả về void, không có hoặc có 1 tham số)
                foreach (var method in scriptType.GetMethods(bindingFlags).Where(m => !m.IsSpecialName && m.ReturnType == typeof(void)))
                {
                    var parameters = method.GetParameters();
                    string path = $"{scriptType.AssemblyQualifiedName}.{method.Name}";

                    // Hỗ trợ hàm không có tham số
                    if (parameters.Length == 0)
                    {
                        list.Add(new ValueDropdownItem<string>($"{groupName}/CALL {method.Name}()", path));
                    }
                    // Hỗ trợ hàm có 1 tham số (chỉ các kiểu cơ bản)
                    else if (parameters.Length == 1)
                    {
                        Type paramType = parameters[0].ParameterType;
                        if (paramType == typeof(bool) || paramType == typeof(int) || paramType == typeof(float) || paramType == typeof(string))
                        {
                            list.Add(new ValueDropdownItem<string>($"{groupName}/CALL {method.Name}({paramType.Name})", path));
                        }
                    }
                }
            }
            return list;
        }
#endif
    }
}