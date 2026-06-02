using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheLegends.Base.Ads.Editor
{
    public class AdsMediationDrawer
    {
        public void Draw(AdsSettings instance, GUIStyle titleStyle)
        {
            EditorGUILayout.LabelField("Ad Networks", titleStyle);
            EditorGUILayout.Space(2);
            DrawMediationGrid(instance);
            EditorGUILayout.Space(4);
            DrawPrimaryMediationPopup(instance);
        }

        private void DrawMediationGrid(AdsSettings instance)
        {
            // Define all known mediations with their labels. Add more here as needed.
            var mediations = new (AdsMediation mediation, string label, bool enabled)[]
            {
                (AdsMediation.Admob, "Google Admob",   instance.showADMOB),
                (AdsMediation.Max,   "AppLovin MAX",   instance.showMAX),
                (AdsMediation.Iron,  "IronSource / LP",instance.showIRON),
                // Future mediations go here — the grid wraps automatically.
            };

            const int columns = 3;
            int col = 0;

            for (int i = 0; i < mediations.Length; i++)
            {
                if (col == 0) EditorGUILayout.BeginHorizontal();

                var (mediation, label, enabled) = mediations[i];
                bool newEnabled = GUILayout.Toggle(enabled, label, "Button",
                    GUILayout.Height(24), GUILayout.MinWidth(90));

                if (newEnabled != enabled)
                {
                    // Sync AdsSettings flags
                    if (newEnabled) instance.FlagMediations |= mediation;
                    else instance.FlagMediations &= ~mediation;

                    switch (mediation)
                    {
                        case AdsMediation.Admob: instance.showADMOB = newEnabled; break;
                        case AdsMediation.Max: instance.showMAX = newEnabled; break;
                        case AdsMediation.Iron: instance.showIRON = newEnabled; break;
                    }
                }

                col++;
                if (col == columns || i == mediations.Length - 1)
                {
                    // Pad remaining cells so the grid stays aligned
                    while (col < columns) { GUILayout.Label(GUIContent.none, GUILayout.MinWidth(90)); col++; }
                    EditorGUILayout.EndHorizontal();
                    col = 0;
                }
            }
        }

        private void DrawPrimaryMediationPopup(AdsSettings instance)
        {
            var active = GetActiveMediationList(instance);
            if (active.Count >= 2)
            {
                if (!active.Contains(instance.primaryMediation))
                    instance.primaryMediation = active[0];

                var names = active.Select(n => n.ToString()).ToArray();
                var values = active.Select(n => (int)n).ToArray();
                int idx = Array.IndexOf(values, (int)instance.primaryMediation);
                int newIdx = EditorGUILayout.Popup(
                    new GUIContent("  Primary Mediation",
                        "The mediation used as the main ad source when multiple are enabled."),
                    idx, names);
                if (newIdx != idx)
                    instance.primaryMediation = (AdsMediation)values[newIdx];
            }
            else
            {
                instance.primaryMediation = active.Count == 1 ? active[0] : AdsMediation.None;
            }
        }

        public List<AdsMediation> GetActiveMediationList(AdsSettings instance)
        {
            var list = new List<AdsMediation>();
            foreach (AdsMediation n in Enum.GetValues(typeof(AdsMediation)))
            {
                if (n == AdsMediation.None) continue;
                if ((instance.FlagMediations & n) == n) list.Add(n);
            }
            return list;
        }

        public int CountActiveMediations(AdsSettings instance)
        {
            return GetActiveMediationList(instance).Count;
        }
    }
}
