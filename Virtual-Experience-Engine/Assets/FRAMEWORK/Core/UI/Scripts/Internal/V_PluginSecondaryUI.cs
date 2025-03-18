using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    //TODO - also need some utils for confirming that the UI is configured correctly, only one child of the holder, etc
    //TODO - look at merging this with the other PluginUi script, as well as the UIProvider
    [ExecuteAlways]
    internal class V_PluginSecondaryUI : MonoBehaviour
    {
        [SerializeField, HideInInspector] private GameObject _pluginSecondaryUIHolder;

        private void Awake()
        {
            if (!Application.isPlaying && _pluginSecondaryUIHolder == null)
            {
                GameObject pluginPrimaryUIHolderPrefab = Resources.Load<GameObject>("PluginSecondaryUIHolder");
                _pluginSecondaryUIHolder = Instantiate(pluginPrimaryUIHolderPrefab, transform);
            }
        }

        private void OnEnable()
        {
            _pluginSecondaryUIHolder.SetActive(true);

            if (!Application.isPlaying)
                return;

            GameObject pluginSecondaryUI = _pluginSecondaryUIHolder.transform.GetChild(0).gameObject;

            if (UIAPI.SecondaryUIService != null)
            {
                UIAPI.SecondaryUIService.SetContent(pluginSecondaryUI.GetComponent<RectTransform>());
            }
            else 
            {
                Debug.LogError("Could not move plugin primary UI to primary UI - please add a V_UIProvider to the scene.");
            }

            Destroy(_pluginSecondaryUIHolder);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                return;

            _pluginSecondaryUIHolder.SetActive(false);
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying && _pluginSecondaryUIHolder != null)
                DestroyImmediate(_pluginSecondaryUIHolder);
        }
    }
}

