using System.IO;
using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    internal interface IInstantMessageHandlerInternal
    {
        public void ReceiveInstantMessage(MemoryStream serializedMessage);
    }
}