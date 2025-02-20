using UnityEngine;

namespace VE2.Core.Player.API
{
    internal interface IPlayerServiceProvider
    {
        public IPlayerService PlayerService { get; }
        public string GameObjectName { get; }
    }
}