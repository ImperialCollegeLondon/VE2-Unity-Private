using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.Core.VComponents
{
    public interface ISingleInteractorActivatableStateModuleIntegrator
    {
        protected abstract ISingleInteractorActivatableStateModuleImplementor _implementor { get; }

        public UnityEvent OnActivate => _implementor.OnActivate;
        public UnityEvent OnDeactivate => _implementor.OnDeactivate;

        public bool IsActivated { get { return _implementor.IsActivated; } set { _implementor.IsActivated = value; } }
        public InteractorID CurrentInteractor => _implementor.CurrentInteractor;
    }

    public interface ISingleInteractorActivatableStateModuleImplementor
    {
        protected ISingleInteractorActivatableStateModule _stateModule { get; }

        public UnityEvent OnActivate => _stateModule.OnActivate;
        public UnityEvent OnDeactivate => _stateModule.OnDeactivate;

        public bool IsActivated { get { return _stateModule.IsActivated; } set { _stateModule.IsActivated = value; } }
        public InteractorID CurrentInteractor => _stateModule.CurrentInteractor;
    }

    public interface ISingleInteractorActivatableStateModule
    {
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; set; }
        public InteractorID CurrentInteractor { get; }
    }
}
