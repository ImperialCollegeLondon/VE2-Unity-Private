using System;
using System.Collections;
using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using static VE2.NonCore.Platform.Internal.PlatformSerializables;

namespace VE2.NonCore.Platform.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class DebugGlobalInfoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text globalInfoText;

        private PlatformService _platformIntegration;

        private void OnEnable()
        {
            StartCoroutine(DelayedOnEnable());
        }

        private IEnumerator DelayedOnEnable()
        {
            yield return new WaitForSeconds(0.1f);

            //PlatformServiceProvider provider = FindFirstObjectByType<PlatformServiceProvider>();
            _platformIntegration = (PlatformService)VE2API.PlatformService;
            if (_platformIntegration != null)
            {

                //If we're already connected to the server, display initial global info rather than waiting for an update
                if (_platformIntegration.IsConnectedToServer)
                    HandleGlobalInfoChanged(_platformIntegration.GlobalInfo);

                _platformIntegration.OnGlobalInfoChanged += HandleGlobalInfoChanged;
            }
            else
            {
                globalInfoText.text = "No platform service provider found";
                gameObject.SetActive(false);
            }
        }


        private void HandleGlobalInfoChanged(GlobalInfo globalInfo)
        {
            string globalInfoString = $"<b>PLATFORM</b> \nLocal ID = {_platformIntegration.LocalClientID}\n";

            foreach (PlatformInstanceInfo platformInstanceInfo in globalInfo.InstanceInfos.Values)
            {
                globalInfoString += $"{platformInstanceInfo.InstanceCode.ToString()}_____";
                foreach (PlatformClientInfo clientInfo in platformInstanceInfo.ClientInfos.Values)
                {
                    if (clientInfo.ClientID.Equals(_platformIntegration.LocalClientID))
                        globalInfoString += $"<color=green>";

                    globalInfoString += $"\n   {clientInfo.ClientID}";
                    globalInfoString += $"({clientInfo.PlayerPresentationConfig.PlayerName}): ";

                    if (clientInfo.ClientID.Equals(_platformIntegration.LocalClientID))
                        globalInfoString += $"</color>";
                }
                globalInfoString += "\n";
            }

            globalInfoText.text = globalInfoString;
        }

        private void OnDisable()
        {
            if (_platformIntegration != null)
                _platformIntegration.OnGlobalInfoChanged -= HandleGlobalInfoChanged;
        }
    }
}
