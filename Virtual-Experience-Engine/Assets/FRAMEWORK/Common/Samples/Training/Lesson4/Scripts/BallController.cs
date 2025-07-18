using UnityEngine;

public class BallController : MonoBehaviour
{
    public PingPongGameColor PingPongBallColor { get; private set; }

    public void SetupBall(PingPongGameColor color)
    {
        PingPongBallColor = color;
        Renderer renderer = GetComponent<Renderer>();

        // Set the color of the ball based on the game color
        switch (PingPongBallColor)
        {
            case PingPongGameColor.Red:
                renderer.material.color = Color.red;
                break;
            case PingPongGameColor.Green:
                renderer.material.color = Color.green;
                break;
            case PingPongGameColor.Blue:
                renderer.material.color = Color.blue;
                break;
        }

        GetComponent<Rigidbody>().AddForce(transform.forward * 10, ForceMode.Impulse);
    }
}
