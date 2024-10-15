using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ViRSE.Core;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    public class RemoteAvatarController : MonoBehaviour
    {
        [SerializeField] private Transform _headHolder;
        [SerializeField] private Transform _torsoHolder;
        private float _torsoOffsetFromHead;

        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Transform _namePlateTransform;

        private List<Material> _colorMaterials = new();

        private GameObject _activeHead;
        private GameObject _activeTorso;

        private ViRSEAvatarAppearanceWrapper _avatarAppearance;
        private IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;
        private List<GameObject> _virseAvatarHeadGameObjects;
        private List<GameObject> _virseAvatarTorsoGameObjects;

        private void Awake() 
        {
            _torsoOffsetFromHead = _torsoHolder.position.y - _headHolder.position.y;
            _activeHead = _headHolder.transform.GetChild(0).gameObject;
            _activeTorso = _torsoHolder.transform.GetChild(0).gameObject;   

            RefreshMaterials();
        }

        private void RefreshMaterials() 
        {
            _colorMaterials.Clear();

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].name.Contains("V_AvatarPrimary"))
                        _colorMaterials.Add(renderer.materials[i]);
                }
            }
        }

        public void Initialize(IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, List<GameObject> virseAvatarHeadGameObjects, List<GameObject> virseAvatarTorsoGameObjects)
        {
            _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;
            _virseAvatarHeadGameObjects = virseAvatarHeadGameObjects;
            _virseAvatarTorsoGameObjects = virseAvatarTorsoGameObjects;
        }

        public void HandleReceiveRemotePlayerState(PlayerState playerState)
        {
            transform.position = playerState.RootPosition;
            transform.rotation = playerState.RootRotation;

            _headHolder.position = playerState.HeadPosition;
            _headHolder.rotation = playerState.HeadRotation;

            _torsoHolder.position = playerState.HeadPosition + (_torsoOffsetFromHead * Vector3.up);
        }

        public void HandleReceiveAvatarAppearance(ViRSEAvatarAppearanceWrapper newAvatarAppearance)
        {
            if (_avatarAppearance != null && _avatarAppearance.Equals(newAvatarAppearance))
                return;

            _playerNameText.text = newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.PlayerName;

            GameObject avatarHead = null;
            if (newAvatarAppearance.ViRSEAvatarAppearance.HeadOverrideType != AvatarAppearanceOverrideType.None) 
                avatarHead = _playerAppearanceOverridesProvider.GetHeadOverrideGO(newAvatarAppearance.ViRSEAvatarAppearance.HeadOverrideType);
            if (avatarHead == null) //No override, or gameobject not found
            {
                Debug.Log($"Avatar head type: {newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.AvatarHeadType} heads found? {_virseAvatarHeadGameObjects.Count}");
                avatarHead = _virseAvatarHeadGameObjects[(int)newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.AvatarHeadType];
            }
            bool headChanged = SetHeadGameObject(avatarHead);

            GameObject avatarTorso = null;
            if (newAvatarAppearance.ViRSEAvatarAppearance.TorsoOverrideType != AvatarAppearanceOverrideType.None)
                avatarTorso = _playerAppearanceOverridesProvider.GetTorsoOverrideGO(newAvatarAppearance.ViRSEAvatarAppearance.TorsoOverrideType);
            if (avatarTorso == null) //No override, or gameobject not found
                avatarTorso = _virseAvatarTorsoGameObjects[(int)newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.AvatarTorsoType];
            bool torsoChanged = SetTorsoGameObject(avatarTorso);

            if (headChanged || torsoChanged)
                RefreshMaterials();

            foreach (Material material in _colorMaterials)
                material.color = new Color(newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.AvatarRed, newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.AvatarGreen, newAvatarAppearance.ViRSEAvatarAppearance.PresentationConfig.AvatarBlue) / 255f;

            _avatarAppearance = newAvatarAppearance;
        }

        private bool SetHeadGameObject(GameObject newHead) 
        {
            if (newHead.name.Equals(_activeHead.name))
                return false; 

            GameObject.Destroy(_activeHead);
            _activeHead = GameObject.Instantiate(newHead, _headHolder.transform.position, _headHolder.transform.rotation, _headHolder);

            return true;
        }

        private bool SetTorsoGameObject(GameObject newTorso)
        {
            if (newTorso.name.Equals(_activeTorso.name))
                return false;

            GameObject.Destroy(_activeTorso);
            _activeTorso = GameObject.Instantiate(newTorso, _torsoHolder.transform.position, _torsoHolder.transform.rotation, _torsoHolder);

            return true;
        }

        private void Update() 
        {
            if (Camera.main == null)
                return;

            Vector3 dirToCamera = Camera.main.transform.position - _namePlateTransform.position;
            Vector3 lookPosition = _namePlateTransform.position - dirToCamera;
            _namePlateTransform.LookAt(lookPosition);
        }

        private void OnDisable() 
        {
            //Destroy GO for domain reload
            if (gameObject != null)
                Destroy(gameObject);
        }
    }
}
