using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.Core.VComponents
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

//These interfaces can't see anything that lives in the VC assembly 
//So I guess that means we need to either...
//1. Not pass the entire config to the network
//      That might make sense, it's not like we pass the entire config for everything else 
//      Right, the state module just has exposed stuff for frequency and protocol 
//2. 