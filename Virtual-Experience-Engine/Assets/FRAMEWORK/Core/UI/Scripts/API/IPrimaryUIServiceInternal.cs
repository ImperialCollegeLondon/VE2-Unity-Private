using System;
using UnityEngine;

namespace VE2.Core.UI.API
{
    internal interface IPrimaryUIServiceInternal : IPrimaryUIService
    {
        public void MovePrimaryUIToHolderRect(RectTransform rect);

        public void SetPlatformQuickpanel(GameObject platformQuickPanel);

        public event Action OnSwitchTo2DButtonClicked;
        public event Action OnSwitchToVRButtonClicked;

        public event Action OnUIShowInternal;
        public event Action OnUIHideInternal;
        public void EnableModeSwitchButtons();
        public void ShowSwitchToVRButton();
        public void ShowSwitchTo2DButton();

        public void SetInstanceCodeText(string text);
    }
}
