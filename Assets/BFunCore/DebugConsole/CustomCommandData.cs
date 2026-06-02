// FILE: CustomCommandData.cs
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BFunCoreKit
{
    public class CustomCommandData : ScriptableObject
    {

        [ListDrawerSettings(OnBeginListElementGUI = "BeginDrawListElement", OnEndListElementGUI = "EndDrawListElement")]
        public List<CustomCommandEntry> Commands = new List<CustomCommandEntry>();

#if UNITY_EDITOR
        public void Save()
        {
            CommandGenerator.GenerateCommandsFile(this);
        }

        // Các hàm này chỉ để vẽ cho đẹp trong Odin
        private void BeginDrawListElement(int index)
        {
            Sirenix.Utilities.Editor.SirenixEditorGUI.BeginBox(this.Commands[index].CommandID ?? "New Command");
        }

        private void EndDrawListElement(int index)
        {
            Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
        }
#endif
    }
}