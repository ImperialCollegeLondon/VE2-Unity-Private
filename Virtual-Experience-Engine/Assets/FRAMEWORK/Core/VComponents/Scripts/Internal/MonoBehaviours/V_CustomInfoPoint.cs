using UnityEngine;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.API;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace VE2.Core.VComponents.Integration
{
    [ExecuteAlways]
    [AddComponentMenu("")] //Better to create this from the gameobject menu, where it'll spawn the entire prefab. Hide this from the AC menu
    internal class V_CustomInfoPoint : MonoBehaviour, IV_InfoPoint
    {
        [Title("InfoPoint Settings", Order = -50)]
        [SerializeField, BeginGroup] private GameObject _triggerGameObject;
        [SerializeField] private GameObject _canvasGameObject;
        [SerializeField, EndGroup] private Button _canvasCloseButton;

        [SerializeField, IgnoreParent] private ToggleActivatableConfig _toggleActivatableConfig = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _triggerProgrammaticInterface._StateModule;
        IRangedToggleClickInteractionModule IV_ToggleActivatable._RangedToggleClickModule => _triggerProgrammaticInterface._RangedToggleClickModule;
        #endregion

        private V_ToggleActivatable _triggerActivatable = null;
        private IV_ToggleActivatable _triggerProgrammaticInterface => _triggerActivatable as IV_ToggleActivatable;

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            //Set up trigger=================================
            //Must pass the activatable config _before_ enabling, otherwise that config wont make it into the ActivatableService
            //Why do we create this at runtime? Because we want to show the config on the root InfoPoint object, but the actual activatable needs to live on the trigger at runtime 
            _triggerGameObject.SetActive(false); //To ensure the trigger activatable's OnEnable doesn't fire
            _triggerActivatable = _triggerGameObject.AddComponent<V_ToggleActivatable>(); 
            _triggerActivatable.Config = _toggleActivatableConfig;
            _triggerGameObject.SetActive(true); //Trigger activatable's OnEnable can now execute with the config 

            InfoPointTriggerAnimationHandler _triggerAnimationHandler = _triggerGameObject.GetComponent<InfoPointTriggerAnimationHandler>();
            if (_triggerAnimationHandler != null)
            {
                if (_toggleActivatableConfig.StateConfig.ActivateOnStart)
                    _triggerAnimationHandler.ToggleShowTrigger(false, instant: true);
                else
                    _triggerAnimationHandler.ToggleShowTrigger(true, instant: true);
            }

            ///Set up canvas=================================
            _canvasGameObject.SetActive(true);

            InfoPointCanvasAnimationHandler _canvasAnimationHandler = _canvasGameObject.GetComponent<InfoPointCanvasAnimationHandler>();
            if (_canvasAnimationHandler != null)
            {
                if (_toggleActivatableConfig.StateConfig.ActivateOnStart)
                    _canvasAnimationHandler.ToggleShowCanvas(true, instant: true);
                else
                    _canvasAnimationHandler.ToggleShowCanvas(false, instant:  true);
            }

            //Wire up the close button to close the canvas
            _canvasCloseButton?.onClick.AddListener(() => ToggleCanvas(false));
        }

        public void ToggleCanvas(bool canvasActive) => _triggerProgrammaticInterface.SetActivated(canvasActive);

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _triggerActivatable.enabled = false;
        }
    }
}
