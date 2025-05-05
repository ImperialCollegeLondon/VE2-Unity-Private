using UnityEngine;

namespace VE2.Common.API
{
    public interface IBaseStateModule
    {
        public void SetNetworked(bool isNetworked);
        public bool IsNetworked { get; }
    }
}
