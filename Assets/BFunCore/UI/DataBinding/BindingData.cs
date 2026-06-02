using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BFunCoreKit
{
    public class BindingData : ScriptableObject
    {
#if UNITY_EDITOR
        [Header("EDITOR ONLY - Drag MonoScripts here")]
        public List<MonoScript> _bindableScripts = new List<MonoScript>();
#endif

        [HideInInspector]
        public List<string> _bindableTypeNames = new List<string>();

#if UNITY_EDITOR
        public void OnValidate()
        {
            _bindableTypeNames.Clear();

            foreach (var script in _bindableScripts)
            {
                if (script == null) continue;
                Type t = script.GetClass();
                if (t != null)
                {
                    _bindableTypeNames.Add(t.AssemblyQualifiedName);
                }
            }

            EditorUtility.SetDirty(this);
        }
#endif

        public IEnumerable<Type> GetBindableTypes()
        {
            var list = new List<Type>();
            foreach (var name in _bindableTypeNames)
            {
                var t = Type.GetType(name);
                if (t != null)
                    list.Add(t);
            }
            return list;
        }
    }
}
