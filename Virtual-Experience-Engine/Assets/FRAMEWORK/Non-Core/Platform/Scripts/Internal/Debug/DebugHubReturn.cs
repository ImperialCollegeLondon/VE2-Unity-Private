using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.Platform.API;

internal class DebugHubReturn : MonoBehaviour
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    public void ReturnToHub()
    {
        ((IPlatformServiceInternal)VE2API.PlatformService).ReturnToHub();
    }
}
