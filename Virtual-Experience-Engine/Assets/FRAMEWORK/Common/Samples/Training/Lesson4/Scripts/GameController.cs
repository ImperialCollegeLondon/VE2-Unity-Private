using System;
using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;

public class GameController : MonoBehaviour
{
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _numBallsShotText;
    [SerializeField] private InterfaceReference<IV_NetworkObject> _networkObject;

    private const float GAME_DURATION = 30;
    private GameData _gameData = new();

    [Serializable] //Must mark this as serializable so it can be synced!!
    private class GameData
    {
        public float TimeLeft;
        public bool IsPlaying;
        public int Score;
        public int NumBallsShot;

        public GameData()
        {
            TimeLeft = GAME_DURATION;
            IsPlaying = false;
            Score = 0;
            NumBallsShot = 0;
        }
    }

    public void HandlePickupGun()
    {
        if (VE2API.InstanceService.IsHost)
        {
            //Start new game, reset data
            if (!_gameData.IsPlaying)
            {
                _gameData.TimeLeft = GAME_DURATION;
                _gameData.IsPlaying = true;
                _gameData.Score = 0;
                _gameData.NumBallsShot = 0;

                _networkObject.Interface.UpdateData(_gameData);
            }
        }
    }

    public void HandleTargetHit(bool colorMatches, float speedMult, float scaleMult)
    {
        if (VE2API.InstanceService.IsHost)
        {
            //Add score. More score for matching colors, and smaller, faster targets
            if (colorMatches)
            {
                _gameData.Score += (int)(10 * speedMult / scaleMult);
            }
            else
            {
                _gameData.Score += (int)(5 * speedMult / scaleMult);
            }

            //Update the game data on the network
            _networkObject.Interface.UpdateData(_gameData);
        }
    }

    public void HandleBallShot()
    {
        if (VE2API.InstanceService.IsHost)
        {
            //Increment the number of balls shot
            _gameData.NumBallsShot++;
            _networkObject.Interface.UpdateData(_gameData);
        }
    }

    //Called on all machines
    public void HandleSyncDataUpdated(object receivedData)
    {
        //To be safe, we only accept incomming data if we are not the host 
        //This prevents the host's data being overwritten by anyone else
        if (!VE2API.InstanceService.IsHost)
        {
            //Cast our received data object back into 
            //our syncable GameData class
            _gameData = (GameData)receivedData;
        }

        //Now that we have an up to date copy of 
        //the game data, update the UI!
        UpdateUI();
    }

    private void Update()
    {
        if (VE2API.InstanceService.IsHost)
        {
            if (_gameData.IsPlaying)
            {
                _gameData.TimeLeft -= Time.deltaTime;

                if (_gameData.TimeLeft <= 0)
                {
                    _gameData.IsPlaying = false;
                }

                _networkObject.Interface.UpdateData(_gameData);
            }
        }
    }

    private void UpdateUI()
    {
        if (!_gameData.IsPlaying && _gameData.TimeLeft <= 0)
        {
            _timerText.text = "Game Over!";
        }
        else
        {
            _timerText.text = ((int)_gameData.TimeLeft).ToString();
        }

        _scoreText.text = _gameData.Score.ToString();

        _numBallsShotText.text = _gameData.NumBallsShot.ToString();
    }
}
