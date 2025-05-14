using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_NetworkObject
    {
        #region State Module Interface
        protected INetworkObjectStateModule _StateModule { get; }
        public UnityEvent<object> OnDataChange => _StateModule.OnStateChange;
        public object CurrentData => _StateModule.NetworkObject;
        public void UpdateData(object data) => _StateModule.NetworkObject = data;
        #endregion
    }
}
