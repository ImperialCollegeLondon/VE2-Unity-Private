#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace VE2.VComponents.Internal 
{
    public class VComponentsEditorMenu
    {
        [MenuItem("/GameObject/VE2/ToggleButton", priority = 1)]
        private static void CreateToggleButton()
        {
            InstantiateVCResource("ToggleButton");
        }

        private static void InstantiateVCResource(string resourceName)
        {
            GameObject vcResource = Resources.Load<GameObject>(resourceName);

            // Check if the prefab is null
            if (vcResource == null)
            {
                Debug.LogError("Prefab not found!");
                return;
            }

            GameObject instantiatedVC = GameObject.Instantiate<GameObject>(vcResource);
            instantiatedVC.name = "tempName";

            int extraNum = 0;
            string newName = resourceName;

            while (GameObject.Find(newName) != null) 
            {
                extraNum++;
                newName = $"{resourceName}{extraNum}";
            } 

            instantiatedVC.name = newName;

            Selection.activeGameObject = instantiatedVC;

            // Add the instantiation to the Undo buffer
            Undo.RegisterCreatedObjectUndo(instantiatedVC, "Create " + instantiatedVC.name);
        }
    }
}
#endif