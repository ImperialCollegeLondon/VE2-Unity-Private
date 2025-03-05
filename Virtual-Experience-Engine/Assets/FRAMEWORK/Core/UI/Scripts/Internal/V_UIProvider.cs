using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    public class V_UIProvider : MonoBehaviour, IUIProvider
    {
        [SerializeField] private bool _enablePrimaryUI = true;
        [SerializeField] private bool _enableSecondaryUI = true;

        public IPrimaryUIService PrimaryUIService => _primaryUIService;
        public ISecondaryUIService SecondaryUIService => _secondaryUIService;
        public string GameObjectName => gameObject.name;
        public bool IsEnabled => IsEnabled;

        private PrimaryUIService _primaryUIService;
        private SecondaryUIService _secondaryUIService;

        private void OnEnable()
        {
            UIAPI.UIProvider = this;

            if (_primaryUIService == null && _enablePrimaryUI)
            {
                _primaryUIService = new PrimaryUIService();
            };

            if (_secondaryUIService == null && _enableSecondaryUI)
            {
                GameObject secondaryUIGO = GameObject.Instantiate(Resources.Load<GameObject>("SecondaryUI"));
                SecondaryUIReferences secondaryUIReferences = secondaryUIGO.GetComponent<SecondaryUIReferences>();
                _secondaryUIService = new SecondaryUIService(secondaryUIReferences);
            };
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _primaryUIService.TearDown();
            _secondaryUIService.TearDown();
        }
    }

    /*
        Perhaps we should follow the same pattern as the player here? 
        I.E - have the actual service itself be responsible for spawning the gameobject 

    */
}
