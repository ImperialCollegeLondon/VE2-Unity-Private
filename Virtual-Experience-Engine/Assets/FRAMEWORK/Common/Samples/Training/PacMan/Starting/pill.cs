using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.VComponents.API;

public class Pill : MonoBehaviour
{
    public bool isPowerPill=false;

    private void Start()
    {
        Scorer.instance.RegisterPill();
    }

    //TODO - needs to be triggered on collision with the player... maybe use a V_PressurePlateActivatable?
    //Needs a collider!
    public void PlayerPickup()
    {
        gameObject.SetActive(false);
        if (isPowerPill)
        {
            Scorer.instance.AddScore(200);
            Scorer.instance.StartPowerPill();
            Scorer.instance.PlaySoundPowerPill();
        }
        else
        {
            Scorer.instance.AddScore(50);
            Scorer.instance.PlaySoundChomp();
        }

        Scorer.instance.IncPillCount();
    }

    private void Update()
    {
        if (isPowerPill)
        {
            float scale = (Mathf.Sin(Time.time * 7f) + 2f)/5f;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
