using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_NetworkObject
    {
        internal INetworkObjectStateModule _stateModule { get; }

        #region State Module Interface
        public UnityEvent<object> OnDataChange { get; }
        public object CurrentData { get; }

        //NOTE: Deviation from the pattern here, unlike other methods, we DON'T want this one showing up in the inspector 
        //E.G available as a listener in the inspector for OnActivate...
        //Plugin devs frequently wire this up to V_NetworkObject.OnDataChange.. causing problems!!
        public void UpdateData(object data) => _stateModule.NetworkObject = data;
        #endregion
    }
}
