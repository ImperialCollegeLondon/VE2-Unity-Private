using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    //TODO - refactor this to be reusable between built in and custom, should just deal with one or the other
    internal class PlayerAvatarGameObjectHandler //: IPlayerGameObjectHandler
    {
        private readonly Transform _holderTransform;

        //private bool _isTransparent = false;

        private readonly List<GameObject> _builtInGameObjectPrefabs;
        private GameObject _activeBuiltInGameObject;
        private bool _builtInGameObjectEnabled = false;
        private int _builtInGameObjectIndex = 0;
        private Color _builtInColor;

        private readonly List<GameObject> _customGameObjectPrefabs;
        private GameObject _activeCustomGameObject;
        private bool _customGameObjectEnabled = false;
        private int _customGameObjectIndex = 0;

        private readonly int _layerIndex;
        private readonly ushort _clientID;
        private readonly bool _prefabNeedsMirroring;

        public PlayerAvatarGameObjectHandler(Transform holderTransform,
            List<GameObject> builtInGameObjectPrefabs, ushort builtInGameObjectIndex, Color builtInColor,
            List<GameObject> customGameObjectPrefabs, AvatarGameObjectSelection gameObjectSelection, int layerIndex, ushort clientID, bool prefabNeedsMirroring = false)
        {
            _holderTransform = holderTransform;
            _layerIndex = layerIndex;
            _clientID = clientID;
            _prefabNeedsMirroring = prefabNeedsMirroring;

            _builtInGameObjectPrefabs = builtInGameObjectPrefabs;
            SetBuiltInGameObjectEnabled(gameObjectSelection.BuiltInGameObjectEnabled);
            SetBuiltInGameObjectIndex(builtInGameObjectIndex);
            SetBuiltInColor(builtInColor);

            _customGameObjectPrefabs = customGameObjectPrefabs;
            SetCustomGameObjectEnabled(gameObjectSelection.CustomGameObjectEnabled);
            SetCustomGameObjectIndex(gameObjectSelection.CustomGameObjectIndex);
        }

        public void SetGameObjectSelections(AvatarGameObjectSelection gameObjectSelection)
        {
            SetBuiltInGameObjectEnabled(gameObjectSelection.BuiltInGameObjectEnabled);
            SetCustomGameObjectEnabled(gameObjectSelection.CustomGameObjectEnabled);
            SetCustomGameObjectIndex(gameObjectSelection.CustomGameObjectIndex);
        }

        public void SetBuiltInGameObjectEnabled(bool newIsEnabled)
        {
            if (!newIsEnabled && _builtInGameObjectEnabled)
            {
                GameObject.Destroy(_activeBuiltInGameObject);
                _activeBuiltInGameObject = null;
            }
            else if (newIsEnabled && !_builtInGameObjectEnabled)
            {
                try
                {
                    _activeBuiltInGameObject = InstantiatePrefabAfterRenaming(
                        _builtInGameObjectPrefabs[_builtInGameObjectIndex],
                        _holderTransform.position,
                        _holderTransform.rotation,
                        _holderTransform);
                    SetBuiltInColor(_builtInColor);
                }
                catch (Exception ex)
                {
                    //Debug.LogError("ERROR: " + ex.StackTrace);
                    Debug.LogError("Error - index " + _builtInGameObjectIndex + " there are only " + _builtInGameObjectPrefabs.Count + " prefabs available. holder null? " + (_holderTransform == null) + " Exception: " + ex.Message);
                    foreach (GameObject prefab in _builtInGameObjectPrefabs)
                    {
                        Debug.LogError("prefab null? " + (prefab == null));
                        Debug.LogError("prefab name: " + prefab.name);
                    }
                    return;
                }
            }

            _builtInGameObjectEnabled = newIsEnabled;
        }

        internal void SetBuiltInGameObjectIndex(ushort index)
        {
            if (_builtInGameObjectIndex == index)
                return;

            _builtInGameObjectIndex = index;

            if (!_builtInGameObjectEnabled)
                return;

            GameObject.Destroy(_activeBuiltInGameObject);

            _activeBuiltInGameObject = InstantiatePrefabAfterRenaming(
                _builtInGameObjectPrefabs[_builtInGameObjectIndex],
                _holderTransform.position,
                _holderTransform.rotation,
                _holderTransform);
            SetBuiltInColor(_builtInColor);
        }

        public void SetCustomGameObjectEnabled(bool newIsEnabled)
        {
            if (!newIsEnabled && _customGameObjectEnabled)
            {
                GameObject.Destroy(_activeCustomGameObject);
                _activeCustomGameObject = null;
            }
            else if (newIsEnabled && !_customGameObjectEnabled!)
            {
                _activeCustomGameObject = InstantiatePrefabAfterRenaming(
                    _customGameObjectPrefabs[_customGameObjectIndex],
                    _holderTransform.position,
                    _holderTransform.rotation,
                    _holderTransform);
            }

            _customGameObjectEnabled = newIsEnabled;
        }

        public void SetCustomGameObjectIndex(ushort newIndex)
        {
            if (_customGameObjectIndex == newIndex)
                return;

            _customGameObjectIndex = newIndex;

            if (!_customGameObjectEnabled)
                return;

            GameObject.Destroy(_activeCustomGameObject);

            _activeCustomGameObject = InstantiatePrefabAfterRenaming(
                _customGameObjectPrefabs[_customGameObjectIndex],
                _holderTransform.position,
                _holderTransform.rotation,
                _holderTransform);
        }

        internal void SetBuiltInColor(Color color)
        {
            if (_activeBuiltInGameObject == null)
            {
                return;
            }

            foreach (Material material in CommonUtils.GetAvatarColorMaterialsForGameObject(_activeBuiltInGameObject))
                material.color = color;

            _builtInColor = color;
        }

        private GameObject InstantiatePrefabAfterRenaming(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform)
        {
            GameObject temp = new GameObject("temp");
            temp.SetActive(false);
            GameObject newGO = GameObject.Instantiate(prefab, position, rotation, temp.transform);
            SetGameObjectLayerAndName(newGO);

            if (_prefabNeedsMirroring)
                newGO.transform.localScale = new Vector3(-1, 1, 1);

            newGO.transform.SetParent(parentTransform);

            if (Application.isPlaying)
                GameObject.Destroy(temp);
            else
                GameObject.DestroyImmediate(temp);

            return newGO;
        }

        internal void SetGameObjectLayerAndName(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError("SetGameObjectLayer: GameObject is null.");
                return;
            }

            gameObject.layer = _layerIndex;
            gameObject.name += $"_{_clientID}";

            foreach (Transform child in gameObject.transform.GetComponentInChildren<Transform>())
            {
                if (child.gameObject == gameObject) // Skip the parent gameObject itself
                    continue;

                // Recursively set the layer for all child GameObjects
                SetGameObjectLayerAndName(child.gameObject);
            }
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
        public AvatarPrefabs PlayerBuiltInGameObjectPrefabs;
        public AvatarPrefabs PlayerCustomGameObjectPrefabs;
        public InstancedAvatarAppearance CurrentInstancedAvatarAppearance;

        public AvatarHandlerBuilderContext(AvatarPrefabs playerBuiltInGameObjectPrefabs, AvatarPrefabs playerCustomGameObjectPrefabs, InstancedAvatarAppearance currentInstancedAvatarAppearance)
        {
            PlayerBuiltInGameObjectPrefabs = playerBuiltInGameObjectPrefabs;
            PlayerCustomGameObjectPrefabs = playerCustomGameObjectPrefabs;
            CurrentInstancedAvatarAppearance = currentInstancedAvatarAppearance;
        }
    }

    //[AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class PlayerAvatarHandler //: MonoBehaviour//, IPlayerGameObjectsHandler
    {
        public bool IsEnabled = false;
        public PlayerAvatarGameObjectHandler HeadHandler;
        public PlayerAvatarGameObjectHandler TorsoHandler;
        public PlayerAvatarGameObjectHandler HandVRRightHandler;
        public PlayerAvatarGameObjectHandler HandVRLeftHandler;

        private float _torsoYOffsetFromHead = -0.42f;
        private InstancedAvatarAppearance _avatarAppearance;
        private ushort _clientID;

        private readonly AvatarPrefabs _builtInGameObjectPrefabs;
        private readonly AvatarPrefabs _playerGameObjectPrefabs;
        private readonly bool _isLocalPlayer;
        private readonly Transform _headHolder;
        private readonly Transform _torsoHolder;
        private readonly Transform _handVRRightHolder;
        private readonly Transform _handVRLeftHolder;

        /// <summary>
        /// Note, Enable must be called after this constructor to actually activate the avatar.
        /// </summary>
        public PlayerAvatarHandler(AvatarPrefabs BuiltInGameObjectPrefabs, AvatarPrefabs playerGameObjectPrefabs, InstancedAvatarAppearance avatarAppearance, bool isLocalPlayer,
            Transform headHolder, Transform torsoHolder, Transform handVRRightHolder = null, Transform handVRLeftHolder = null)
        {
            _builtInGameObjectPrefabs = BuiltInGameObjectPrefabs;
            _playerGameObjectPrefabs = playerGameObjectPrefabs;
            _isLocalPlayer = isLocalPlayer;
            _avatarAppearance = avatarAppearance;
            _headHolder = headHolder;
            _torsoHolder = torsoHolder;
            _handVRRightHolder = handVRRightHolder;
            _handVRLeftHolder = handVRLeftHolder;
        }

        public void Enable(ushort id)
        {
            IsEnabled = true;
            _clientID = id;

            Color avatarColor = _avatarAppearance.BuiltInPresentationConfig.AvatarColor;

            int headAndTorsoLayer = !_isLocalPlayer ? CommonUtils.RemotePlayerLayer : CommonUtils.PlayerInvisibleLayer;

            HeadHandler = new PlayerAvatarGameObjectHandler(_headHolder,
                _builtInGameObjectPrefabs.Heads,
                _avatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex,
                avatarColor,
                _playerGameObjectPrefabs.Heads,
                _avatarAppearance.PlayerGameObjectSelections.HeadGameObjectSelection, headAndTorsoLayer, _clientID);

            TorsoHandler = new PlayerAvatarGameObjectHandler(_torsoHolder,
                _builtInGameObjectPrefabs.Torsos,
                _avatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex,
                avatarColor,
                _playerGameObjectPrefabs.Torsos,
                _avatarAppearance.PlayerGameObjectSelections.TorsoGameObjectSelection, headAndTorsoLayer, _clientID);

            int handLayer = !_isLocalPlayer ? CommonUtils.RemotePlayerLayer : CommonUtils.PlayerVisibleLayer;

            if (_handVRRightHolder == null || _handVRLeftHolder == null)
                return;

            HandVRRightHandler = new PlayerAvatarGameObjectHandler(_handVRRightHolder,
                _builtInGameObjectPrefabs.VRHands,
                0,
                avatarColor,
                _playerGameObjectPrefabs.VRHands,
                _avatarAppearance.PlayerGameObjectSelections.RightHandVRGameObjectSelection, handLayer, _clientID, true);

            HandVRLeftHandler = new PlayerAvatarGameObjectHandler(_handVRLeftHolder,
                _builtInGameObjectPrefabs.VRHands,
                0,
                avatarColor,
                _playerGameObjectPrefabs.VRHands,
                _avatarAppearance.PlayerGameObjectSelections.LeftHandVRGameObjectSelection, handLayer, _clientID);
        }

        public void UpdateInstancedAvatarAppearance(InstancedAvatarAppearance newAvatarAppearance)
        {
            _avatarAppearance = newAvatarAppearance;

            HeadHandler?.SetGameObjectSelections(newAvatarAppearance.PlayerGameObjectSelections.HeadGameObjectSelection);
            HeadHandler?.SetBuiltInGameObjectIndex(newAvatarAppearance.BuiltInPresentationConfig.AvatarHeadIndex);
            HeadHandler?.SetBuiltInColor(newAvatarAppearance.BuiltInPresentationConfig.AvatarColor);

            TorsoHandler?.SetGameObjectSelections(newAvatarAppearance.PlayerGameObjectSelections.TorsoGameObjectSelection);
            TorsoHandler?.SetBuiltInGameObjectIndex(newAvatarAppearance.BuiltInPresentationConfig.AvatarTorsoIndex);
            TorsoHandler?.SetBuiltInColor(newAvatarAppearance.BuiltInPresentationConfig.AvatarColor);

            //Note, 2d avatars wont have hands, so will not execute the rest. 
            //This could also all be null if the avatar hasn't been enabled yet

            HandVRRightHandler?.SetGameObjectSelections(newAvatarAppearance.PlayerGameObjectSelections.RightHandVRGameObjectSelection);
            HandVRRightHandler?.SetBuiltInGameObjectIndex(0);
            HandVRRightHandler?.SetBuiltInColor(newAvatarAppearance.BuiltInPresentationConfig.AvatarColor);

            HandVRLeftHandler?.SetGameObjectSelections(newAvatarAppearance.PlayerGameObjectSelections.LeftHandVRGameObjectSelection);
            HandVRLeftHandler?.SetBuiltInGameObjectIndex(0);
            HandVRLeftHandler?.SetBuiltInColor(newAvatarAppearance.BuiltInPresentationConfig.AvatarColor);
        }

        public void HandleUpdate() => _torsoHolder.position = _headHolder.position + (_torsoYOffsetFromHead * Vector3.up);
    }
}

