using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;

public class Scorer : MonoBehaviour
{
    [SerializeField] private InterfaceReference<IV_NetworkObject> _networkObject;

    public static Scorer instance;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    private int totalScore = 0;
    public void AddScore(int score)
    {
        if (VE2API.InstanceService.IsHost)
        {
            totalScore += score;
            _networkObject.Interface.UpdateData(totalScore);
        }
        
    }

    public void HandleSyncDataUpdated(object obj)
    {
        int newScore = (int)obj;

        if (!VE2API.InstanceService.IsHost)
        {
            totalScore = newScore;
        }

        GetComponent<TMP_Text>().text = $"Score: {totalScore}";
    }

    public GhostMovement[] ghosts;
    public void StartPowerPill()
    {
        foreach (var g in  ghosts) { g.SetModeToPowerPill(); }
    }

    public AudioSource chomp, powerpill;
    public void PlaySoundChomp()
    {
        chomp.Play();
    }

    public void PlaySoundPowerPill()
    {
        powerpill.Play();
    }

    internal int GetScore()
    {
        return totalScore;
    }

    int pillcount = 0;
    int pillCountToWin = 0;
    public void IncPillCount()
    {
        pillcount++;
        
        if (pillcount==pillCountToWin)
        {
            GameHandler.instance.LevelCleared(totalScore);
        }
    }

    internal void RegisterPill()
    {
        pillCountToWin++;
    }

}
