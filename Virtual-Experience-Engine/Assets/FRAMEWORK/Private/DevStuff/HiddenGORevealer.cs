using UnityEngine;

public class HiddenGORevealer : MonoBehaviour
{
    [EditorButton(nameof(RevealHiddenGameObjects), "Reveal Hidden GameObjects")]
    [SerializeField, Disable]
    private bool _bodge = true; // This is a bodge to give us the button in the inspector

    private void RevealHiddenGameObjects()
    {
        // Find all GameObjects in the scene
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>(true);

        // Iterate through each GameObject
        foreach (GameObject go in allGameObjects)
        {
            go.hideFlags = HideFlags.None; // Remove any hide flags
        }
    }
}
