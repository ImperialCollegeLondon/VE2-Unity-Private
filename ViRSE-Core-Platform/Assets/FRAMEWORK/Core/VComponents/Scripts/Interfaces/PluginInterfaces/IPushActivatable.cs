using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.VComponents
{
    public interface IV_PushActivatable : ISingleInteractorActivatableStateModuleIntegrator, IGeneralInteractionModuleIntegrator, IRangedInteractionModuleIntegrator 
    {
        ISingleInteractorActivatableStateModuleImplementor ISingleInteractorActivatableStateModuleIntegrator._implementor => _pushActivatableService;
        IGeneralInteractionModuleImplementor IGeneralInteractionModuleIntegrator._implementor => _pushActivatableService;
        IRangedInteractionModuleImplementor IRangedInteractionModuleIntegrator._implementor => _pushActivatableService;

        protected IPushActivatable _pushActivatableService {get;}
    }

    public interface IPushActivatable : ISingleInteractorActivatableStateModuleImplementor, IGeneralInteractionModuleImplementor, IRangedInteractionModuleImplementor { }
}