using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using static System.Linq.Expressions.Expression;

namespace Watermelon
{
    public class Hierarchy
    {
        private static Type sceneHierarchyWindowType;
        private static Type sceneHierarchyType;
        private static Type treeViewControllerType;
        private static Type treeViewGUIType;

        private static PropertyInfo sceneHierarchyProperty;
        private static FieldInfo hierarchyTreeViewField;
        private static PropertyInfo treeViewGUIProperty;

        private static FieldInfo iconWidthField;
        private static FieldInfo iconSpaceField;

        private EditorWindow window;

        private object sceneHierarchy;
        private object treeViewController;
        private object treeViewGUI;

        private readonly float defaultIconWidth;
        private readonly float defaultSpaceBeforeIcon;

        private static PropertyInfo lastInteractedHierarchyWindow;
        private static Func<object> getLastInteractedHierarchyWindow;
        private static Dictionary<object, Hierarchy> hierarchies = new Dictionary<object, Hierarchy>();
        private static bool isReflectionSupported;

        [InitializeOnLoadMethod]
        private static void PrepareData()
        {
            try
            {
                sceneHierarchyWindowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                if (sceneHierarchyWindowType == null) return;

                sceneHierarchyProperty = sceneHierarchyWindowType.GetProperty("sceneHierarchy");
                if (sceneHierarchyProperty == null) return;

                sceneHierarchyType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchy");
                if (sceneHierarchyType == null) return;

                hierarchyTreeViewField = sceneHierarchyType.GetField("m_TreeView", BindingFlags.NonPublic | BindingFlags.Instance);
                if (hierarchyTreeViewField == null) return;

                treeViewControllerType = typeof(TreeViewState).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
                if (treeViewControllerType == null) return;

                treeViewGUIProperty = treeViewControllerType.GetProperty("gui");
                if (treeViewGUIProperty == null) return;

                treeViewGUIType = typeof(UnityEditor.IMGUI.Controls.TreeView).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewGUI");
                if (treeViewGUIType == null) return;

                iconWidthField = treeViewGUIType.GetField("k_IconWidth", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
                iconSpaceField = treeViewGUIType.GetField("k_SpaceBetweenIconAndText", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);

                lastInteractedHierarchyWindow = sceneHierarchyWindowType.GetProperty("lastInteractedHierarchyWindow", BindingFlags.Public | BindingFlags.Static);
                if (lastInteractedHierarchyWindow == null) return;

                getLastInteractedHierarchyWindow = Lambda<Func<object>>(Property(null, lastInteractedHierarchyWindow)).Compile();

                isReflectionSupported = iconWidthField != null && iconSpaceField != null && getLastInteractedHierarchyWindow != null;
            }
            catch
            {
                isReflectionSupported = false;
            }
        }

        public Hierarchy(EditorWindow window)
        {
            this.window = window;

            if (window == null || !isReflectionSupported) return;

            try
            {
                sceneHierarchy = sceneHierarchyProperty.GetValue(window);
                treeViewController = hierarchyTreeViewField.GetValue(sceneHierarchy);
                treeViewGUI = treeViewGUIProperty.GetValue(treeViewController);

                defaultIconWidth = (float)iconWidthField.GetValue(treeViewGUI);
                defaultSpaceBeforeIcon = (float)iconSpaceField.GetValue(treeViewGUI);

                SetIconWidth(0, 18);
            }
            catch
            {
                // Silence warning to keep console clean
            }
        }

        private void SetIconWidth(float iconWidth, float spaceBeforeIcon)
        {
            if (!isReflectionSupported || treeViewGUI == null) return;
            try
            {
                iconWidthField.SetValue(treeViewGUI, iconWidth);
                iconSpaceField.SetValue(treeViewGUI, spaceBeforeIcon);
            }
            catch {}
        }

        private void ResetIconWidth()
        {
            SetIconWidth(defaultIconWidth, defaultSpaceBeforeIcon);
        }

        public void DrawElementGUI(int instanceID, Rect selectionRect)
        {
            if (!isReflectionSupported || treeViewGUI == null) return;

            GameObject instanceObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (!instanceObject) return;

            Texture texture = EditorCustomHierarchy.GetTexture(instanceObject);

            if (texture == null) return;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                ResetIconWidth();

                return;
            }

            SetIconWidth(0, 18);

            Rect iconRect = new Rect(selectionRect) { width = 16, height = 16 };
            iconRect.y += (iconRect.height - 16) / 2;

            using(new ColorScope(EditorCustomStyles.HIERARCHY_COLOR))
            {
                GUI.DrawTexture(iconRect, texture);
            }
        }

        public static Hierarchy GetLastHierarchy()
        {
            if (!isReflectionSupported || getLastInteractedHierarchyWindow == null) return null;

            try
            {
                object lastHierarchyWindow = getLastInteractedHierarchyWindow();
                if (lastHierarchyWindow == null) return null;

                if (!hierarchies.TryGetValue(lastHierarchyWindow, out var hierarchy))
                {
                    hierarchy = new Hierarchy(lastHierarchyWindow as EditorWindow);
                    hierarchies.Add(lastHierarchyWindow, hierarchy);
                }

                return hierarchy;
            }
            catch
            {
                return null;
            }
        }

        public static void ClearHierarchies()
        {
            if (hierarchies.Count == 0) return;

            foreach(Hierarchy hierarchy in hierarchies.Values)
            {
                if(hierarchy != null)
                {
                    hierarchy.ResetIconWidth();
                }
            }

            hierarchies.Clear();
        }
    }
}
