using UnityEngine.Events;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;

namespace VE2.NonCore.Instancing.VComponents.PluginInterfaces
{
    public interface IV_RigidbodySyncable
    {
        #region State Module Interface
        protected IRigidbodySyncableStateModule _StateModule { get; }

        // Not sure what state module interface elements should go here yet...
        #endregion
    }
}