using UnityEngine;

namespace VE2.Core.UI.Internal
{
    internal class UIUtils
    {
        // public static void MoveUIToCanvas(Canvas canvas)
        // {
        //     throw new System.NotImplementedException();
        // }

        // public static void MovePanelToMatchGuide(GameObject panelToMove, GameObject guidePanel)
        // {
        //     //Set parent to the panel holder and update scale
        //     panelToMove.transform.SetParent(guidePanel.transform.parent, true);  //Parent the panel to the holder

        //     //Set the position to match the guide
        //     RectTransform panelToAddRect = panelToMove.GetComponent<RectTransform>();
        //     RectTransform guidePanelRect = guidePanel.GetComponent<RectTransform>();
        //     panelToAddRect.localScale = guidePanelRect.localScale;
        //     panelToAddRect.localPosition = guidePanelRect.localPosition;
        //     panelToAddRect.offsetMin = guidePanelRect.offsetMin;
        //     panelToAddRect.offsetMax = guidePanelRect.offsetMax;
        //     panelToAddRect.rotation = guidePanelRect.rotation;

        //     guidePanel.SetActive(false);
        //     panelToAddRect.ForceUpdateRectTransforms();
        // }

    }
}
