using System.Collections.Generic;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal static class CommonUtils
    {
        public static List<Material> GetAvatarColorMaterialsForGameObject(GameObject go) //TODO, move to player?
        {
            List<Material> colorMaterials = new();

            //If we're in edit mode (i.e, a test) just return empty list
            if (!Application.isPlaying)
                return colorMaterials;

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].name.Contains("V_AvatarPrimary"))
                        colorMaterials.Add(renderer.materials[i]);
                }
            }

            return colorMaterials;
        }

        public static void MovePanelToFillRect(RectTransform panelRect, RectTransform targetRect)
        {
            if (panelRect == null)
            {
                Debug.LogError("MovePanelToFillCanvas: panel to move is null");
                return;
            }

            if (targetRect == null)
            {
                Debug.LogError("MovePanelToFillCanvas: target Canvas is null.");
                return;
            }

            // Get the Canvas's RectTransform
            RectTransform canvasRect = targetRect.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Debug.LogError("MovePanelToFillCanvas: Target Canvas does not have a RectTransform.");
                return;
            }

            // Reparent while preserving local position
            panelRect.SetParent(canvasRect, false);

            // Stretch the panel to fill the entire canvas
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
            panelRect.localRotation = Quaternion.identity;

            // Ensure layout updates correctly
            panelRect.ForceUpdateRectTransforms();
        }

        public static GameObject InstantiateResource(string resourceName)
        {
            GameObject resource = Resources.Load<GameObject>(resourceName);

            // Check if the prefab is null
            if (resource == null)
            {
                Debug.LogError("Prefab not found!");
                return null;
            }

            GameObject instantiatedGO = GameObject.Instantiate<GameObject>(resource);
            instantiatedGO.name = "tempName";

            int extraNum = 0;
            string resourceNameShort = resourceName.Contains("/")? resourceName.Substring(resourceName.LastIndexOf("/") + 1) : resourceName;
            string newName = resourceNameShort;

            while (GameObject.Find(newName) != null) 
            {
                extraNum++;
                newName = $"{resourceNameShort}{extraNum}";
            } 

            instantiatedGO.name = newName;

            #if UNITY_EDITOR

           UnityEditor.Selection.activeGameObject = instantiatedGO;

            // Add the instantiation to the Undo buffer
            UnityEditor.Undo.RegisterCreatedObjectUndo(instantiatedGO, "Create " + instantiatedGO.name);

            return instantiatedGO;

            #endif
        }

        public static GameObject FindInChildrenByName(GameObject parent, string targetName)
        {
            foreach (Transform child in parent.transform)
            {
                if (child.name == targetName)
                    return child.gameObject;

                GameObject result = FindInChildrenByName(child.gameObject, targetName);
                if (result != null)
                    return result;
            }

            return null; // Not found
        }

        public static bool IsGameObjectInLayerMask(GameObject go, LayerMask layerMask) => (layerMask.value & (1 << go.layer)) != 0;
    }
}
