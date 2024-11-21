using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;

public class V_HandController
{
    public Transform GrabberTransform => _interactor.GrabberTransform;

    private readonly GameObject _handGO;
    private readonly IValueInput<Vector3> _positionInput;
    private readonly IValueInput<Quaternion> _rotationInput;
    private readonly InteractorVR _interactor;

    public V_HandController(GameObject handGO, HandVRInputContainer handVRInputContainer, InteractorType interactorType, IMultiplayerSupport multiplayerSupport, IRaycastProvider raycastProvider)
    {
        V_HandVRReferences handVRReferences = handGO.GetComponent<V_HandVRReferences>();
        _interactor = handGO.GetComponent<InteractorVR>(); //TODO: Move to refs, or decouple from MB?
        _interactor.InitializeVR(handVRReferences.RayOrigin, interactorType, multiplayerSupport, handVRInputContainer.InteractorVRInputContainer, raycastProvider, 
            handVRReferences.CollisionDetector, handVRReferences.HandVisualGO, handVRReferences.LineRenderer);

        //TODO: Ray should end at the racyast hit point... how do we do this? RaycastProvider needs to return even if it doesn't hit an interactable
    }

    public void HandleOnEnable() 
    {
        _interactor.HandleOnEnable();
    }

    public void HandleOnDisable() 
    {
        _interactor.HandleOnDisable();
    }

    public void HandleUpdate() 
    {
        _handGO.transform.localPosition = _positionInput.Value;
        _handGO.transform.localRotation = _rotationInput.Value;
    }

    //TODO:
    /*
        When interactor tells us it's grabbed, we need to hide the hand model 
        Add some HandModel handler, wire in input to the hand model
        Figure out hand poses, should probably be orchestrated through here? 

    */
}
