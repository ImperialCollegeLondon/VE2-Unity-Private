using System.Diagnostics;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class V_TeleportAnchor : MonoBehaviour
    {
        [SerializeField, Range(0.75f, 2.5f)] public float Range = 0.75f;

        void OnEnable()
        {
            
        }
    }
}