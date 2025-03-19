using System;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    //Note, these classes contain things that we couldn't stub out in tests (monobehaviours and gameobjects)
    //This is fine - the PlayerService creates the player from a prefab (which contains these references)
    //We don't want to test the internal operation of the player, just that the service as a whole behaves correctly 
    //Therefore, we wouldn't be stubbing thee internal dependencies out anyway
    internal class V_HandVRReferences : MonoBehaviour
    {
        public InteractorVRReferences InteractorVRReferences => _interactorVRReferences;
        [SerializeField] private InteractorVRReferences _interactorVRReferences;

        public DragLocomotorReferences LocomotorVRReferences => _locomotorVRReferences;
        [SerializeField] private DragLocomotorReferences _locomotorVRReferences;

        public TeleporterReferences TeleporterReferences => _teleporterReferences;
        [SerializeField] private TeleporterReferences _teleporterReferences;

        public WristUIReferences WristUIReferences => _wristUIReferences;
        [SerializeField] private WristUIReferences _wristUIReferences;

        //TODO: AnimationController?
        //TODO: Tooltips? 
    }

    [Serializable]
    internal class InteractorVRReferences : InteractorReferences
    {
        public LineRenderer LineRenderer => _lineRenderer;
        [SerializeField, IgnoreParent] private LineRenderer _lineRenderer;

        public V_CollisionDetector CollisionDetector => _collisionDetector;
        [SerializeField, IgnoreParent] private V_CollisionDetector _collisionDetector;

        public GameObject HandVisualGO => _handVisualGO;
        [SerializeField, IgnoreParent] private GameObject _handVisualGO;
    }

    [Serializable]
    public class DragLocomotorReferences
    {
        public GameObject DragIconHolder => _dragIconHolder;
        [SerializeField, IgnoreParent] public GameObject _dragIconHolder; //Entire icon

        public GameObject HorizontalDragIndicator => _horizontalDragIndicator;
        [SerializeField, IgnoreParent] public GameObject _horizontalDragIndicator;

        public GameObject VerticalDragIndicator => _verticalDragIndicator;
        [SerializeField, IgnoreParent] public GameObject _verticalDragIndicator;

        public GameObject SphereDragIcon => _sphereDragIcon;
        [SerializeField, IgnoreParent] public GameObject _sphereDragIcon;
    }

    [Serializable]
    public class TeleporterReferences
    {
        public LineRenderer TeleportLineRenderer => _teleportLineRenderer;
        [SerializeField] private  LineRenderer _teleportLineRenderer;

        public GameObject TeleportCursorPrefab => _teleportCursorPrefab;
        [SerializeField] private  GameObject _teleportCursorPrefab;
    }

    [Serializable]
    public class WristUIReferences
    {
        public Canvas WristUIHolder => _wristUIHolder;
        [SerializeField] private Canvas _wristUIHolder;

        public GameObject Indicator => _indicator;
        [SerializeField] private GameObject _indicator;
    }
}
