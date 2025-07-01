#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal 
{
    internal class VComponentsEditorMenu
    {
        [MenuItem("/GameObject/VE2/Interactables/ToggleButton", priority = 0)]
        private static void CreateToggleButton()
        {
            CommonUtils.InstantiateResource("ToggleButton");
        }

        [MenuItem("/GameObject/VE2/Interactables/HoldButton", priority = 1)]
        private static void CreateHoldButton()
        {
            CommonUtils.InstantiateResource("HoldButton");
        }

        [MenuItem("/GameObject/VE2/Interactables/PressurePlate", priority = 2)]
        private static void CreatePressurePlate()
        {
            CommonUtils.InstantiateResource("PressurePlate");
        }

        [MenuItem("/GameObject/VE2/Interactables/AdjustableWheel", priority = 3)]
        private static void CreateWheel()
        {
            CommonUtils.InstantiateResource("AdjustableWheel");
        }

        [MenuItem("/GameObject/VE2/Interactables/AdjustableLever", priority = 4)]
        private static void CreateLever()
        {
            CommonUtils.InstantiateResource("AdjustableLever");
        }

        [MenuItem("/GameObject/VE2/Interactables/AdjustableSlider", priority = 5)]
        private static void CreateSlider()
        {
            CommonUtils.InstantiateResource("AdjustableSlider");
        }

        [MenuItem("/GameObject/VE2/Interactables/GrabbableBox", priority = 6)]
        private static void CreateGrabbableBox()
        {
            CommonUtils.InstantiateResource("GrabbableBox");
        }

        [MenuItem("/GameObject/VE2/Interactables/GrabbableShooter", priority = 6)]
        private static void CreateGrabbableShooter()
        {
            CommonUtils.InstantiateResource("GrabbableShooter");
        }

        [MenuItem("/GameObject/VE2/UIs/CustomInfoPoint", priority = 6)]
        private static void CreateCustomInfoPoint()
        {
            GameObject infoPoint = CommonUtils.InstantiateResource("CustomInfoPoint");

            GameObject trigger = infoPoint.GetComponentInChildren<InfoPointTriggerAnimationHandler>().gameObject;
            trigger.name = $"{infoPoint.name}_Trigger"; //The actual trigger holds the activatable!

            GameObject canvas = infoPoint.GetComponentInChildren<InfoPointCanvasAnimationHandler>().gameObject;
            canvas.name = $"{infoPoint.name}_Canvas"; //May as well do the same to the canvas, for consistency
        }

        [MenuItem("/GameObject/VE2/Models/TableA", priority = 0)]
        private static void CreateTableA()
        {
            CommonUtils.InstantiateResource("TableA");
        }

        [MenuItem("/GameObject/VE2/Models/TableB", priority = 1)]
        private static void CreateTableB()
        {
            CommonUtils.InstantiateResource("TableB");
        }

        [MenuItem("/GameObject/VE2/Models/ButtonStand", priority = 2)]
        private static void CreateButtonStand()
        {
            CommonUtils.InstantiateResource("ButtonStand");
        }
        
        [MenuItem("/GameObject/VE2/Models/TrainingShooter", priority = 2)]
        private static void CreateTrainingShooter()
        {
            CommonUtils.InstantiateResource("TrainingShooter");
        }
    }
}
#endif