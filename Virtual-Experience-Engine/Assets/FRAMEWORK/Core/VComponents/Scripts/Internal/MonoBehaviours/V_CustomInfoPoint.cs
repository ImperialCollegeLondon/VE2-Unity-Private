using UnityEngine;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.API;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Integration
{
    internal partial class V_CustomInfoPoint : IV_InfoPoint
    {
        #region State Module Interface

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated  => _StateModule.IsActivated;
        public void Activate() => _StateModule.Activate();
        public void Deactivate() => _StateModule.Deactivate();
        public void SetActivated(bool isActivated) => _StateModule.SetActivated(isActivated);
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;

        public void SetNetworked(bool isNetworked) => _StateModule.SetNetworked(isNetworked);
        #endregion

        #region Ranged Interaction Module Interface
        public float InteractRange { get => _RangedToggleClickModule.InteractRange; set => _RangedToggleClickModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedToggleClickModule.AdminOnly; set => _RangedToggleClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedToggleClickModule.EnableControllerVibrations; set => _RangedToggleClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedToggleClickModule.ShowTooltipsAndHighlight; set => _RangedToggleClickModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedToggleClickModule.IsInteractable; set => _RangedToggleClickModule.IsInteractable = value; }
        #endregion
    }

    [ExecuteAlways]
    [AddComponentMenu("")] //Better to create this from the gameobject menu, where it'll spawn the entire prefab. Hide this from the AC menu
    internal partial class V_CustomInfoPoint : MonoBehaviour
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_CustomInfoPoint-20f0e4d8ed4d81eb8e3eee6377ffa130?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above, Order = -100)]
        [Title("InfoPoint Settings", Order = -50)]
        [SerializeField, BeginGroup] private GameObject _triggerGameObject;
        [SerializeField] private GameObject _canvasGameObject;
        [SerializeField, EndGroup] private Button _canvasCloseButton;

        [SerializeField, IgnoreParent] private ToggleActivatableConfig _toggleActivatableConfig = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule _StateModule => _TriggerActivatable._StateModule;
        IRangedToggleClickInteractionModule _RangedToggleClickModule => _TriggerActivatable._RangedToggleClickModule;
        #endregion

        private V_ToggleActivatable _triggerActivatable = null;
        private V_ToggleActivatable _TriggerActivatable
        {
            get
            {
                if (_triggerActivatable == null)
                    OnEnable();
                return _triggerActivatable;
            }
        }

        private void OnEnable()
        {
            _triggerGameObject.name = $"{gameObject.name}_Trigger";
            _canvasGameObject.name = $"{gameObject.name}_Canvas";

            if (!Application.isPlaying || _triggerActivatable != null)
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

        public void ToggleCanvas(bool canvasActive) => _TriggerActivatable.SetActivated(canvasActive);

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _triggerActivatable.enabled = false;
        }
    }
}
