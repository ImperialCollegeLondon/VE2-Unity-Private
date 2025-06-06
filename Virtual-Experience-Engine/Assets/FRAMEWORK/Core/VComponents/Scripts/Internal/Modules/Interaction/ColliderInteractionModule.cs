using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class CollisionClickInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Collision Click Interaction Settings")]
        [SerializeField, IgnoreParent, EndGroup] public bool ClickWithCollisionInVR = true;
    }

    internal class ColliderInteractionModule : GeneralInteractionModule, ICollideInteractionModule
    {
        public bool IsNetworked => _syncConfig.IsNetworked;

        public event Action<InteractorID> OnCollideEnter;
        public event Action<InteractorID> OnCollideExit;
        public CollideInteractionType CollideInteractionType => _collideInteractionType;

        public void InvokeOnCollideEnter(InteractorID id) => OnCollideEnter?.Invoke(id);
        public void InvokeOnCollideExit(InteractorID id) => OnCollideExit?.Invoke(id);

        public string ID { get; }
        private CollideInteractionType _collideInteractionType;
        private readonly HoldActivatablePlayerSyncIndicator _syncConfig;

        public ColliderInteractionModule(CollisionClickInteractionConfig collClickConfig, GeneralInteractionConfig generalConfig, HoldActivatablePlayerSyncIndicator syncConfig, string id) : base(generalConfig)
        {
            ID = id;
            _collideInteractionType = collClickConfig.ClickWithCollisionInVR
                 ? CollideInteractionType.Hand
                 : CollideInteractionType.None;
            _syncConfig = syncConfig;
        }

        public ColliderInteractionModule(CollideInteractionType collideInteractionType, GeneralInteractionConfig generalConfig, HoldActivatablePlayerSyncIndicator syncConfig, string id) : base(generalConfig)
        {
            ID = id;
            _collideInteractionType = collideInteractionType;
            _syncConfig = syncConfig;
        }
    }
}
