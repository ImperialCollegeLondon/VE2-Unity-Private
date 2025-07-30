using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Diagnostics.Contracts;

/// <summary>
/// You cannot use particle systems on screen-space overlay canvasses annoyingly
/// So this implements a very simple alternative using UI elements
/// </summary>
public class Q_FakeParticleController : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] int particleCount = 50;
    [SerializeField] GameObject particlePrototype;
    [SerializeField] CanvasGroup canvasGroup;

    List<RectTransform> particles = new List<RectTransform>();
    void Start()
    {
        for (int i=0; i<particleCount; i++)
        {
            GameObject go = Instantiate(particlePrototype, transform);
            particles.Add(go.GetComponent<RectTransform>());

            InitialiseParticle(go);
        }
    }

    private void InitialiseParticle(GameObject go)
    {
        go.GetComponent<RawImage>().color = new Color(UnityEngine.Random.Range(.5f, 1f), UnityEngine.Random.Range(.5f, 1f), UnityEngine.Random.Range(.5f, 1f));
        float scale = UnityEngine.Random.Range(.5f, 1.2f);
        go.GetComponent<RectTransform>().localScale = new Vector3(scale,scale,scale);
    }

    public void Burst()
    {
        DOVirtual.Float(1f, 1f, .25f, (float v) => canvasGroup.alpha = v);
        foreach (RectTransform rt in particles)
        {
            rt.gameObject.SetActive(true);
            rt.DOLocalMove(UnityEngine.Random.insideUnitCircle * 130f, 1f);
            rt.DOScale(Vector3.one / 100f, 1f);
            rt.DOLocalRotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-500f, 500f)), 1f);
        }
    }
}
