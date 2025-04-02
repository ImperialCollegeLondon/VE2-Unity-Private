using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.Core.Common;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUIQuickPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _platformQuickPanelHolder;
        [SerializeField] private GameObject _platformPromoPanel;

        internal void SetPlatformQuickpanel(GameObject platformQuickPanel) 
        {
            CommonUtils.MovePanelToFillRect(platformQuickPanel.GetComponent<RectTransform>(), _platformQuickPanelHolder.GetComponent<RectTransform>());
            platformQuickPanel.SetActive(true);
            _platformPromoPanel.SetActive(false);
        }
    }
}
