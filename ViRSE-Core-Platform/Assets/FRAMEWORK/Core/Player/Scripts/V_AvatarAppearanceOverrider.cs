using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    [ExecuteInEditMode]
    public class V_AvatarAppearanceOverrider : MonoBehaviour, IPlayerAppearanceOverridesProvider
    {
        [Title("Avatar Presentation Override Selection")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField] public AvatarAppearanceOverrideType HeadOverrideType = AvatarAppearanceOverrideType.None;
        [EditorButton(nameof(NotifyProviderOfChangeAppearanceOverrides), "Update overrides", activityType: ButtonActivityType.OnPlayMode)]
        [EndGroup, SerializeField] public AvatarAppearanceOverrideType TorsoOverrideType = AvatarAppearanceOverrideType.None;

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

        public GameObject GetHeadOverrideGO(AvatarAppearanceOverrideType overrideType)
        {
            return overrideType switch
            {
                AvatarAppearanceOverrideType.None => null,
                AvatarAppearanceOverrideType.OverideOne => HeadOverrideOne,
                AvatarAppearanceOverrideType.OverrideTwo => HeadOverrideTwo,
                AvatarAppearanceOverrideType.OverrideThree => HeadOverrideThree,
                AvatarAppearanceOverrideType.OverrideFour => HeadOverrideFour,
                AvatarAppearanceOverrideType.OverrideFive => HeadOverrideFive,
                _ => null,
            };
        }

        public GameObject GetTorsoOverrideGO(AvatarAppearanceOverrideType overrideType)
        {
            return overrideType switch
            {
                AvatarAppearanceOverrideType.None => null,
                AvatarAppearanceOverrideType.OverideOne => TorsoOverrideOne,
                AvatarAppearanceOverrideType.OverrideTwo => TorsoOverrideTwo,
                AvatarAppearanceOverrideType.OverrideThree => TorsoOverrideThree,
                AvatarAppearanceOverrideType.OverrideFour => TorsoOverrideFour,
                AvatarAppearanceOverrideType.OverrideFive => TorsoOverrideFive,
                _ => null,
            };
        }
        #endregion

        //TODO - need an API for changing overrrides 

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider = this;
                return;
            }
        }
    }
}

