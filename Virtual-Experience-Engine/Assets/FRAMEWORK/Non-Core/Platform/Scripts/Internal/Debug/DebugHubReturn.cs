using UnityEngine;
using VE2.NonCore.Platform.API;

internal class DebugHubReturn : MonoBehaviour
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    public void ReturnToHub()
    {
        ((IPlatformServiceInternal)PlatformAPI.PlatformService).ReturnToHub();
    }
}
