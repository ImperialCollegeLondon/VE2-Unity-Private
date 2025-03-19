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

        public static void MovePanelToFillRect(RectTransform panelRect, RectTransform targetRect)
        {
            if (panelRect == null)
            {
                Debug.LogError("MovePanelToFillCanvas: panel to move is null");
                return;
            }

            if (targetRect == null)
            {
                Debug.LogError("MovePanelToFillCanvas: target Canvas is null.");
                return;
            }

            // Get the Canvas's RectTransform
            RectTransform canvasRect = targetRect.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Debug.LogError("MovePanelToFillCanvas: Target Canvas does not have a RectTransform.");
                return;
            }

            // Reparent while preserving local position
            panelRect.SetParent(canvasRect, false);

            // Stretch the panel to fill the entire canvas
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
            panelRect.localRotation = Quaternion.identity;

            // Ensure layout updates correctly
            panelRect.ForceUpdateRectTransforms();
        }

    }
}
