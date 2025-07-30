using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _numBallsShotText;

    private const float GAME_DURATION = 30;
    private GameData _gameData = new();

    //Holding all this game data in a separate object, you'll see why next lesson!
    private class GameData
    {
        public float TimeLeft;
        public bool IsPlaying;
        public int NumBallsShot;

        public GameData()
        {
            TimeLeft = GAME_DURATION;
            IsPlaying = false;
            NumBallsShot = 0;
        }
    }

    private void Start()
    {
        _gameData.TimeLeft = GAME_DURATION;
        UpdateUI();
    }

    public void HandlePickupGun()
    {
        //Start new game, reset data
        if (!_gameData.IsPlaying)
        {
            _gameData.NumBallsShot = 0;
            _gameData.TimeLeft = GAME_DURATION;
            _gameData.IsPlaying = true;
            UpdateUI();
        }
    }
    
    public void HandleBallShot()
    {
        _gameData.NumBallsShot++;
    }

    private void Update()
    {
        if (_gameData.IsPlaying)
        {
            _gameData.TimeLeft -= Time.deltaTime;

            if (_gameData.TimeLeft <= 0)
            {
                _gameData.IsPlaying = false;
            }

            UpdateUI();
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

        _numBallsShotText.text = _gameData.NumBallsShot.ToString();
    }
}
