using System;
using TMPro;
using UnityEngine;
using VE2.Common.Shared;

namespace VE2.Core.UI.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
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
