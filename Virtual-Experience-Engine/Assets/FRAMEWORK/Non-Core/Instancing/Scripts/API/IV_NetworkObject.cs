using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_NetworkObject
    {
        #region State Module Interface
        public UnityEvent<object> OnDataChange { get; }
        public object CurrentData { get; }
        public void UpdateData(object data);
        #endregion
    }
}
