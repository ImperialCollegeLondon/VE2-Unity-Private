using UnityEngine;

namespace VE2.NonCore.Instancing.API
{
    internal interface IInstanceProvider
    {
        public IInstanceService InstanceService { get; }
        public string GameObjectName { get; }
        public bool IsEnabled { get; }
    }
}
