using UnityEngine;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.Private.Hub
{
    /*
        TODO - we want to check if the platform has already been setup with arguments 
        If so, we don't need to update settings 
        
        What happens if the hub app is still active on the headset? How do we know to request instance allocation back to hub?? 

        We can check if we regain focus, if we do, request hub allocation...


        We'll need to disconnect from the server when we go to a plugin 
        This means we can 

        We're in the hub, we request allocation to an instance, the server then thinks we're in that instance, when the platformService gets that message in the hub, it goes to that plugin 
        When we return to the hub... yeah, maybe we should then request platform allocation explicitly?

        Every time we start in the hub, we should request allocation to the hub 

    */
    public class HubControllerOLD : MonoBehaviour
    {
        private void OnEnable()
        {
            Debug.Log("Connecting to hub instance");
            IPlatformServiceInternal platformService = (IPlatformServiceInternal)PlatformAPI.PlatformService;
            platformService.UpdateSettings(_platformServerConnectionSettings, new InstanceCode("Hub", "Solo", 0));
            platformService.ConnectToPlatform();

            Application.focusChanged += OnFocusChanged;
        }

        private void OnDisable()
        {
            Application.focusChanged -= OnFocusChanged;

            //on android, we need to disconnect here, as the plugin we're going to will connect to the server itself
            //if we're in windows, this will happen automatically
            if (Application.platform == RuntimePlatform.Android)
            {
                Debug.Log("Disconnecting from hub instance");
                IPlatformServiceInternal platformService = (IPlatformServiceInternal)PlatformAPI.PlatformService;
                platformService.TearDown();
            }
        }

        private void OnFocusChanged(bool hasFocus)
        {
            Debug.Log("Focus changed: " + hasFocus);
            if (Application.platform == RuntimePlatform.Android)
            {
                if (hasFocus)
                {
                    OnEnable();
                }
                else 
                {
                    //This happens when we go to a plugin
                    //We need to disconnect from the server
                    OnDisable();
                }
            }
        }

        //TODO: username and password should come from arguments - or probably in a different scene?
        [SerializeField] private ServerConnectionSettings _platformServerConnectionSettings = new("devName", "devPassword", "127.0.0.1", 4298);
    }
}
