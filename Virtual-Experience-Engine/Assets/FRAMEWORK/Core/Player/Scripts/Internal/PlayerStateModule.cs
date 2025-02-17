using System;
using System.IO;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    //TODO: Rename to PlayerTransformState?
    // public class PlayerStateModule : BaseStateModule, IPlayerStateModule //TODO - customer interfaces for changing player position/rotation? Those should maybe be on the service module.
    // {
    //     public PlayerTransformData PlayerTransformData
    //     {
    //         get => (PlayerTransformData)State;
    //         set => State.Bytes = value.Bytes;
    //     }


    //     public AvatarAppearance AvatarAppearance { get; }
    //     public event Action<AvatarAppearance> OnAvatarAppearanceChanged;

    //     public PlayerStateModule(PlayerTransformData state, BaseStateConfig config, PlayerStateModuleContainer playerStateModuleContainer)
    //             : base(state, config, playerStateModuleContainer)
    //     {
    //         PlayerTransformData = state;
    //     }

    //     public override void TearDown()
    //     {
    //         base.TearDown();
    //     }
    // }

}

