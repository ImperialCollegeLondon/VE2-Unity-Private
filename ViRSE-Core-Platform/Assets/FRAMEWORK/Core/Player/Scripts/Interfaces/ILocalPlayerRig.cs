using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;

namespace ViRSE.Core.Player
{
    public interface ILocalPlayerRig
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }
}

/*
 * Do we still call this "local player" if it might all be single player?
 * Maybe just "player"?
 * What's the flow here 
 * We want something to spawn the player 
 * That something will contain the config... PlayerControlConfig, maybe a Player2DControlConfig and a PlayerVRControlConfig
 * V_PlayerSpawner will also have the interface to the player
 */

