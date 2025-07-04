using TMPro;
using UnityEngine;
using VE2.Core.VComponents.API;

public class GunController : MonoBehaviour
{
    [SerializeField] private GameController _gameController;
    [SerializeField] private GameObject _pingPongBallPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private TMP_Text _colorText;

    public void HandleShoot()
    {
        PingPongGameColor ballColor = ConvertFloatToColor(GetComponent<IV_HandheldAdjustable>().Value);

        Debug.Log($"Shoot! ball color: {ballColor}");

        GameObject ball = Instantiate(_pingPongBallPrefab, _spawnPoint.position, _spawnPoint.rotation);
        BallController ballController = ball.GetComponent<BallController>();
        ballController.SetupBall(ballColor);

        _gameController.HandleBallShot();
    }

    public void HandleAdjustColor(float newValue)
    {
        // Update the color text based on the adjustable value
        PingPongGameColor color = ConvertFloatToColor(newValue);
        
        switch (color)
        {
            case PingPongGameColor.Red:
                _colorText.text = "Red";
                _colorText.color = Color.red;
                break;
            case PingPongGameColor.Green:
                _colorText.text = "Green";
                _colorText.color = Color.green;
                break;
            case PingPongGameColor.Blue:
                _colorText.text = "Blue";
                _colorText.color = Color.cyan;
                break;
            default:
                _colorText.text = "Error";
                _colorText.color = Color.white;
                break;
        }
    }

    private PingPongGameColor ConvertFloatToColor(float value)
    {
        //(Assuming the adjustable is on this gameobject)
        //The adjustable gives us a float, so first cast to int, then to the enum
        return (PingPongGameColor)(int)value;
    }
}
