using UnityEngine;

namespace VE2.Common.Shared
{
    internal interface ILocalInteractor: IInteractor
    {
        public bool IsCurrentlyGrabbing { get; }

        public bool TryLocalDrop();
    }
}
