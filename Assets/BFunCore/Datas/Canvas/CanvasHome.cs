#if BFUN_INSTALLED_TRUE
using System.Collections;
using System.Collections.Generic;
using BFunCoreKit;
using UnityEngine;

namespace BFunCoreKit
{
    public class CanvasHome : CanvasBase<CanvasHome>
    {
        public IEnumerator ShowPanel(PanelHome panelName, string effectOption = "Default")
        {
            yield return uiPanel.ShowPanel(panelName.ToString(), effectOption);
        }

        public IEnumerator ClosePanel(PanelHome panelName, string effectOption = "Default")
        {
            yield return uiPanel.ClosePanel(panelName.ToString(), effectOption);
        }
    }
}
#endif
