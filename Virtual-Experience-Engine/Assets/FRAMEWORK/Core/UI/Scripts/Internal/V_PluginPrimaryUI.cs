using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    //TODO - also need some utils for confirming that the UI is confdiged correctly, only one child of the holder, etc
    [ExecuteAlways]
    public class V_PluginPrimaryUI : MonoBehaviour
    {
        [SerializeField] private GameObject _pluginPrimaryUIHolder;

        //TODO - when creating mono in edit mode, create prefab in scene 

        //On enable in play mode, ask the PrimaryUIService to move our UI panel into the canvas - probably also destroy the surrounding canvas 

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            UIAPI.PrimaryUIService.AddNewTab(
                _pluginPrimaryUIHolder.transform.GetChild(0).gameObject, //transform child out of bounds? Still seems to work though
                "My World", 
                IconType.Plugin);
        }
    }
}
