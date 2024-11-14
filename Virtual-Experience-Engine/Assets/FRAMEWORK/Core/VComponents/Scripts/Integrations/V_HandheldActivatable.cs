using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Integration
{
    public class V_HandheldActivatable : MonoBehaviour
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        internal IHandheldClickInteractionModule HandheldClickInteractionModule => _service.HandheldClickInteractionModule;
      
        private HandheldActivatableService _service = null;

        private void OnEnable()
        {
            string id = "HHActivatable-" + gameObject.name; 
            _service = new (_config, _state, id, VE2CoreServiceLocator.Instance.WorldStateModulesContainer);
        }

        private void FixedUpdate()
        {
            _service.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _service.TearDown();
            _service = null;
        }
    }
}

