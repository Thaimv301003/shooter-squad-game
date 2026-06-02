#if BFUN_INSTALLED_TRUE
using System.Collections;
using System.Collections.Generic;
using BFunCoreKit;
using UnityEngine;

namespace BFunCoreKit
{
    public class CanvasGlobal : CanvasBase<CanvasGlobal>
    {
        public IEnumerator ShowPanel(PanelGlobal panelName, string effectOption = "Default")
        {
            yield return uiPanel.ShowPanel(panelName.ToString(), effectOption);
        }

        public IEnumerator ClosePanel(PanelGlobal panelName, string effectOption = "Default")
        {
            yield return uiPanel.ClosePanel(panelName.ToString(), effectOption);
        }
    }
}
#endif
