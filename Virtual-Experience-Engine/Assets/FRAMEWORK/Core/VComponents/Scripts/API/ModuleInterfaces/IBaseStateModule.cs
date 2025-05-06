using UnityEngine;

namespace VE2.Core.VComponents.API
{
    public interface IBaseStateModule
    {
        public void SetNetworked(bool isNetworked);
        public bool IsNetworked { get; }
    }
}
