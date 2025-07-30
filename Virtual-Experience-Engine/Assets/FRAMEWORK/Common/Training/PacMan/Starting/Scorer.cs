using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Scorer : MonoBehaviour
{
    public static Scorer instance;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    private int totalScore = 0;
    public void AddScore(int score)
    {
        //TODO - only modify if we're host, should then send to a V_NetworkObject
        //We'll also need a receiver method for our V_NetworkObject, which should 
        //receive this score, and update the UI
        totalScore += score;
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
