using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

public class NewInterfaceReferenceExample : MonoBehaviour
{
    [SerializeField] private InterfaceReference<IV_ToggleActivatable> _toggleButton;

    public void DoThing()
    {
        if (_toggleButton.Interface.MostRecentInteractingClientID == InstancingAPI.InstanceService.LocalClientID) 
            PlayerAPI.Player.SetAvatarHeadOverride(0);
    }

    public void DoOtherThing()
    {
        if (_toggleButton.Interface.MostRecentInteractingClientID == InstancingAPI.InstanceService.LocalClientID)
            PlayerAPI.Player.ClearAvatarHeadOverride();
    }
}
