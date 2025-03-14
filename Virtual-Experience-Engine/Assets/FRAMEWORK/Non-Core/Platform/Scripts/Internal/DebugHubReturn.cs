using UnityEngine;
using VE2.NonCore.Platform.API;

public class DebugHubReturn : MonoBehaviour
{
    public void ReturnToHub()
    {
        ((IPlatformServiceInternal)PlatformAPI.PlatformService).ReturnToHub();
    }
}
