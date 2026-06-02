using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;

namespace BFunCoreKit
{
    /// <summary>
    /// Script duy nhất trong project, hoạt động như một trung tâm tham chiếu (Singleton).
    /// Nó giữ một danh sách các script (dưới dạng MonoScript từ Assets) để DataBinder có thể truy vấn.
    /// </summary>
    public class BindingManager : Singleton<BindingManager>
    {
        [ReadOnly] public BindingData bindingData;

        // Dictionary để cache các tham chiếu tìm thấy tại Runtime. Việc truy cập Dictionary cực kỳ nhanh.
        private readonly Dictionary<Type, MonoBehaviour> _runtimeReferences = new Dictionary<Type, MonoBehaviour>();

        public override void Awake()
        {
            base.Awake();
            bindingData = Resources.Load<BindingData>("Binding Setting");
            LoadAllRuntimeReferences();
        }

        /// <summary>
        /// Tìm và cache tất cả các instance của các script đã được định nghĩa trong _bindableScripts.
        /// Hàm này chỉ chạy MỘT LẦN lúc Awake, chi phí tìm kiếm được trả trước và không ảnh hưởng đến hiệu năng trong game.
        /// </summary>
        private void LoadAllRuntimeReferences()
        {
            _runtimeReferences.Clear();

            var bindingData = this.bindingData;
            if (bindingData == null) return;

            foreach (var type in bindingData.GetBindableTypes())
            {
                if (type == null || !type.IsSubclassOf(typeof(MonoBehaviour))) continue;

                var instance = FindObjectOfType(type, true) as MonoBehaviour;
                if (instance != null)
                {
                    _runtimeReferences[type] = instance;
                }
                else
                {
                    //Debug.LogWarning($"[BindingManager] Không tìm thấy instance nào của '{type.Name}' trong scene.");
                }
            }
        }

        /// <summary>
        /// Lấy tham chiếu đã được cache của một script tại Runtime. Thao tác này rất nhanh (O(1)).
        /// </summary>
        public T GetRef<T>() where T : MonoBehaviour
        {
            if (_runtimeReferences.TryGetValue(typeof(T), out var reference))
            {
                return reference as T;
            }
            return null;
        }

        /// <summary>
        /// (Tối ưu cho DataBinder) Lấy tham chiếu đã được cache của một script tại Runtime bằng Type. Thao tác này rất nhanh (O(1)).
        /// </summary>
        public MonoBehaviour GetRef(Type type)
        {
            if (_runtimeReferences.TryGetValue(type, out var reference))
            {
                return reference;
            }
           // Debug.LogWarning($"[ScriptReferenceList] Không tìm thấy tham chiếu runtime cho kiểu '{type.Name}'.", this);
            return null;
        }
    }
}