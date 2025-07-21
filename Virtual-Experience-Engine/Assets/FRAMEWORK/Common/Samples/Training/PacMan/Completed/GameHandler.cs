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
    [SerializeField] private InterfaceReference<IV_NetworkObject> _networkObject;
    [SerializeField] private InterfaceReference<IV_HoldActivatable> _startButton;

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
        if (_startButton.Interface.MostRecentInteractingClientID.IsLocal)
        {
            VE2API.Player.SetPlayerPosition(mazeTeleportPosition.position);
            VE2API.Player.SetPlayerRotation(lobbyTeleportPosition.rotation);    
        }
    }

    public void LoseLife()
    {
        if (VE2API.InstanceService.IsHost)
        {
            lives--;
            _networkObject.Interface.UpdateData(lives);
        }

        if (_startButton.Interface.MostRecentInteractingClientID.IsLocal)
        {
            VE2API.Player.SetPlayerPosition(lobbyTeleportPosition.position);
            VE2API.Player.SetPlayerRotation(lobbyTeleportPosition.rotation);
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

    public void HandleSyncDataUpdated(object obj)
    {
        int newLives = (int)obj;

        if (newLives < lives)
        {
            GetComponent<AudioSource>().Play();
        }

        if (!VE2API.InstanceService.IsHost)
            lives = (int)obj;

        livesText.text = $"Lives: {lives}";

        if (lives == 0)
        {
            GameOver(Scorer.instance.GetScore());
        }
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
