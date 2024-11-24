using System;
using System.Collections.Generic;
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
    private readonly DragLocomotor _dragLocomotor;
    private List<Material> _colorMaterials = new();

    public V_HandController(InteractorContainer interactorContainer, Transform playerTransform, GameObject handGO, HandVRInputContainer handVRInputContainer, InteractorType interactorType, IMultiplayerSupport multiplayerSupport, IRaycastProvider raycastProvider)
    {
        V_HandVRReferences handVRReferences = handGO.GetComponent<V_HandVRReferences>();
        _interactor = handGO.GetComponent<InteractorVR>(); //TODO: Move to refs, or decouple from MB?
        _interactor.InitializeVR(interactorContainer, handVRReferences.RayOrigin, interactorType, multiplayerSupport, handVRInputContainer.InteractorVRInputContainer, raycastProvider, 
            handVRReferences.CollisionDetector, handVRReferences.HandVisualGO, handVRReferences.LineRenderer);

        _dragLocomotor = new(handVRReferences.DragIconHolder, handVRReferences.HorizontalDragIndicator, handVRReferences.VerticalDragIndicator, handVRReferences.SphereDragIcon, 
            handVRInputContainer.DragLocomotorInputContainer, playerTransform, handGO.transform);

        _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(handGO);
    }

    public void HandleOnEnable() 
    {
        _interactor.HandleOnEnable();
        _dragLocomotor.HandleOEnable();
    }

    public void HandleOnDisable() 
    {
        _interactor.HandleOnDisable();
        _dragLocomotor.HandleOnDisable();
    }

    public void HandleUpdate() 
    {
        _handGO.transform.localPosition = _positionInput.Value;
        _handGO.transform.localRotation = _rotationInput.Value;

        _dragLocomotor.HandleUpdate();
    }

    public void HandleLocalAvatarColorChanged(Color newColor)
    {
        foreach (Material material in _colorMaterials)
            material.color = newColor;
    }


    //TODO:
    /*
        When interactor tells us it's grabbed, we need to hide the hand model 
        Add some HandModel handler, wire in input to the hand model
        Figure out hand poses, should probably be orchestrated through here? 

    */
}
