using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_RigidbodySyncable
    {
        #region State Module Interface
        protected IRigidbodySyncableStateModule _StateModule { get; }

        // Not sure what state module interface elements should go here yet...
        #endregion
    }
}