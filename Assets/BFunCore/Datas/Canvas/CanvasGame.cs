#if BFUN_INSTALLED_TRUE
using System.Collections;
using System.Collections.Generic;
using BFunCoreKit;
using UnityEngine;

namespace BFunCoreKit
{
    public class CanvasGame : CanvasBase<CanvasGame>
    {
        public IEnumerator ShowPanel(PanelGame panelName, string effectOption = "Default")
        {
            yield return uiPanel.ShowPanel(panelName.ToString(), effectOption);
        }

        public IEnumerator ClosePanel(PanelGame panelName, string effectOption = "Default")
        {
            yield return uiPanel.ClosePanel(panelName.ToString(), effectOption);
        }
    }
}
#endif
