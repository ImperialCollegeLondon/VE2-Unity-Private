using System;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using UnityEngine.Events;
using UnityEngine.UI;
using VE2.Core.Common;

namespace VE2.Core.VComponents.Integration
{
    [ExecuteAlways]
    internal class V_CustomInfoPoint : MonoBehaviour, IV_ToggleActivatable
    {
        [SerializeField, IgnoreParent] private ToggleActivatableConfig _toggleActivatableConfig = new();

        [SerializeField, HideInInspector] private GameObject _infoPointCanvasGO;
        [SerializeField, HideInInspector] private GameObject _infoPointTriggerGO;
        [SerializeField, HideInInspector] private GameObject _infoPointTriggerColliderAndVisualsGO;

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _triggerProgrammaticInterface._StateModule;
        IRangedToggleClickInteractionModule IV_ToggleActivatable._RangedToggleClickModule => _triggerProgrammaticInterface._RangedToggleClickModule;
        #endregion

        private V_ToggleActivatable _triggerActivatable = null;
        private IV_ToggleActivatable _triggerProgrammaticInterface => _triggerActivatable as IV_ToggleActivatable;

        private void Reset()
        {
            if (_infoPointTriggerGO != null)
                DestroyImmediate(_infoPointTriggerGO);

            _infoPointTriggerGO = GameObject.Instantiate(Resources.Load<GameObject>("InfoPoints/InfoPointTrigger"));
            _infoPointTriggerGO.transform.SetParent(transform, worldPositionStays: false);
            _infoPointTriggerGO.transform.localPosition = Vector3.zero;
            _infoPointTriggerGO.transform.localRotation = Quaternion.identity;
            _infoPointTriggerColliderAndVisualsGO = _infoPointTriggerGO.transform.GetChild(0).gameObject;
            
            if (_infoPointCanvasGO != null)
                DestroyImmediate(_infoPointCanvasGO);

            _infoPointCanvasGO = GameObject.Instantiate(Resources.Load<GameObject>("InfoPoints/InfoPointCustomCanvas"));
            _infoPointCanvasGO.transform.SetParent(transform, worldPositionStays: false);
            _infoPointCanvasGO.transform.localPosition = Vector3.zero;
            _infoPointCanvasGO.transform.localRotation = Quaternion.identity;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            //Must pass the config _before_ enabling, otherwise that config wont make it into the ActivatableService
            _infoPointTriggerGO.SetActive(false); //To ensure the trigger activatable's OnEnable doesn't fire
            _triggerActivatable = _infoPointTriggerGO.AddComponent<V_ToggleActivatable>(); //Fires V_ToggleActivatable.OnEnable, I need it not to! Can I attach it disabled?
            Debug.Log("About to give config");
            _triggerActivatable.Config = _toggleActivatableConfig;
            Debug.Log("Config given");
            _infoPointTriggerGO.SetActive(true); //Trigger activatable's OnEnable can now execute with the config 

            _triggerProgrammaticInterface.OnActivate.AddListener(OpenInfoPoint);
            _triggerProgrammaticInterface.OnDeactivate.AddListener(CloseInfoPoint);

            GameObject canvasCloseButtonGO = CommonUtils.FindInChildrenByName(_infoPointCanvasGO, "InfoPointFrameworkCloseButton");
            if (canvasCloseButtonGO != null)
                canvasCloseButtonGO.GetComponent<Button>()?.onClick.AddListener(_triggerProgrammaticInterface.Deactivate);

            _infoPointCanvasGO.SetActive(false);
        }

        public void OpenInfoPoint()
        {
            _infoPointCanvasGO.SetActive(true);
            _infoPointTriggerColliderAndVisualsGO.SetActive(false);
        }

        public void CloseInfoPoint()
        {
            _infoPointCanvasGO.SetActive(false);
            _infoPointTriggerColliderAndVisualsGO.SetActive(true);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _triggerActivatable.enabled = false;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
                return;

            if (_infoPointTriggerGO != null)
                DestroyImmediate(_infoPointTriggerGO);
            
            if (_infoPointCanvasGO != null)
                DestroyImmediate(_infoPointCanvasGO);
        }
    }
}