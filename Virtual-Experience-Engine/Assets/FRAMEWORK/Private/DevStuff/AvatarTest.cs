using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

public class AvatarTest : MonoBehaviour
{
    [SerializeField] private GameObject gameObjectWithActivatable;
    private IV_ToggleActivatable activatable => gameObjectWithActivatable.GetComponent<IV_ToggleActivatable>();

    public void DoThing()
    {
        if (activatable.MostRecentInteractingClientID == InstancingAPI.InstanceService.LocalClientID)
            PlayerAPI.Player.SetAvatarHeadOverride(0);
    }

    public void DoOtherThing()
    {
        if (activatable.MostRecentInteractingClientID == InstancingAPI.InstanceService.LocalClientID)
            PlayerAPI.Player.ClearAvatarHeadOverride();
    }
}
