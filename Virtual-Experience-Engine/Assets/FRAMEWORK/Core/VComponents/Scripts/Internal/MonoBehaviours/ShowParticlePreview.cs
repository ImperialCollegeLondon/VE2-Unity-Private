using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShowParticlePreview : MonoBehaviour
{
    [SerializeField] private float startPoint = 0.2f;

    private void OnEnable()
    {
        if (!Application.isPlaying && Application.isEditor)
            GetComponent<ParticleSystem>().Simulate(startPoint);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
