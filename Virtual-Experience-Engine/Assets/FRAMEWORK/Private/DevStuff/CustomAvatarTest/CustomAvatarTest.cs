using UnityEngine;
using VE2.Common.API;

public class CustomAvatarTest : MonoBehaviour
{
    public void OnToggleCustomHeadEnabled(bool isEnabled)
    {
        VE2API.Player.SetCustomHeadEnabled(isEnabled);
    }

    public void OnHead1ButtonPushed()
    {
        VE2API.Player.SetCustomHeadIndex(0);
    }

    public void OnHead2ButtonPushed()
    {
        VE2API.Player.SetCustomHeadIndex(1);
    }

    public void OnClearButtonPushed()
    {
        VE2API.Player.SetCustomHeadEnabled(false);
        VE2API.Player.SetCustomHeadIndex(0);
    }
}
