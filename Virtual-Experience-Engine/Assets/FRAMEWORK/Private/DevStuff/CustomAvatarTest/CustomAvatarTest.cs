using UnityEngine;
using VE2.Common.API;

public class CustomAvatarTest : MonoBehaviour
{
    public void OnHead1ButtonPushed()
    {
        VE2API.Player.SetAvatarHeadOverride(0);
        VE2API.Player.SetCustomHeadIndex(0);
    }

    public void OnHead2ButtonPushed()
    {
        VE2API.Player.SetAvatarHeadOverride(1);
        VE2API.Player.SetCustomHeadIndex(0);
    }

    public void OnClearButtonPushed()
    {
        VE2API.Player.ClearAvatarHeadOverride();
        VE2API.Player.ClearAvatarTorsoOverride();
    }
}
