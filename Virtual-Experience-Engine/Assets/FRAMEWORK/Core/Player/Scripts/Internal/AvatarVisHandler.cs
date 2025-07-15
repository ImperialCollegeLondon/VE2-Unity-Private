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
    //TODO - refactor this to be reusable between built in and custom, should just deal with one or the other
    internal class PlayerGameObjectHandler : IPlayerGameObjectHandler
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
            List<GameObject> builtInGameObjectPrefabs, ushort builtInGameObjectIndex, Color builtInColor,
            List<GameObject> customGameObjectPrefabs, PlayerGameObjectSelection gameObjectSelection)
        {
            _holderTransform = holderTransform;

            _builtInGameObjectPrefabs = builtInGameObjectPrefabs;
            SetBuiltInGameObjectEnabled(gameObjectSelection.BuiltInGameObjectEnabled);
            SetBuiltInGameObjectIndex(builtInGameObjectIndex);
            SetBuiltInColor(builtInColor);

            _customGameObjectPrefabs = customGameObjectPrefabs;
            SetCustomGameObjectEnabled(gameObjectSelection.CustomGameObjectEnabled);
            SetCustomGameObjectIndex(gameObjectSelection.CustomGameObjectIndex);
        }

        public void SetGameObjectSelections(PlayerGameObjectSelection gameObjectSelection)
        {
            SetBuiltInGameObjectEnabled(gameObjectSelection.BuiltInGameObjectEnabled);
            SetCustomGameObjectEnabled(gameObjectSelection.CustomGameObjectEnabled);
            SetCustomGameObjectIndex(gameObjectSelection.CustomGameObjectIndex);
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

    // internal class AvatarHandlerContextSub
    // {

    // }

    // internal class AvatarHandlerContext
    // {
    //     public List<GameObject> BuiltInHeadPrefabs { get; set; }
    //     public List<GameObject> BuiltInTorsoPrefabs { get; set; }
    //     public List<GameObject> CustomHeadPrefabs { get; set; }
    //     public List<GameObject> CustomTorsoPrefabs { get; set; }
    //     public List<GameObject> CustomVRRightHands { get; set; }
    //     public List<GameObject> CustomVRLeftHands { get; set; }
    // }

    internal class AvatarHandlerBuilderContext
    {
        public PlayerGameObjectPrefabs PlayerBuiltInGameObjectPrefabs;
        public PlayerGameObjectPrefabs PlayerCustomGameObjectPrefabs;
        public InstancedAvatarAppearance CurrentInstancedAvatarAppearance;

        public AvatarHandlerBuilderContext(PlayerGameObjectPrefabs playerBuiltInGameObjectPrefabs, PlayerGameObjectPrefabs playerCustomGameObjectPrefabs, InstancedAvatarAppearance currentInstancedAvatarAppearance)
        {
            PlayerBuiltInGameObjectPrefabs = playerBuiltInGameObjectPrefabs;
            PlayerCustomGameObjectPrefabs = playerCustomGameObjectPrefabs;
            CurrentInstancedAvatarAppearance = currentInstancedAvatarAppearance;
        }
    }

    //[AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class PlayerGameObjectsHandler //: MonoBehaviour//, IPlayerGameObjectsHandler
    {
        private readonly Transform _headHolder;
        private readonly Transform _torsoHolder;
        private readonly Transform _handVRRightHolder;
        private readonly Transform _handVRLeftHolder;

        public readonly PlayerGameObjectHandler HeadHandler;
        public readonly PlayerGameObjectHandler TorsoHandler;
        public readonly PlayerGameObjectHandler HandVRRightHandler;
        public readonly PlayerGameObjectHandler HandVRLeftHandler;

        private float _torsoYOffsetFromHead = -0.387f;

        public void HandleUpdate()
        {
            _torsoHolder.position = _headHolder.position + (_torsoYOffsetFromHead * Vector3.up);
        }

        //We don't really want to have to pipe the prefab lists all the way down 
        //What if we created this as empty, just with the prefabs, and we give it the config in some setup method
        //That healps cut down the size of the constructor I guess, and removes repeated set appearance code in setup

        /// <summary>
        /// Note, this WON'T set the initial appearance, HandleReceiveAvatarAppearance should be called after initialization
        /// </summary>
        public PlayerGameObjectsHandler(PlayerGameObjectPrefabs BuiltInGameObjectPrefabs, PlayerGameObjectPrefabs playerGameObjectPrefabs, InstancedAvatarAppearance avatarAppearance,
            Transform headHolder, Transform torsoHolder, Transform handVRRightHolder = null, Transform handVRLeftHolder = null)
        {
            _headHolder = headHolder;
            _torsoHolder = torsoHolder;
            _handVRRightHolder = handVRRightHolder;
            _handVRLeftHolder = handVRLeftHolder;

            Color avatarColor = avatarAppearance.BuiltInPresentationConfig.AvatarColor;

            HeadHandler = new PlayerGameObjectHandler(_headHolder,
                BuiltInGameObjectPrefabs.Heads,
                avatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex,
                avatarColor,
                playerGameObjectPrefabs.Heads,
                avatarAppearance.PlayerGameObjectSelections.HeadGameObjectConfig);

            TorsoHandler = new PlayerGameObjectHandler(_torsoHolder,
                BuiltInGameObjectPrefabs.Torsos,
                avatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex,
                avatarColor,
                playerGameObjectPrefabs.Torsos,
                avatarAppearance.PlayerGameObjectSelections.TorsoGameObjectConfig);

            //TODO: left and right hands, need prefabbing too
        }

        public void UpdateInstacedAvatarAppearance(InstancedAvatarAppearance newAvatarAppearance)
        {
            HeadHandler.SetGameObjectSelections(newAvatarAppearance.PlayerGameObjectSelections.HeadGameObjectConfig);
            HeadHandler.SetBuiltInGameObjectIndex(newAvatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex);
            HeadHandler.SetBuiltInColor(newAvatarAppearance.BuiltInPresentationConfig.AvatarColor);

            TorsoHandler.SetGameObjectSelections(newAvatarAppearance.PlayerGameObjectSelections.TorsoGameObjectConfig);
            TorsoHandler.SetBuiltInGameObjectIndex(newAvatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex);
            TorsoHandler.SetBuiltInColor(newAvatarAppearance.BuiltInPresentationConfig.AvatarColor);

            //_currentRemoteAvatarAppearance = newAvatarAppearance;
        }
    }
}

//What if we remove this layer and just have the player managing each sub peice directly 
//The player needs all the interfaces on it anyway
//Nah, this way works fine
