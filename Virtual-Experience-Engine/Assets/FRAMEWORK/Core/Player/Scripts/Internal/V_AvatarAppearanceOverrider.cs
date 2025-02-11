using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    [ExecuteInEditMode]
    public class V_AvatarAppearanceOverrider : MonoBehaviour, IPlayerAppearanceOverridesProvider
    {
        [Title("Avatar Presentation Override Selection")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField] private AvatarAppearanceOverrideType HeadOverrideType = AvatarAppearanceOverrideType.None;
        [EditorButton(nameof(NotifyProviderOfChangeAppearanceOverrides), "Update overrides", activityType: ButtonActivityType.OnPlayMode)]
        [EndGroup, SerializeField] private AvatarAppearanceOverrideType TorsoOverrideType = AvatarAppearanceOverrideType.None;

        [Title("Head Overrides")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, AssetPreview] private GameObject HeadOverrideOne;
        [SerializeField, AssetPreview] private GameObject HeadOverrideTwo;
        [SerializeField, AssetPreview] private GameObject HeadOverrideThree;
        [SerializeField, AssetPreview] private GameObject HeadOverrideFour;
        [EndGroup, SerializeField, AssetPreview] private GameObject HeadOverrideFive;

        [Title("Torso Overrides")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, AssetPreview] private GameObject TorsoOverrideOne;
        [SerializeField, AssetPreview] private GameObject TorsoOverrideTwo;
        [SerializeField, AssetPreview] private GameObject TorsoOverrideThree;
        [SerializeField, AssetPreview] private GameObject TorsoOverrideFour;
        [EndGroup, SerializeField, AssetPreview] private GameObject TorsoOverrideFive;
        
        #region Appearance Overrides Interfaces 
        public void NotifyProviderOfChangeAppearanceOverrides() => OnAppearanceOverridesChanged?.Invoke();
        public event Action OnAppearanceOverridesChanged;
        public bool IsEnabled => enabled;
        public string GameObjectName => gameObject.name;

        AvatarAppearanceOverrideType IPlayerAppearanceOverridesProvider.HeadOverrideType => HeadOverrideType;
        AvatarAppearanceOverrideType IPlayerAppearanceOverridesProvider.TorsoOverrideType => TorsoOverrideType;
        public List<GameObject> HeadOverrideGOs => new() { HeadOverrideOne, HeadOverrideTwo, HeadOverrideThree, HeadOverrideFour, HeadOverrideFive };
        public List<GameObject> TorsoOverrideGOs => new() { TorsoOverrideOne, TorsoOverrideTwo, TorsoOverrideThree, TorsoOverrideFour, TorsoOverrideFive }; 
        #endregion

        //TODO - need an API for changing overrrides 

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                PlayerLocator.Instance.PlayerAppearanceOverridesProvider = this;
                return;
            }
        }
    }
}

