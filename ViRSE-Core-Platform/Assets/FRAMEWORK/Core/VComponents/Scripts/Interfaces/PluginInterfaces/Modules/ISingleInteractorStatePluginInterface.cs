using UnityEngine;
using UnityEngine.Events;
using ViRSE.Core.VComponents.InternalInterfaces;

public interface ISingleInteractorStateModulePluginInterface
{
    protected ISingleInteractorActivatableStateModuleImplementor _StateModuleImplementor { get; }

    public UnityEvent OnActivate => _StateModuleImplementor.StateModule.OnActivate;
    public UnityEvent OnDeactivate => _StateModuleImplementor.StateModule.OnDeactivate;

    public bool IsActivated { get { return _StateModuleImplementor.StateModule.IsActivated; } set { _StateModuleImplementor.StateModule.IsActivated = value; } }
    public ushort MostRecentInteractingClientID => _StateModuleImplementor.StateModule.MostRecentInteractingClientID;
}
