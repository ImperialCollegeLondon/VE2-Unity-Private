using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VIRSE.Common;
using static ViRSE.Common.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    public class RemoteAvatarController : MonoBehaviour
    {
        [SerializeField] private Transform _headHolder;
        [SerializeField] private Transform _torsoHolder;
        private float _torsoOffsetFromHead;

        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Transform _namePlateTransform;
        [SerializeField] private GameObject _interactor2DGameObject;
        [SerializeField] private GameObject _interactorVRLeftGameObject;
        [SerializeField] private GameObject _interactorVRRightGameObject;

        private List<Material> _colorMaterials = new();

        private ViRSEAvatarAppearance _currentRemoteAvatarAppearance;
        private GameObject _activeHead;
        private GameObject _activeTorso;

        private List<GameObject> _virseAvatarHeadGameObjects;
        private List<GameObject> _virseAvatarTorsoGameObjects;
        private List<GameObject> _avatarHeadOverrideGameObjects;
        private List<GameObject> _avatarTorsoOverrideGameObjects;

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

        /// <summary>
        /// Note, this WON'T set the initial appearance, HandleReceiveAvatarAppearance should be called after initialization
        /// </summary>
        public void Initialize(List<GameObject> virseAvatarHeadGameObjects, List<GameObject> virseAvatarTorsoGameObjects, List<GameObject> avatarHeadOverrideGameObjects, List<GameObject> avatarTorsoOverrideGameObjects)
        {
            _virseAvatarHeadGameObjects = virseAvatarHeadGameObjects;
            _virseAvatarTorsoGameObjects = virseAvatarTorsoGameObjects;
            _avatarHeadOverrideGameObjects = avatarHeadOverrideGameObjects;
            _avatarTorsoOverrideGameObjects = avatarTorsoOverrideGameObjects;
        }

        public void HandleReceiveRemotePlayerState(PlayerTransformData playerState)
        {
            transform.SetPositionAndRotation(playerState.RootPosition, playerState.RootRotation);
            _headHolder.SetLocalPositionAndRotation(playerState.HeadLocalPosition, playerState.HeadLocalRotation);
            _torsoHolder.position = _headHolder.position + (_torsoOffsetFromHead * Vector3.up);

            _interactorVRLeftGameObject.SetActive(playerState.IsVRMode);
            _interactorVRRightGameObject.SetActive(playerState.IsVRMode);
            _interactor2DGameObject.SetActive(!playerState.IsVRMode);

            if (playerState.IsVRMode)
            {
                _interactorVRLeftGameObject.transform.SetLocalPositionAndRotation(playerState.HandVRLeftLocalPosition, playerState.HandVRLeftLocalRotation);
                _interactorVRRightGameObject.transform.SetLocalPositionAndRotation(playerState.HandVRRightLocalPosition, playerState.HandVRRightLocalRotation);
            }
            else 
            {
                _interactor2DGameObject.transform.SetLocalPositionAndRotation(playerState.Hand2DLocalPosition, playerState.Hand2DLocalRotation);
            }
        }

        public void HandleReceiveAvatarAppearance(ViRSEAvatarAppearance newAvatarAppearance)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.Equals(newAvatarAppearance))
                return;

            _playerNameText.text = newAvatarAppearance.PresentationConfig.PlayerName;

            bool headChanged = SetHead(newAvatarAppearance.PresentationConfig.AvatarHeadType, newAvatarAppearance.HeadOverrideType);
            bool torsoChanged = SetTorso(newAvatarAppearance.PresentationConfig.AvatarTorsoType, newAvatarAppearance.TorsoOverrideType);

            if (headChanged || torsoChanged)
                RefreshMaterials();

            foreach (Material material in _colorMaterials)
                material.color = new Color(newAvatarAppearance.PresentationConfig.AvatarRed, newAvatarAppearance.PresentationConfig.AvatarGreen, newAvatarAppearance.PresentationConfig.AvatarBlue) / 255f;

            _currentRemoteAvatarAppearance = newAvatarAppearance;
        }


        private bool SetHead(ViRSEAvatarHeadAppearanceType avatarHeadType, AvatarAppearanceOverrideType headOverrideType)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.PresentationConfig.AvatarHeadType == avatarHeadType && _currentRemoteAvatarAppearance.HeadOverrideType == headOverrideType)
                return false;

            GameObject newHead;
            if (headOverrideType != AvatarAppearanceOverrideType.None && TryGetOverrideGO(headOverrideType, _avatarHeadOverrideGameObjects, out GameObject headOverrideGO))
                newHead = headOverrideGO;
            else
                newHead = _virseAvatarHeadGameObjects[(int)avatarHeadType];

            if (_activeHead != null)
                GameObject.Destroy(_activeHead);

            _activeHead = GameObject.Instantiate(newHead, _headHolder.transform.position, _headHolder.transform.rotation, _headHolder);

            return true;
        }

        private bool SetTorso(ViRSEAvatarTorsoAppearanceType avatarTorsoType, AvatarAppearanceOverrideType torsoOverrideType)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.PresentationConfig.AvatarTorsoType == avatarTorsoType && _currentRemoteAvatarAppearance.TorsoOverrideType == torsoOverrideType)
                return false;

            GameObject newTorso;
            if (torsoOverrideType != AvatarAppearanceOverrideType.None && TryGetOverrideGO(torsoOverrideType, _avatarTorsoOverrideGameObjects, out GameObject torsoOverrideGO))
                newTorso = torsoOverrideGO;
            else
                newTorso = _virseAvatarTorsoGameObjects[(int)avatarTorsoType];

            if (_activeTorso != null)
                GameObject.Destroy(_activeTorso);

            _activeTorso = GameObject.Instantiate(newTorso, _torsoHolder.transform.position, _torsoHolder.transform.rotation, _torsoHolder);

            return true;
        }

        private bool TryGetOverrideGO(AvatarAppearanceOverrideType overrideType, List<GameObject> overrideGameObjects, out GameObject overrideGO)
        {
            int index = (int)overrideType - 1; //-1 as 0 is "no override"
            if (overrideGameObjects.Count > index && overrideGameObjects[index] != null)
            {
                overrideGO = overrideGameObjects[index];
                return true;
            }
            else 
            {
                overrideGO = null;
                return false;
            }

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
