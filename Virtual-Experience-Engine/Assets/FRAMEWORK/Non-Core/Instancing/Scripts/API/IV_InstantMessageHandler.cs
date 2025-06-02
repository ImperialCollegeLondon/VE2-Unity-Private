using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_InstantMessageHandler
    {
        public void SendInstantMessage(object message);

        public UnityEvent<object> OnMessageReceived { get; }
    }
}