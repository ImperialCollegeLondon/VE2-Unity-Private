using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    [ExecuteAlways]
    internal class V_TeleportAnchor : MonoBehaviour
    {
        [SerializeField, Range(0.75f, 2.5f)] public float Range = 0.75f;

        private LineRenderer _lineRenderer => GetComponent<LineRenderer>();
        private int _segments = 30;

        private bool _editorListenersSetup = false;

        void OnEnable()
        {

#if UNITY_EDITOR
            if (!Application.isPlaying && !_editorListenersSetup)
            {
                _editorListenersSetup = true;
                Selection.selectionChanged += OnSelectionChanged;
            }
#endif

            gameObject.SetActive(!(Application.isPlaying && !VE2API.Player.IsVRMode));
            _lineRenderer.enabled = !Application.isPlaying;
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _editorListenersSetup)
            {
                Selection.selectionChanged -= OnSelectionChanged;
                _editorListenersSetup = false;
            }
#endif
        }

        //FYI THIS DOESNT FIRE EVERY FRAME IN EDITOR MODE
        void Update()
        {
            if (!Application.isPlaying)
                DrawAnchorRange();
        }

        public void DrawAnchorRange()
        {
            _lineRenderer.positionCount = _segments + 1;

            for (int i = 0; i < _segments + 1; i++)
            {
                float angle = i * 2 * Mathf.PI / _segments;

                Vector3 point = new Vector3(Mathf.Cos(angle) * Range, 0.05f, Mathf.Sin(angle) * Range);

                _lineRenderer.SetPosition(i, transform.position + point);
            }
        }

#if UNITY_EDITOR
        private void OnSelectionChanged()
        {
            return;
            // Check if the selected object is the target or a child of it
            foreach (var selected in Selection.gameObjects)
            {
                if (IsChildOrSelf(gameObject, selected))
                {
                    EditorApplication.delayCall += () => Selection.activeGameObject = gameObject;
                    EditorApplication.delayCall += () => EditorApplication.RepaintHierarchyWindow();
                    break;
                }
            }
        }

        private bool IsChildOrSelf(GameObject parent, GameObject obj) => obj == parent || obj.transform.IsChildOf(parent.transform);
#endif
    }
}