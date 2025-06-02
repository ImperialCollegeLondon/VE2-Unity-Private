using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IBaseStateModule
    {
        public void SetNetworked(bool isNetworked);
        public bool IsNetworked { get; }
    }
}
