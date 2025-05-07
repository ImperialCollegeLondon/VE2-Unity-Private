using UnityEngine;

namespace VE2.Core.UI.API
{
    internal interface IUIProvider
    {
        public IPrimaryUIService PrimaryUIService { get; }
        public ISecondaryUIService SecondaryUIService { get; }
        public bool IsEnabled { get; }
    }
}