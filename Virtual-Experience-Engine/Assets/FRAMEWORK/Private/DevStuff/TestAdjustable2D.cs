using UnityEngine;

public class TestAdjustable2D : MonoBehaviour
{
    public void MoveTransform(Vector2 delta)
    {   
        Debug.Log($"Moving transform by delta: {delta}");
        transform.Translate(delta.x * Time.deltaTime,0, delta.y * Time.deltaTime, Space.World);
    }
}
