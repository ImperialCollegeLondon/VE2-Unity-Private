using System.Diagnostics;
using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    [ExecuteAlways]
    internal class V_TeleportAnchor : MonoBehaviour
    {
        [SerializeField, Range(0.75f, 2.5f)] public float Range = 0.75f;

        private LineRenderer _lineRenderer => GetComponent<LineRenderer>();
        private int _segments = 30;

        void OnEnable()
        {
            gameObject.SetActive(!(Application.isPlaying && !PlayerAPI.Player.IsVRMode));
            _lineRenderer.enabled = !Application.isPlaying;
        }

        //FYI THIS DOESNT FIRE EVERY FRAME IN EDITOR MODE
        void Update()
        {
            if(!Application.isPlaying)
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
    }
}