using UnityEngine;

public class BallController : MonoBehaviour
{
    public void SetupBall()
    {
        GetComponent<Rigidbody>().AddForce(transform.forward * 10, ForceMode.Impulse);
    }
}
