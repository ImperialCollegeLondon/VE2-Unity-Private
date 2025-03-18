using UnityEngine;

namespace VE2.Core.UI.API
{
    internal interface ISecondaryUIServiceInternal : ISecondaryUIService
    {
        public void MoveSecondaryUIToHolderRect(RectTransform rect);
        public void SetContent(RectTransform contentRect);
        public void EnableShowHideKeyboardControl();
        public void DisableShowHideKeyboardControl();
    }
}
