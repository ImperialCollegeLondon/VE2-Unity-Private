using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class V_InstantMessageHandler : MonoBehaviour, IV_InstantMessageHandler
    {

        private InstantMessageHandlerService _service;

        private void OnEnable()
        {
            string id = "IMH-" + gameObject.name;
            _service = new InstantMessageHandlerService(id);
        }

        public void SendInstantMessage(object messageObject)
        {
            _service.SendInstantMessage(messageObject);
        }
    }
}
