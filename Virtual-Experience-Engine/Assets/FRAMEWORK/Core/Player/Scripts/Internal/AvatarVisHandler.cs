using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;

//Would be good if this was also used for the local avatar?
//Local player can be injected with GO list, and the selections config 
//Player service can be in charge of managing changes to these, calling a "InvokeOnChange" method 
//AvatarVishandler listens to that, and uses it to configure 

//Mmm, thing is, we don't want to deactivate and reactivate gameobjects when we don't need to be 
//Even the remote avatar needs to work out what changed, and call the method 
//Well, that bit can be done from inside RemotePlayerController

namespace VE2.Core.Player.Internal
{
    internal class PlayerGameObjectHandler
    {
        private readonly Transform _holderTransform;

        //private bool _isTransparent = false;

        private readonly List<GameObject> _builtInGameObjectPrefabs;
        private GameObject _activeBuiltInGameObject;
        private bool _isBuiltInGameObjectEnabled;
        private int _builtInGameObjectIndex;
        private Color _builtInColor;

        private readonly List<GameObject> _customGameObjectPrefabs;
        private GameObject _activeCustomGameObject;
        private bool _isCustomGameObjectEnabled;
        private int _customGameObjectIndex;

        public PlayerGameObjectHandler(Transform holderTransform,
            List<GameObject> builtInGameObjectPrefabs, bool isBuiltInGameObjectEnabled, ushort builtInGameObjectIndex, Color builtInColor,
            List<GameObject> customGameObjectPrefabs, bool isCustomGameObjectEnabled, ushort customGameObjectIndex)
        {
            _holderTransform = holderTransform;

            _builtInGameObjectPrefabs = builtInGameObjectPrefabs;
            SetBuiltInGameObjectEnabled(isBuiltInGameObjectEnabled);
            SetBuiltInGameObjectIndex(builtInGameObjectIndex);
            SetBuiltInColor(builtInColor);

            _customGameObjectPrefabs = customGameObjectPrefabs;
            SetCustomGameObjectEnabled(isCustomGameObjectEnabled);
            SetCustomGameObjectIndex(customGameObjectIndex);
        }

        public void SetBuiltInGameObjectEnabled(bool isEnabled)
        {
            if (_isBuiltInGameObjectEnabled == isEnabled)
                return;

            _isBuiltInGameObjectEnabled = isEnabled;

            GameObject.Destroy(_activeCustomGameObject);
            _activeCustomGameObject = GameObject.Instantiate(
                _customGameObjectPrefabs[_customGameObjectIndex],
                _holderTransform.position,
                _holderTransform.rotation,
                _holderTransform);
            SetBuiltInColor(_builtInColor);
        }

        internal void SetBuiltInGameObjectIndex(ushort index)
        {
            if (_builtInGameObjectIndex == index)
                return;

            GameObject.Destroy(_activeBuiltInGameObject);

            _builtInGameObjectIndex = index;

            _activeBuiltInGameObject = GameObject.Instantiate(
                _builtInGameObjectPrefabs[_builtInGameObjectIndex],
                _holderTransform.position,
                _holderTransform.rotation,
                _holderTransform);
            SetBuiltInColor(_builtInColor);
        }

        public void SetCustomGameObjectEnabled(bool isEnabled)
        {
            if (_isCustomGameObjectEnabled == isEnabled)
                return;

            _isCustomGameObjectEnabled = isEnabled;

            GameObject.Destroy(_activeCustomGameObject);
            _activeCustomGameObject = GameObject.Instantiate(
                _customGameObjectPrefabs[_customGameObjectIndex],
                _holderTransform.position,
                _holderTransform.rotation,
                _holderTransform);
        }

        public void SetCustomGameObjectIndex(ushort index)
        {
            if (_customGameObjectIndex == index)
                return;


            GameObject.Destroy(_activeCustomGameObject);

            _customGameObjectIndex = index;

            _activeCustomGameObject = GameObject.Instantiate(
                _customGameObjectPrefabs[_customGameObjectIndex],
                _holderTransform.position,
                _holderTransform.rotation,
                _holderTransform);
        }

        internal void SetBuiltInColor(Color color)
        {
            foreach (Material material in CommonUtils.GetAvatarColorMaterialsForGameObject(_activeBuiltInGameObject))
                material.color = color / 255f;

            _builtInColor = color;
        }

        // private Dictionary<(Renderer, int), (Material originalMaterial, Shader originalShader, Color originalColor)> originalMaterials = new();
        // private bool _hasBeenMadeTransparentBefore = false;

        // public void SetTransparent(bool isTransparent)
        // {
        //     foreach (Renderer renderer in _holderTransform.GetComponentsInChildren<Renderer>())
        //     {
        //         Material[] materials = renderer.materials;

        //         for (int i = 0; i < materials.Length; i++)
        //         {
        //             Material material = materials[i];
        //             var key = (renderer, i);

        //             if (isTransparent)
        //             {
        //                 // Save the original material's properties
        //                 originalMaterials[key] = (new Material(material), material.shader, material.color);

        //                 // Switch to transparent rendering
        //                 material.SetOverrideTag("RenderType", "Transparent");
        //                 material.SetInt("_Surface", 1); // Surface Type: Transparent
        //                 material.SetInt("_Blend", 2); // Blend Mode: Alpha
        //                 material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        //                 material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        //                 material.SetInt("_ZWrite", 0);
        //                 material.DisableKeyword("_ALPHATEST_ON");
        //                 material.EnableKeyword("_ALPHABLEND_ON");
        //                 material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        //                 material.renderQueue = 3000; // Transparent queue

        //                 // Reduce alpha to make it transparent
        //                 Color color = material.color;
        //                 color.a = 0.05f;
        //                 material.color = color;

        //                 _hasBeenMadeTransparentBefore = true;
        //             }
        //             else if (_hasBeenMadeTransparentBefore)
        //             {
        //                 // Restore original material properties if they were stored
        //                 if (originalMaterials.TryGetValue(key, out var originalData))
        //                 {
        //                     // Explicitly reassign the original material to the renderer at index `i`
        //                     materials[i] = originalData.originalMaterial;
        //                     renderer.materials = materials; // Reassign materials array to the renderer
        //                 }
        //             }
        //         }
        //     }

        //     if (!isTransparent)
        //     {
        //         originalMaterials.Clear(); // Clear stored original materials after switching back
        //     }
        // }
    }

    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class AvatarVisHandler : MonoBehaviour
    {
        [SerializeField] private Transform _headHolder;
        [SerializeField] private Transform _torsoHolder;
        [SerializeField] private Transform _handVRRightHolder;
        [SerializeField] private Transform _handVRLeftHolder;

        private PlayerGameObjectHandler _headHandler;
        private PlayerGameObjectHandler _torsoHandler;
        private PlayerGameObjectHandler _handVRRightHandler;
        private PlayerGameObjectHandler _handVRLeftHandler;

        private float _torsoYOffsetFromHead = -0.387f;

        private InstancedAvatarAppearance _currentRemoteAvatarAppearance;


        private void Awake()
        {
            //Assuming at this point the head and torso are a child of this GO
        }

        private void Update()
        {
            _torsoHolder.position = _headHolder.position + (_torsoYOffsetFromHead * Vector3.up);
        }

        /// <summary>
        /// Note, this WON'T set the initial appearance, HandleReceiveAvatarAppearance should be called after initialization
        /// </summary>
        public void Initialize(IPlayerServiceInternal playerServiceInternal, InstancedAvatarAppearance avatarAppearance)
        {
            //TODO - maybe these should come from PlayerService too?
            List<GameObject> builtInHeadGameObjectPrefabs = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_1"),
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_2"),
            };

            //This doesn't seem great to me... lot of chaining, and weird that BuiltInGameObject enabled comes from PlayerGameObjectSelections, not BuiltInPresentationConfig
            //it's because the plugin can decide if it wants to use the built-in head or not, so it needs to be set in the PlayerGameObjectSelections

            PlayerGameObjectSelection headSelection = avatarAppearance.PlayerGameObjectSelections._headGameObjectConfig;
            _headHandler = new PlayerGameObjectHandler(_headHolder,
                builtInHeadGameObjectPrefabs,
                headSelection.BuiltInGameObjectEnabled,
                avatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex,
                avatarAppearance.BuiltInPresentationConfig.AvatarColor,
                playerServiceInternal.HeadOverrideGOs,
                headSelection.CustomGameObjectEnabled,
                headSelection.CustomGameObjectIndex);

            List<GameObject> builtInTorsoGameObjectPrefabs = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Torsos/V_Avatar_Torso_Default_1"),
            };

            PlayerGameObjectSelection torsoSelection = avatarAppearance.PlayerGameObjectSelections._torsoGameObjectConfig;
            _torsoHandler = new PlayerGameObjectHandler(_torsoHolder,
                builtInTorsoGameObjectPrefabs,
                torsoSelection.BuiltInGameObjectEnabled,
                avatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex,
                avatarAppearance.BuiltInPresentationConfig.AvatarColor,
                playerServiceInternal.HeadOverrideGOs,
                torsoSelection.CustomGameObjectEnabled,
                torsoSelection.CustomGameObjectIndex);

            //TODO: left and right hands, need prefabbing too
        }

        public void UpdateInstacedAvatarAppearance(InstancedAvatarAppearance newAvatarAppearance)
        {
            PlayerGameObjectSelection headSelection = newAvatarAppearance.PlayerGameObjectSelections._headGameObjectConfig;

            _headHandler.SetBuiltInGameObjectEnabled(headSelection.BuiltInGameObjectEnabled);
            _headHandler.SetCustomGameObjectEnabled(headSelection.CustomGameObjectEnabled);
            _headHandler.SetBuiltInGameObjectIndex(newAvatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex);
            _headHandler.SetCustomGameObjectIndex(headSelection.CustomGameObjectIndex);


            _torsoHandler.SetBuiltInGameObjectEnabled(newAvatarAppearance.PlayerGameObjectSelections._torsoGameObjectConfig.BuiltInGameObjectEnabled);
            _torsoHandler.SetCustomGameObjectEnabled(newAvatarAppearance.PlayerGameObjectSelections._torsoGameObjectConfig.CustomGameObjectEnabled);
            _torsoHandler.SetBuiltInGameObjectIndex(newAvatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex);

            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.Equals(newAvatarAppearance))
                return;

            bool headChanged = SetHead(newAvatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex, newAvatarAppearance.OverrideHead, newAvatarAppearance.HeadOverrideIndex);
            bool torsoChanged = SetTorso(newAvatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex, newAvatarAppearance.OverrideTorso, newAvatarAppearance.TorsoOverrideIndex);

            if (headChanged || torsoChanged)
                RecaptureMaterials();

            foreach (Material material in _colorMaterials)
                material.color = new Color(newAvatarAppearance.BuiltInPresentationConfig.AvatarRed, newAvatarAppearance.BuiltInPresentationConfig.AvatarGreen, newAvatarAppearance.BuiltInPresentationConfig.AvatarBlue) / 255f;

            _currentRemoteAvatarAppearance = newAvatarAppearance;
        }


        // private bool TryGetOverrideGO(ushort overrideIndex, List<GameObject> overrideGameObjects, out GameObject overrideGO)
        // {
        //     if (overrideGameObjects.Count > overrideIndex && overrideGameObjects[overrideIndex] != null)
        //     {
        //         overrideGO = overrideGameObjects[overrideIndex];
        //         return true;
        //     }
        //     else
        //     {
        //         overrideGO = null;
        //         return false;
        //     }
        // }
    }
}
