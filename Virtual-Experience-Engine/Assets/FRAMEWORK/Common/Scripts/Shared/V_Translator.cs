using UnityEngine;

public class V_Translator : MonoBehaviour
{
    public enum MovementAxis
    {
        X,
        Y,
        Z
    }

    [Help("Useful when coupled with adjustables. When 'Tranlate' is called with a value of 1, this transform will translate along the given axis by the distance multiplier")]

    [SerializeField] private float distanceMultiplier = -0.5f;
    [SerializeField] private MovementAxis axis = MovementAxis.X;

    private Vector3 _startingPosition;

    private void Awake()
    {
        _startingPosition = transform.localPosition;
    }

    public void Translate(float value)
    {
        value *= distanceMultiplier;
        Vector3 newPosition = _startingPosition;

        switch (axis)
        {
            case MovementAxis.X:
                newPosition.x += value;
                break;
            case MovementAxis.Y:
                newPosition.y += value;
                break;
            case MovementAxis.Z:
                newPosition.z += value;
                break;
        }

        transform.localPosition = newPosition;
    }
}
