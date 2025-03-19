using System;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VE2.Core.VComponents.Integration
{
    internal class V_CustomInfoPoint : MonoBehaviour
    {

        public UnityEvent OnInfoPointOpen = new();
        public UnityEvent OnInfoPointClose = new();

        private IV_ToggleActivatable _toggleActivatable = null;
        private GameObject _infoPointCanvasGO;
        private Button _infoPointCloseButton;
        private GameObject _infoPointTriggerGO;
        private GameObject _infoPointTriggerVisualElementsGO;
        private Renderer _infoPointTriggerGORenderer;
        private Collider _infoPointTriggerGOCollider;

        private void OnEnable()
        {
            _toggleActivatable ??= GetComponentInChildren<IV_ToggleActivatable>();

            _infoPointCanvasGO = GetComponentInChildren<Canvas>(true).gameObject;
            _infoPointCloseButton = _infoPointCanvasGO.GetComponentInChildren<Button>();
            _infoPointTriggerGO = GetComponentInChildren<V_ToggleActivatable>(true).gameObject;
            _infoPointTriggerGORenderer = _infoPointTriggerGO.GetComponent<MeshRenderer>();
            _infoPointTriggerGOCollider = _infoPointTriggerGO.GetComponent<Collider>();
            _infoPointTriggerVisualElementsGO = _infoPointTriggerGO.transform.GetChild(0).gameObject;


            //_toggleActivatable.OnActivate.AddListener(OpenInfoPoint);
            //_toggleActivatable.OnDeactivate.AddListener(OpenInfoPoint);
            //_infoPointCloseButton.onClick.AddListener(CloseInfoPoint);
        }

        private void OnDisable()
        {
            //_toggleActivatable.OnActivate.RemoveListener(OpenInfoPoint);
            //_toggleActivatable.OnDeactivate.RemoveListener(OpenInfoPoint);
            //_infoPointCloseButton.onClick.RemoveListener(CloseInfoPoint);
        }

        void Update()
        {

        }   

        public void OpenInfoPoint()
        {
            _infoPointCanvasGO.SetActive(true);
            _infoPointTriggerGORenderer.enabled = false;
            _infoPointTriggerGOCollider.enabled = false;
            _infoPointTriggerVisualElementsGO.SetActive(false);

            OnInfoPointOpen.Invoke();
        }

        public void CloseInfoPoint()
        {
            _infoPointCanvasGO.SetActive(false);
            _infoPointTriggerGORenderer.enabled = true;
            _infoPointTriggerGOCollider.enabled = true;
            _infoPointTriggerVisualElementsGO.SetActive(true);

            OnInfoPointClose.Invoke();
        }

        
    }
}