using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    //TODO - also need some utils for confirming that the UI is confdiged correctly, only one child of the holder, etc
    [ExecuteAlways]
    internal class V_PluginPrimaryUI : MonoBehaviour
    {
        [SerializeField, HideInInspector] private GameObject _pluginPrimaryUIHolder;
        private string _tabName => "World Info";

        //TODO - when creating mono in edit mode, create prefab in scene 

        //On enable in play mode, ask the PrimaryUIService to move our UI panel into the canvas - probably also destroy the surrounding canvas 

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

            UIAPI.PrimaryUIService.AddNewTab(
                _tabName, 
                pluginPrimaryUI, 
                icon,
                0);
            UIAPI.PrimaryUIService.ShowTab(_tabName);

            Destroy(_pluginPrimaryUIHolder);
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying && _pluginPrimaryUIHolder != null)
                DestroyImmediate(_pluginPrimaryUIHolder);
        }
    }
}
