using DG.Tweening;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class PlayerHandLightHandler : MonoBehaviour
    {
        [SerializeField] private Renderer _handRenderer;
        [SerializeField] private int _materialIndex;

        [SerializeField] private float _minOffsetUpdateTime;
        [SerializeField] private float _maxOffsetUpdateTime;
        private float _timeOfNextCycle = 0;

        [SerializeField] private float _brightnessCycleDuration;
        [SerializeField] private Color _emissiveColor;
        [SerializeField] private float _maxEmissiveIntensity;
        [SerializeField] private float _minEmissiveIntensity;   

        private Material _handMaterial => _handRenderer.materials[_materialIndex];

        private void Awake()
        {
            _handMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            _handMaterial.EnableKeyword("_EMISSION");
        }

        private void Update()
        {
            if (Time.time > _timeOfNextCycle)
            {
                _timeOfNextCycle = Time.time + Random.Range(_minOffsetUpdateTime, _maxOffsetUpdateTime);

                //Randomize an offset for the texture
                _handMaterial.mainTextureOffset = new Vector2(Random.Range(1000f, -1000f) / 1000f, Random.Range(1000f, -1000f) / 1000f);
                _handMaterial.mainTexture = _handMaterial.mainTexture;
            }

            float currentIntensity = Mathf.Max(0, Mathf.Sin(Time.time / _brightnessCycleDuration) + 0.25f);
            Color currentColor = _emissiveColor * Mathf.Lerp(_minEmissiveIntensity, _maxEmissiveIntensity, currentIntensity);

            _handMaterial.SetColor("_EmissionColor", currentColor);
        }
    }
}
