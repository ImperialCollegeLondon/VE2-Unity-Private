using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.Core.VComponents.InternalInterfaces
{
    public interface ISingleInteractorActivatableStateModuleImplementor
    {
        public ISingleInteractorActivatableStateModule StateModule { get; }
    }

    public interface ISingleInteractorActivatableStateModule
    {
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; set; }
        public ushort MostRecentInteractingClientID { get; }
    }
}
