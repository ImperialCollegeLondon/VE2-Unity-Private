using UnityEngine;

public class LateralMovement : MonoBehaviour
{
    [SerializeField] private float speed = -0.5f;
    private Vector3 _startingPosition;
    private void Awake()
    {
        // Initialize the starting position of the object
        _startingPosition = transform.localPosition;
    }

    // Update is called once per frame
    public void Translate(float value)
    {
        value *= speed;
        float newX = _startingPosition.x + value;
        transform.localPosition = new Vector3(newX, _startingPosition.y, _startingPosition.z);
    }

}
