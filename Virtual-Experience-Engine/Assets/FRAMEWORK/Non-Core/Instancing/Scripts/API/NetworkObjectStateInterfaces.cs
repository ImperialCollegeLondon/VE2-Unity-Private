using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.NonCore.Instancing.API
{
    public interface INetworkObjectStateModule
    {
        public UnityEvent<object> OnStateChange { get; }

        public object NetworkObject { get; }

        public void UpdateDataFromPlugin(object networkObject);
    }
}
