using UnityEngine;
using UnityEngine.InputSystem.UI;
using VE2.Common.API;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    [ExecuteAlways]
    public class V_UIProvider : MonoBehaviour, IUIProvider
    {
        //For now, we always want both. 
        private bool _enablePrimaryUI => true;
        private bool _enableSecondaryUI => true;

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
        public bool IsEnabled => gameObject != null && enabled && gameObject.activeInHierarchy;

        [Help("If enabled, the secondary UI can be customised. If disabled, the secondary UI  This is always enabled for the primary UI.")]
        [SerializeField, HideInInspector] private bool _lastUseSecondaryUI;
        [SerializeField] private bool _useCustomSecondaryUI = true;

        private PrimaryUIService _primaryUIService;
        private SecondaryUIService _secondaryUIService;

        private GameObject _pluginPrimaryUIHolder => FindFirstObjectByType<PluginPrimaryHolderUITag>(FindObjectsInactive.Include)?.gameObject;
        private GameObject _pluginSecondaryUIHolder => FindFirstObjectByType<PluginSecondaryUIHolderTag>(FindObjectsInactive.Include)?.gameObject;
        private string _primaryUIPluginTabName => "World Info";

        private void OnValidate()
        {
            if (_useCustomSecondaryUI != _lastUseSecondaryUI)
            {
                _lastUseSecondaryUI = _useCustomSecondaryUI;
                _pluginSecondaryUIHolder?.SetActive(_useCustomSecondaryUI);
            }
        }

        private void Awake()
        {
            if (Application.isPlaying)
                return;

            if (_pluginPrimaryUIHolder == null)
                Instantiate(Resources.Load<GameObject>("PluginPrimaryUIHolder"), transform);

            if (_pluginSecondaryUIHolder == null)
                Instantiate(Resources.Load<GameObject>("PluginSecondaryUIHolder"), transform);
        }

        private void OnEnable()
        {
            VE2API.UIProvider = this;

            if (!Application.isPlaying || (_enablePrimaryUI && _primaryUIService != null) || (_enableSecondaryUI && _secondaryUIService != null))
                return;

            if (_primaryUIService == null && _enablePrimaryUI)
            {
                //Create Primary UI Service==========
                InputSystemUIInputModule inputSystemUIInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
                if (inputSystemUIInputModule == null)
                    inputSystemUIInputModule = new GameObject("InputSystemUIInputModule").AddComponent<InputSystemUIInputModule>();

                _primaryUIService = new PrimaryUIService(VE2API.InputHandler.TogglePrimaryUI, inputSystemUIInputModule);

                //Move plugin primary UI to primary UI==========
                GameObject pluginPrimaryUI = _pluginPrimaryUIHolder.transform.GetChild(0).gameObject;
                Sprite icon = Resources.Load<Sprite>("PluginPrimaryUIIcon");

                VE2API.PrimaryUIService.AddNewTab(
                    _primaryUIPluginTabName, 
                    pluginPrimaryUI, 
                    icon,
                    0);

                VE2API.PrimaryUIService.ShowTab(_primaryUIPluginTabName);   
            };

            if (_secondaryUIService == null && _enableSecondaryUI)
            {                
                //Create Secondary UI Service==========
                _secondaryUIService = new SecondaryUIService(VE2API.InputHandler.ToggleSecondaryUI);

                //Move plugin secondary UI to secondary UI==========
                if (_useCustomSecondaryUI)
                {
                    GameObject pluginSecondaryUI = _pluginSecondaryUIHolder.transform.GetChild(0).gameObject;
                    ISecondaryUIServiceInternal secondaryUIService = VE2API.SecondaryUIService as ISecondaryUIServiceInternal;
                    secondaryUIService.SetContent(pluginSecondaryUI.GetComponent<RectTransform>());
                }
            };

            if (_pluginPrimaryUIHolder != null)
                Destroy(_pluginPrimaryUIHolder);

            if (_pluginSecondaryUIHolder != null)
                Destroy(_pluginSecondaryUIHolder);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _primaryUIService?.TearDown();
            _secondaryUIService?.TearDown();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            return;

            if (_pluginPrimaryUIHolder != null)
                DestroyImmediate(_pluginPrimaryUIHolder);

            if (_pluginSecondaryUIHolder != null)
                DestroyImmediate(_pluginSecondaryUIHolder);
        }
    }

    /*
        Perhaps we should follow the same pattern as the player here? 
        I.E - have the actual service itself be responsible for spawning the gameobject 

    */
}
