using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    internal interface IPlayerServiceInternal : IPlayerService
    {
        public bool RememberPlayerSettings { get; set; }

        public PlayerTransformData PlayerTransformData { get; }

        /// <summary>
        /// Note - this shouldn't be changed directly, use the SetBuiltInHead/Torso methods instead.
        /// </summary>
        public InstancedAvatarAppearance InstancedAvatarAppearance { get; }
        //public void MarkPlayerAvatarChanged() { }
        public event Action<InstancedAvatarAppearance> OnInstancedAvatarAppearanceChanged;

        public AvatarPrefabs BuiltInGameObjectPrefabs { get; }
        public AvatarPrefabs CustomGameObjectPrefabs { get; }

        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent);

        public void AddPanelTo2DOverlayUI(RectTransform rect);

        public void SetBuiltInHeadIndex(ushort type);
        public void SetBuiltInTorsoIndex(ushort type);
        public void SetBuiltInColor(Color color);

        public Collider CharacterCollider2D { get; }
    }
    
    [Serializable]
    internal class AvatarPrefabs
    {
        [SerializeField, ReorderableList, PropertyOrder(1)] internal List<GameObject> Heads = new();
        [SerializeField, ReorderableList, PropertyOrder(3)] internal List<GameObject> Torsos = new();

        [Help("The left hand should be supplied, this will be mirrored at runtime for the right hand.")]
        [SerializeField, ReorderableList, PropertyOrder(5)] internal List<GameObject> VRHands = new();

        public AvatarPrefabs(List<GameObject> heads, List<GameObject> torsos, List<GameObject> vrHands)
        {
            Heads = heads;
            Torsos = torsos;
            VRHands = vrHands;
        }

        public AvatarPrefabs() { }
    }
}