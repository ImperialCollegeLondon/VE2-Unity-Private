using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VE2.Core.Player.API;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    [ExecuteAlways]
    public class V_UIProvider : MonoBehaviour, IUIProvider
    {
        //Hidden for now, we always want both. 
        [SerializeField, HideInInspector] private bool _enablePrimaryUI = true;
        [SerializeField, HideInInspector] private bool _enableSecondaryUI = true;

        public IPrimaryUIService PrimaryUIService {
            get
            {
                if (_primaryUIService == null)
                    OnEnable();
                return _primaryUIService;
            }
        }
        public ISecondaryUIService SecondaryUIService {
            get
            {
                if (_secondaryUIService == null)
                    OnEnable();
                return _secondaryUIService;
            }
        }
        public string GameObjectName => gameObject.name;
        public bool IsEnabled => IsEnabled;

        private PrimaryUIService _primaryUIService;
        private SecondaryUIService _secondaryUIService;

        private void OnEnable()
        {
            UIAPI.UIProvider = this;

            if (!Application.isPlaying || (_enablePrimaryUI && _primaryUIService != null) || (_enableSecondaryUI && _secondaryUIService != null))
                return;

            if (_primaryUIService == null && _enablePrimaryUI)
            {
                InputSystemUIInputModule inputSystemUIInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
                if (inputSystemUIInputModule == null)
                    inputSystemUIInputModule = new GameObject("InputSystemUIInputModule").AddComponent<InputSystemUIInputModule>();

                _primaryUIService = new PrimaryUIService(PlayerAPI.InputHandler.TogglePrimaryUI, inputSystemUIInputModule);
            };

            if (_secondaryUIService == null && _enableSecondaryUI)
            {                
                _secondaryUIService = new SecondaryUIService(PlayerAPI.InputHandler.ToggleSecondaryUI);
            };
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _primaryUIService?.TearDown();
            _secondaryUIService?.TearDown();
        }
    }

    /*
        Perhaps we should follow the same pattern as the player here? 
        I.E - have the actual service itself be responsible for spawning the gameobject 

    */
}
