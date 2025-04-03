using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_NetworkObject
    {
        #region State Module Interface
        protected INetworkObjectStateModule _StateModule { get; }
        public UnityEvent<object> OnStateChange => _StateModule.OnStateChange;
        public object NetworkObject { get => _StateModule.NetworkObject; set => _StateModule.NetworkObject = value; }
        #endregion
    }
}
