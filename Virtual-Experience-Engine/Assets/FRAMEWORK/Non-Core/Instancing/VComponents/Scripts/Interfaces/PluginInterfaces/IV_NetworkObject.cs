using UnityEngine.Events;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;

namespace VE2.NonCore.Instancing.VComponents.PluginInterfaces
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