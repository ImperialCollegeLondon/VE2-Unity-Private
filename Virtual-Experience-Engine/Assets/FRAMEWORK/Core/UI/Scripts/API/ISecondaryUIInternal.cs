using UnityEngine;

namespace VE2.Core.UI.API
{
    internal interface ISecondaryUIServiceInternal : ISecondaryUIService
    {
        public void MoveSecondaryUIToHolderRect(RectTransform rect);
    }
}
