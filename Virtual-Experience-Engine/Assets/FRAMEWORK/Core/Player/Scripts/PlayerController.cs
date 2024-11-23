using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;

namespace VE2.Core.Player 
{
    public abstract class PlayerController : MonoBehaviour //TODO: Can this just be an interface? Or do we even need MB at all here?
    {
        public abstract PlayerTransformData PlayerTransformData { get; }
        public virtual void ActivatePlayer(PlayerTransformData initialTransformData) => gameObject.SetActive(true);
        public virtual void DeactivatePlayer() => gameObject.SetActive(false);
    }
}

