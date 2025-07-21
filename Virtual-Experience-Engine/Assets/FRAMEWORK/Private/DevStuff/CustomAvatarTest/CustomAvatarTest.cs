using UnityEngine;
using VE2.Common.API;

public class CustomAvatarTest : MonoBehaviour
{
    public void OnToggleBuiltInHeadEnabled(bool isEnabled)
    {
        VE2API.Player.SetBuiltInHeadEnabled(isEnabled);
    }

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

    public void OnToggleBuiltInTorsoEnabled(bool isEnabled)
    {
        VE2API.Player.SetBuiltInTorsoEnabled(isEnabled);
    }

    public void OnToggleCustomTorsoEnabled(bool isEnabled)
    {
        VE2API.Player.SetCustomTorsoEnabled(isEnabled);
    }

    public void OnTorso1ButtonPushed()
    {
        VE2API.Player.SetCustomTorsoIndex(0);
    }

    public void OnTorso2ButtonPushed()
    {
        VE2API.Player.SetCustomTorsoIndex(1);
    }

    public void OnToggleBuiltInRightHandVREnabled(bool isEnabled)
    {
        VE2API.Player.SetBuiltInRightHandVREnabled(isEnabled);
    }

    public void OnToggleCustomRightHandVREnabled(bool isEnabled)
    {
        VE2API.Player.SetCustomRightHandVREnabled(isEnabled);
    }

    public void OnRightHandVRPushed()
    {
        VE2API.Player.SetCustomRightHandVRIndex(0);
    }

    public void OnRightHandVR2Pushed()
    {
        VE2API.Player.SetCustomRightHandVRIndex(1);
    }

    public void OnToggleBuiltInLeftHandVREnabled(bool isEnabled)
    {
        VE2API.Player.SetBuiltInLeftHandVREnabled(isEnabled);
    }

    public void OnToggleCustomLeftHandVREnabled(bool isEnabled)
    {
        VE2API.Player.SetCustomLeftHandVREnabled(isEnabled);
    }

    public void OnLeftHandVRPushed()
    {
        VE2API.Player.SetCustomLeftHandVRIndex(0);
    }

    public void OnLeftHandVR2Pushed()
    {
        VE2API.Player.SetCustomLeftHandVRIndex(1);
    }

    public void OnClearButtonPushed()
    {
        VE2API.Player.SetCustomHeadEnabled(false);
        VE2API.Player.SetCustomHeadIndex(0);
    }
}
