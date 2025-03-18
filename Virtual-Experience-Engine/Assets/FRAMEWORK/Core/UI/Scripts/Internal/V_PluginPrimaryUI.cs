using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    //TODO - also need some utils for confirming that the UI is configured correctly, only one child of the holder, etc
    //TODO - look at merging this with the other PluginUi script, as well as the UIProvider
    [ExecuteAlways]
    internal class V_PluginPrimaryUI : MonoBehaviour
    {
        [SerializeField, HideInInspector] private GameObject _pluginPrimaryUIHolder;
        private string _tabName => "World Info";

        private void Awake()
        {
            if (!Application.isPlaying && _pluginPrimaryUIHolder == null)
            {
                GameObject pluginPrimaryUIHolderPrefab = Resources.Load<GameObject>("PluginPrimaryUIHolder");
                _pluginPrimaryUIHolder = Instantiate(pluginPrimaryUIHolderPrefab, transform);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            GameObject pluginPrimaryUI = _pluginPrimaryUIHolder.transform.GetChild(0).gameObject;
            Sprite icon = Resources.Load<Sprite>("PluginPrimaryUIIcon");

            if (UIAPI.PrimaryUIService != null)
            {
                UIAPI.PrimaryUIService.AddNewTab(
                    _tabName, 
                    pluginPrimaryUI, 
                    icon,
                    0);
                UIAPI.PrimaryUIService.ShowTab(_tabName);   
            }
            else 
            {
                Debug.LogError("Could not move plugin primary UI to primary UI - please add a V_UIProvider to the scene.");
            }

            Destroy(_pluginPrimaryUIHolder);
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying && _pluginPrimaryUIHolder != null)
                DestroyImmediate(_pluginPrimaryUIHolder);
        }
    }
}
