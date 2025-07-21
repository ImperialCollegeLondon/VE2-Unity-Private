using UnityEngine;

namespace VE2.Core.VComponents.Internal
{
    public class V_LaserPointer : MonoBehaviour
    {
        #region Inspector Fields


        [SerializeField]
        private GameObject raycastOrigin;

        [SerializeField]
        private GameObject beam;

        [SerializeField]
        private Light pointLight;

        [SerializeField]
        private float beamWidth = 0.01f;

        [SerializeField]
        private float lightIntensity = 1;

        [SerializeField]
        private Color lightColor = Color.red;

        [SerializeField]
        private Color beamEmissionColor = Color.red;

        [SerializeField]
        private Vector2 beamEmissionColorRange = new(6f, 12f);
        [SerializeField] private LayerMask raycastLayers;

        [SerializeField] private GameObject downLaser;
        private bool BeamIsDefined() => beam != null;
        private bool PointLightIsDefined() => pointLight != null;

        #endregion

        private bool isActivated;
        private Renderer beamRenderer, beamRenderer2;
        private readonly int emissionColor = Shader.PropertyToID("_EmissionColor");
        private void Awake()
        {
            if (beam != null) beamRenderer = beam.GetComponent<Renderer>();
            if (downLaser != null) beamRenderer2 = downLaser.GetComponent<Renderer>();
        }

        public void Activate() => isActivated = true;
        public void Deactivate()
        {
            isActivated = false;
            if (beam != null)
            {
                beam.transform.localPosition = new Vector3(0, 0, 0);
                beam.transform.localScale = new Vector3(0.01f, 0f, 0.01f);
            }
            if (pointLight != null)
            {
                pointLight.intensity = 0;
                pointLight.transform.localPosition = new Vector3(0, 0, 0);
            }
        }
        private void Update()
        {
            downLaser.SetActive(false);
            if (!isActivated || raycastOrigin == null) return;
            // Raycast to find what are we pointing at
            if (!Physics.Raycast(raycastOrigin.transform.position, transform.forward, out RaycastHit hit,
                                    Mathf.Infinity, raycastLayers)) return;
            if (beam != null)
            {
                beam.transform.localPosition = new Vector3(0, 0, hit.distance / 2);
                beam.transform.localScale = new Vector3(beamWidth, hit.distance / 2, beamWidth);
            }
            beamRenderer.material.SetColor(emissionColor,
                                            beamEmissionColor * Random.Range(beamEmissionColorRange.x,
                                                                                  beamEmissionColorRange.y));


            if (pointLight != null)
            {
                pointLight.intensity = lightIntensity;
                pointLight.transform.localPosition = new Vector3(0, 0, hit.distance * 0.99f);
                pointLight.color = lightColor;
            }

            //Did we hit map? If so activate the 'down laser' in the correct position
            if (hit.collider.gameObject.name == "MapColliderForLaser")
            {
                Vector3 posInMap = hit.collider.transform.InverseTransformPoint(hit.point);

                Vector3 downLaserPos = new Vector3(posInMap.x, 0, posInMap.y);
                downLaser.SetActive(true);
                downLaser.transform.localPosition = downLaserPos;
                beamRenderer2.material.SetColor(emissionColor,
                                                beamEmissionColor * Random.Range(beamEmissionColorRange.x,
                                                                                      beamEmissionColorRange.y));
            }
        }
    }
}

