using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface ISingleInteractorActivatableStateModuleImplementor
    {
        protected ISingleInteractorActivatableStateModule _module { get; }

        public UnityEvent OnActivate => _module.OnActivate;
        public UnityEvent OnDeactivate => _module.OnDeactivate;

        public bool IsActivated { get { return _module.IsActivated; } set { _module.IsActivated = value; } }
        public InteractorID CurrentInteractor => _module.CurrentInteractor;
    }

    public interface ISingleInteractorActivatableStateModule
    {
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; set; }
        public InteractorID CurrentInteractor { get; }
    }
}