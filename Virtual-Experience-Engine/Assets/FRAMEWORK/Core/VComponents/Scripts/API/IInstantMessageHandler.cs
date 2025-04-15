using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    internal interface IInstantMessageHandler
    {
        public void ReceiveInstantMessage(object message);
    }
}