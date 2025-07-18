using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.Player.API;
using VE2.Core.Player.Internal;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class RemoteAvatarController : MonoBehaviour
    {
        [SerializeField] private Transform _headHolder;
        [SerializeField] private Transform _torsoHolder;
        [SerializeField] private Transform _verticalOffsetTransform;
        [SerializeField] private Transform _leftHandHolder;
        [SerializeField] private Transform _rightHandHolder;

        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Transform _namePlateTransform;
        [SerializeField] private GameObject _interactor2DGameObject;
        [SerializeField] private GameObject _interactorVRLeftGameObject;
        [SerializeField] private GameObject _interactorVRRightGameObject;
        [SerializeField] private GameObject _interactorFeetGameObject;
        [SerializeField] private PlayerAvatarHandler _avatarHandler;
        [SerializeField] private bool _isAdmin;

        /// <summary>
        /// Note, this WON'T set the initial appearance, HandleReceiveAvatarAppearance should be called after initialization
        /// </summary>
        public void Initialize(ushort clientID, HandInteractorContainer interactorContainer, AvatarHandlerBuilderContext avatarHandlerBuilderContext)
        {
            _interactorVRLeftGameObject.name = $"Interactor{clientID}-{InteractorType.LeftHandVR}";
            _interactorVRRightGameObject.name = $"Interactor{clientID}-{InteractorType.RightHandVR}";
            _interactor2DGameObject.name = $"Interactor{clientID}-{InteractorType.Mouse2D}";
            _interactorFeetGameObject.name = $"Interactor{clientID}-{InteractorType.Feet}";

            _interactorVRLeftGameObject.GetComponent<RemoteInteractor>().Initialize(clientID, InteractorType.LeftHandVR, interactorContainer);
            _interactorVRRightGameObject.GetComponent<RemoteInteractor>().Initialize(clientID, InteractorType.RightHandVR, interactorContainer);
            _interactor2DGameObject.GetComponent<RemoteInteractor>().Initialize(clientID, InteractorType.Mouse2D, interactorContainer);
            _interactorFeetGameObject.GetComponent<RemoteInteractor>().Initialize(clientID, InteractorType.Feet, interactorContainer);

            _avatarHandler = new(
                avatarHandlerBuilderContext.PlayerBuiltInGameObjectPrefabs,
                avatarHandlerBuilderContext.PlayerCustomGameObjectPrefabs,
                avatarHandlerBuilderContext.CurrentInstancedAvatarAppearance,
                false,
                _headHolder,
                _torsoHolder,
                _rightHandHolder,
                _leftHandHolder);

            _avatarHandler.Enable(clientID);
        }

        //public void ToggleAvatarsTransparent(bool isTransparent) => _avatarHandler.SetTransparent(isTransparent);

        public void HandleReceiveRemotePlayerState(PlayerTransformData playerState)
        {
            transform.SetPositionAndRotation(playerState.RootPosition, playerState.RootRotation);
            _verticalOffsetTransform.localPosition = new Vector3(0, playerState.VerticalOffset, 0);
            _headHolder.SetLocalPositionAndRotation(playerState.HeadLocalPosition, playerState.HeadLocalRotation);

            //We only want to show the VR hands if we're in VR mode, AND the hands are actually tracking
            _interactorVRLeftGameObject.SetActive(playerState.IsVRMode && playerState.HandVRLeftLocalPosition != Vector3.zero);
            _interactorVRRightGameObject.SetActive(playerState.IsVRMode && playerState.HandVRRightLocalPosition != Vector3.zero);
            _interactor2DGameObject.SetActive(!playerState.IsVRMode); //No visual, so we can show them regardless 

            if (playerState.IsVRMode)
            {
                _interactorVRLeftGameObject.transform.SetLocalPositionAndRotation(playerState.HandVRLeftLocalPosition, playerState.HandVRLeftLocalRotation);
                _interactorVRRightGameObject.transform.SetLocalPositionAndRotation(playerState.HandVRRightLocalPosition, playerState.HandVRRightLocalRotation);

                UpdateHeldActivatableIDs(_interactorVRLeftGameObject, playerState.HeldActivatableIdsVRLeft);
                UpdateHeldActivatableIDs(_interactorVRRightGameObject, playerState.HeldActivatableIdsVRRight);
            }
            else
            {
                _interactor2DGameObject.transform.SetLocalPositionAndRotation(playerState.Hand2DLocalPosition, playerState.Hand2DLocalRotation);

                UpdateHeldActivatableIDs(_interactor2DGameObject, playerState.HeldActivatableIds2D);
            }

            UpdateHeldActivatableIDs(_interactorFeetGameObject, playerState.HeldActivatableIdsFeet);
        }

        public void UpdateHeldActivatableIDs(GameObject interactorGameObject, List<string> receivedHeldActivatableIDs)
        {
            RemoteInteractor remoteInteractor = interactorGameObject.GetComponent<RemoteInteractor>();

            foreach (string receivedActivatableID in receivedHeldActivatableIDs)
                if (!remoteInteractor.HeldActivatableIDs.Contains(receivedActivatableID))
                    remoteInteractor.AddToHeldActivatableIDs(receivedActivatableID);

            List<string> activatableIDsToRemove = new List<string>();

            foreach (string localActivatableID in remoteInteractor.HeldActivatableIDs)
                if (!receivedHeldActivatableIDs.Contains(localActivatableID))
                    activatableIDsToRemove.Add(localActivatableID);

            foreach (string idToRemove in activatableIDsToRemove)
                remoteInteractor.RemoveFromHeldActivatableIDs(idToRemove);
        }


        internal void HandleReceiveAvatarAppearance(InstancedAvatarAppearance newAvatarAppearance)
        {
            _playerNameText.text = newAvatarAppearance.BuiltInPresentationConfig.PlayerName;
            _avatarHandler.UpdateInstancedAvatarAppearance(newAvatarAppearance);
        }

        internal void HandleReceiveAdminUpdateNotice(bool isNewAdmin)
        {
            if (_isAdmin == isNewAdmin)
                return;

            _isAdmin = isNewAdmin;

            if (_isAdmin)
                _playerNameText.color = Color.cyan;
            else
                _playerNameText.color = Color.white;
        }
        private void Update()
        {
            if (Camera.main == null)
                return;

            Vector3 dirToCamera = Camera.main.transform.position - _namePlateTransform.position;
            Vector3 lookPosition = _namePlateTransform.position - dirToCamera;
            _namePlateTransform.LookAt(lookPosition);

            _avatarHandler.HandleUpdate();
        }

        private void OnDisable()
        {
            //Destroy GO for domain reload
            if (gameObject != null)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            _interactorFeetGameObject.GetComponent<RemoteInteractor>().HandleOnDestroy();
            _interactorVRLeftGameObject.GetComponent<RemoteInteractor>().HandleOnDestroy();
            _interactorVRRightGameObject.GetComponent<RemoteInteractor>().HandleOnDestroy();
            _interactor2DGameObject.GetComponent<RemoteInteractor>().HandleOnDestroy();
        }
    }
}
