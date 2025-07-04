using UnityEngine;

public class GunController : MonoBehaviour
{
    [SerializeField] private GameObject _pingPongBallPrefab;
    [SerializeField] private Transform _spawnPoint;

    public void HandleShoot()
    {
        Debug.Log("Shoot!");

        GameObject ball = Instantiate(_pingPongBallPrefab, _spawnPoint.position, _spawnPoint.rotation);
        BallController ballController = ball.GetComponent<BallController>();
        ballController.SetupBall();
    }
}
