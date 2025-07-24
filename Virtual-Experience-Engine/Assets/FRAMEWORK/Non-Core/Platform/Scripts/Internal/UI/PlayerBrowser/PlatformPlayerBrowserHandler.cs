using UnityEngine;
using VE2.Core.UI.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserHandler
    {
        private readonly IInstanceServiceInternal _instanceServiceInternal;

        public PlayerBrowserHandler(IInstanceServiceInternal instanceServiceInternal, IPrimaryUIServiceInternal primaryUIService)
        {
            _instanceServiceInternal = instanceServiceInternal;

            GameObject playerBrowserUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerBrowserUIHolder"));
            GameObject playerBrowserUI = playerBrowserUIHolder.transform.GetChild(0).gameObject;
            playerBrowserUI.SetActive(false);

            primaryUIService.AddNewTab("Players", playerBrowserUI, Resources.Load<Sprite>("PlayerBrowserUIIcon"), 1);
            GameObject.Destroy(playerBrowserUIHolder);
        }
    }
}
