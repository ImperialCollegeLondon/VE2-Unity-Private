using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;

public class Q_Pointer : MonoBehaviour
{
    [SerializeField] private Transform origin;
    [SerializeField] private float maxRange;
    [SerializeField] private GameObject beam;
    private IV_FreeGrabbable grabbable;
    private Material beamMaterial;
    public Transform Origin { get => origin;  }
    public float MaxRange { get => maxRange; }
    public bool IsGrabbed { get => grabbed; }
    public bool IsGrabbedLocally { get => grabbed
            && grabbable.MostRecentInteractingClientID.Value == VE2API.InstanceService.LocalClientID; }

    private bool hovering = false;
    float hoverAmount = 0f;
    [SerializeField][ColorUsage(true, true)] private Color emitColourPointingAtSomething, emitColourNormal;
    private bool grabbed = false;

    private void Start()
    {
        grabbable = GetComponent<IV_FreeGrabbable>();
        beamMaterial = beam.GetComponent<MeshRenderer>().material;
        grabbable.OnGrab.AddListener(Grabbed);
        grabbable.OnDrop.AddListener(Dropped);
        Dropped();

    }

    private void Dropped()
    {
        grabbed = false;
        beam.SetActive(false);
    }

    private void Grabbed()
    {
        grabbed = true;
        beam.SetActive(true);
    }

    public void SetHover()
    {
        hovering = true;
    }
    private void LateUpdate()
    {
        if (hovering)
        {
            hoverAmount += Time.deltaTime* 16f;
        }
        else
        {
            hoverAmount -= Time.deltaTime * 6;
        }
        hovering = false;
        hoverAmount = Mathf.Clamp01(hoverAmount);

        beamMaterial.SetColor("_EmissionColor", Color.Lerp(emitColourNormal, emitColourPointingAtSomething, hoverAmount));

        float beamLength = maxRange;
        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit info, maxRange))
            beamLength = Vector3.Distance(info.point, origin.position);

        beam.transform.position = origin.position + origin.forward * beamLength / 2f;
        beam.transform.localScale = new Vector3(beam.transform.localScale.x,
            beamLength/2f, //because cylinder is 2 units long
            beam.transform.localScale.z);
    }
}
