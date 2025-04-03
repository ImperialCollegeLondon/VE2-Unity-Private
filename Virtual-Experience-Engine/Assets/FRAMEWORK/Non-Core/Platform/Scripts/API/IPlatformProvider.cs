using UnityEngine;

namespace VE2.NonCore.Platform.API
{
    internal interface IPlatformProvider
    {
        public IPlatformService PlatformService { get; }
        public string GameObjectName { get; }
    }
}
