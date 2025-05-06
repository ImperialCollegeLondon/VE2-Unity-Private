using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class AvatarVisHandler : MonoBehaviour
    {
        [SerializeField] private Transform _headHolder;
        [SerializeField] private Transform _torsoHolder;

        private List<Material> _colorMaterials = new();

        private float _torsoYOffsetFromHead = -0.387f;

        private OverridableAvatarAppearance _currentRemoteAvatarAppearance;
        private GameObject _activeHead;
        private GameObject _activeTorso;

        private List<GameObject> _virseAvatarHeadGameObjects;
        private List<GameObject> _virseAvatarTorsoGameObjects;
        private List<GameObject> _avatarHeadOverrideGameObjects;
        private List<GameObject> _avatarTorsoOverrideGameObjects;

        private void Awake()
        {
            //Assuming at this point the head and torso are a child of this GO
            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(gameObject);

            _activeHead = _headHolder.transform.GetChild(0).gameObject;
            _activeTorso = _torsoHolder.transform.GetChild(0).gameObject;

            RecaptureMaterials();
        }

        private void RecaptureMaterials()
        {
            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(gameObject);
        }

        private void Update()
        {
            _torsoHolder.position = _headHolder.position + (_torsoYOffsetFromHead * Vector3.up);
        }  

        /// <summary>
        /// Note, this WON'T set the initial appearance, HandleReceiveAvatarAppearance should be called after initialization
        /// </summary>
        public void Initialize(IPlayerServiceInternal playerServiceInternal)
        {
            //TODO - maybe these should come from PlayerService too?
            _virseAvatarHeadGameObjects = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_1"),
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_2"),
            };

            _virseAvatarTorsoGameObjects = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Torsos/V_Avatar_Torso_Default_1"),
            };

            _avatarHeadOverrideGameObjects = playerServiceInternal.HeadOverrideGOs;
            _avatarTorsoOverrideGameObjects = playerServiceInternal.HeadOverrideGOs;
        }

        public void HandleReceiveAvatarAppearance(OverridableAvatarAppearance newAvatarAppearance)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.Equals(newAvatarAppearance))
                return;

            bool headChanged = SetHead(newAvatarAppearance.PresentationConfig.AvatarHeadType, newAvatarAppearance.OverrideHead, newAvatarAppearance.HeadOverrideIndex);
            bool torsoChanged = SetTorso(newAvatarAppearance.PresentationConfig.AvatarTorsoType, newAvatarAppearance.OverrideTorso, newAvatarAppearance.TorsoOverrideIndex);

            if (headChanged || torsoChanged)
                RecaptureMaterials();

            foreach (Material material in _colorMaterials)
                material.color = new Color(newAvatarAppearance.PresentationConfig.AvatarRed, newAvatarAppearance.PresentationConfig.AvatarGreen, newAvatarAppearance.PresentationConfig.AvatarBlue) / 255f;

            _currentRemoteAvatarAppearance = newAvatarAppearance;
        }

        private Dictionary<(Renderer, int), (Material originalMaterial, Shader originalShader, Color originalColor)> originalMaterials = new();
        private bool _hasBeenMadeTransparentBefore = false;

        public void SetTransparent(bool isTransparent)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.materials;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    var key = (renderer, i);

                    if (isTransparent)
                    {
                        // Save the original material's properties
                        originalMaterials[key] = (new Material(material), material.shader, material.color);
                        
                        // Switch to transparent rendering
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt("_Surface", 1); // Surface Type: Transparent
                        material.SetInt("_Blend", 2); // Blend Mode: Alpha
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000; // Transparent queue

                        // Reduce alpha to make it transparent
                        Color color = material.color;
                        color.a = 0.05f;
                        material.color = color;
                        
                        _hasBeenMadeTransparentBefore = true;
                    }
                    else if (_hasBeenMadeTransparentBefore)
                    {
                        // Restore original material properties if they were stored
                        if (originalMaterials.TryGetValue(key, out var originalData))
                        {
                            // Explicitly reassign the original material to the renderer at index `i`
                            materials[i] = originalData.originalMaterial;
                            renderer.materials = materials; // Reassign materials array to the renderer
                        }
                    }
                }
            }

            if (!isTransparent)
            {
                originalMaterials.Clear(); // Clear stored original materials after switching back
            }
        }

        private bool SetHead(VE2AvatarHeadAppearanceType avatarHeadType, bool overrideHead, ushort headOverrideIndex)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.PresentationConfig.AvatarHeadType == avatarHeadType && 
                _currentRemoteAvatarAppearance.HeadOverrideIndex == headOverrideIndex && _currentRemoteAvatarAppearance.OverrideHead == overrideHead)
                return false;

            GameObject newHead;
            if (overrideHead && TryGetOverrideGO(headOverrideIndex, _avatarHeadOverrideGameObjects, out GameObject headOverrideGO))
            {
                newHead = headOverrideGO;
            }
            else
            {
                newHead = _virseAvatarHeadGameObjects[(int)avatarHeadType];
            }

            if (_activeHead != null)
                GameObject.Destroy(_activeHead);

            _activeHead = GameObject.Instantiate(newHead, _headHolder.transform.position, _headHolder.transform.rotation, _headHolder);

            return true;
        }

        private bool SetTorso(VE2AvatarTorsoAppearanceType avatarTorsoType, bool overrideTorso, ushort torsoOverrideIndex)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.PresentationConfig.AvatarTorsoType == avatarTorsoType && 
                _currentRemoteAvatarAppearance.TorsoOverrideIndex == torsoOverrideIndex && _currentRemoteAvatarAppearance.OverrideTorso == overrideTorso)
                return false;

            GameObject newTorso;
            if (overrideTorso && TryGetOverrideGO(torsoOverrideIndex, _avatarTorsoOverrideGameObjects, out GameObject torsoOverrideGO))
                newTorso = torsoOverrideGO;
            else
                newTorso = _virseAvatarTorsoGameObjects[(int)avatarTorsoType];

            if (_activeTorso != null)
                GameObject.Destroy(_activeTorso);

            _activeTorso = GameObject.Instantiate(newTorso, _torsoHolder.transform.position, _torsoHolder.transform.rotation, _torsoHolder);

            return true;
        }

        private bool TryGetOverrideGO(ushort overrideIndex, List<GameObject> overrideGameObjects, out GameObject overrideGO)
        {
            if (overrideGameObjects.Count > overrideIndex && overrideGameObjects[overrideIndex] != null)
            {
                overrideGO = overrideGameObjects[overrideIndex];
                return true;
            }
            else
            {
                overrideGO = null;
                return false;
            }
        }
    }
}
