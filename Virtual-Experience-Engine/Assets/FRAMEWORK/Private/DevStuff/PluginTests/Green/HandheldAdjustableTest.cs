using UnityEngine;

public class HandleAdjustableTest: MonoBehaviour
{
    public Light torch;
    public void OnColourChanged(float index)
    {
        switch (index)
        {   
            case 0:
                torch.color = Color.white; 
                break;
            case 1:
                torch.color = Color.red;
                break;
            case 2:
                torch.color = Color.blue;
                break;
            case 3:
                torch.color = Color.green;
                break;
            default:
                break;
        }
    }
}
