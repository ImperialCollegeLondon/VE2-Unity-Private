using UnityEngine;
using VE2.Common.API;
using VE2.Core.Player.API;

public class HubTestAvatarCustomisation : MonoBehaviour
{
    IPlayerServiceInternal PlayerServiceInternal => VE2API.Player as IPlayerServiceInternal;

    public void OnBuiltInHeadOneButtonPushed()
    {
        PlayerServiceInternal.SetBuiltInHeadIndex(0);
    }

    public void OnBuiltInHeadTwoButtonPushed()
    {
        PlayerServiceInternal.SetBuiltInHeadIndex(1);
    }

    public void OnBuiltInTorsoOneButtonPushed()
    {
        PlayerServiceInternal.SetBuiltInTorsoIndex(0);
    }

    public void OnBuiltInTorsoTwoButtonPushed()
    {
        PlayerServiceInternal.SetBuiltInTorsoIndex(1);
    }

    public void OnRedColorPressed()
    {
        PlayerServiceInternal.SetBuiltInColor(Color.red);
    }

    public void OnGreenColorPressed()
    {
        PlayerServiceInternal.SetBuiltInColor(Color.green);
    }

    public void OnBlueColorPressed()
    {
        PlayerServiceInternal.SetBuiltInColor(Color.blue);
    }
}
