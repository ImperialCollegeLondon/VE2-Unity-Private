using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.NonCore.Instancing.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class RemoteInteractor : MonoBehaviour, IInteractor //TODO: Maybe doesn't need to be a mononbehaviour?
    {
        public Transform GrabberTransform => transform;
        public List<string> HeldActivatableIDs => _heldActivatableIDs;
        private HandInteractorContainer _interactorContainer;
        private InteractorID _interactorID;
        private List<string> _heldActivatableIDs;

        public void Initialize(ushort clientID, InteractorType interactorType, HandInteractorContainer interactorContainer)
        {
            _interactorID = new InteractorID(clientID, interactorType);

            _interactorContainer = interactorContainer;
            _interactorContainer.RegisterInteractor(_interactorID.ToString(), this);

            _heldActivatableIDs = new List<string>();
        }

        public void AddToHeldActivatableIDs(string activatableID)
        {
            _heldActivatableIDs.Add(activatableID);

            IRangedHoldClickInteractionModule rangedClickInteractable = GetRangedClickInteractionModule(activatableID);
            rangedClickInteractable?.ClickDown(_interactorID);

            ICollideInteractionModule collideInteractable = GetCollideInteractionModule(activatableID);
            collideInteractable?.InvokeOnCollideEnter(_interactorID);
        }

        public void RemoveFromHeldActivatableIDs(string activatableID)
        {
            IRangedHoldClickInteractionModule rangedClickInteractable = GetRangedClickInteractionModule(activatableID);
            rangedClickInteractable?.ClickUp(_interactorID);

            ICollideInteractionModule collideInteractable = GetCollideInteractionModule(activatableID);
            collideInteractable?.InvokeOnCollideExit(_interactorID);

            _heldActivatableIDs.Remove(activatableID);

        }

        public IRangedHoldClickInteractionModule GetRangedClickInteractionModule(string activatableID)
        {
            if (activatableID.Contains("HoldActivatable-"))
            {
                string cleanID = activatableID.Replace("HoldActivatable-", "");
                GameObject activatableObject = GameObject.Find(cleanID);

                if (activatableObject != null)
                    return activatableObject.GetComponent<IRangedInteractionModuleProvider>().RangedInteractionModule as IRangedHoldClickInteractionModule;
                else
                    return null;
            }
            else
                return null;
        }

        public ICollideInteractionModule GetCollideInteractionModule(string activatableID)
        {
            if (activatableID.Contains("PressurePlate-"))
            {
                string cleanID = activatableID.Replace("PressurePlate-", "");
                GameObject activatableObject = GameObject.Find(cleanID);

                if (activatableObject != null)
                    return activatableObject.GetComponent<ICollideInteractionModuleProvider>().CollideInteractionModule;
                else
                    return null;
            }
            else
                return null;
        }

        public void TearDown()
        {
            _interactorContainer.DeregisterInteractor(_interactorID.ToString());
        }

        public void ConfirmGrab(string id)
        {
            //TODO: hide
        }

        public void ConfirmDrop()
        {
            //TODO: Show 
        }

        public void HandleOnDestroy()
        {
            foreach (string activatableID in _heldActivatableIDs)
            {
                IRangedHoldClickInteractionModule rangedClickInteractable = GetRangedClickInteractionModule(activatableID);
                rangedClickInteractable?.ClickUp(_interactorID);

                ICollideInteractionModule collideInteractable = GetCollideInteractionModule(activatableID);
                collideInteractable?.InvokeOnCollideExit(_interactorID);
            }

            _heldActivatableIDs.Clear();
        }
    }
}
