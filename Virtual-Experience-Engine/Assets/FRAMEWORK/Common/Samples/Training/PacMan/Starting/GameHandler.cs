using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

public class GameHandler : MonoBehaviour
{
    public static GameHandler instance;

    public Transform mazeTeleportPosition;
    public Transform lobbyTeleportPosition;

    int lives = 3;

    public List<GhostMovement> ghosts = new List<GhostMovement>();

    public UnityEngine.UI.Image panelImage; //goes to end canvas background
    public GameObject gameOver;
    public GameObject levelCleared;
    public TMPro.TMP_Text scoreText;
    public TMP_Text livesText;

    void Start()
    {
        instance = this;
    }

    public void StartGame()
    {
        //TODO - move the player who pushed the button to the mazeTeleportPosition
    }

    public void LoseLife()
    {
        //TODO - only modify if we're host, should then send to a V_NetworkObject
        //We'll also need a receiver method for our V_NetworkObject, which should 
        //receive this num lives, and update the UI, play sounds, and check for game over 
        lives--;

        //TODO - move the player who pushed the button to the lobbyTeleportPosition

        GetComponent<AudioSource>().Play();
        livesText.text = $"Lives: {lives}";

        if (lives == 0)
        {
            GameOver(Scorer.instance.GetScore());
        }
    }

    private void GameOver(int finalScore)
    {
        ShowEndGamePanel();
        scoreText.text = $"Final Score: {finalScore}";
        DOVirtual.DelayedCall(3f, () => gameOver.SetActive(true));
        DOVirtual.DelayedCall(4.5f, () => ShowScore());
    }

    public void LevelCleared(int finalScore)
    {
        ShowEndGamePanel();
        scoreText.text = $"Final Score: {finalScore}";
        DOVirtual.DelayedCall(3f, () => levelCleared.SetActive(true));
        DOVirtual.DelayedCall(4.5f, () => ShowScore());
    }

    private void ShowScore()
    {
        scoreText.gameObject.SetActive(true);
    }

    private void ShowEndGamePanel()
    {
        panelImage.enabled = true;
        //panelImage.DOColor(new Color(.42f, .8f, 1f, .8f), 2f);
        panelImage.color = new Color(.42f, .8f, 1f, .8f);
    }
}
