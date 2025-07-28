//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace VE2.Core.VComponents.Shared
{
  public interface IInteractableOutline
  {
    //V_InteractableOutline.Mode OutlineMode { get; set; }
    Color OutlineColor { get; set; }
    float OutlineWidth { get; set; }
  }

  [DisallowMultipleComponent]
  internal class V_InteractableOutline : MonoBehaviour, IInteractableOutline
  {
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode
    {
      OutlineAll,
      OutlineVisible,
      OutlineHidden,
      OutlineAndSilhouette,
      SilhouetteOnly
    }

    public Mode OutlineMode
    {
      get { return outlineMode; }
      set
      {
        outlineMode = value;
        needsUpdate = true;
      }
    }

    public Color OutlineColor
    {
      get { return outlineColor; }
      set
      {
        outlineColor = value;
        needsUpdate = true;
      }
    }

    public float OutlineWidth
    {
      get { return outlineWidth; }
      set
      {
        outlineWidth = value;
        needsUpdate = true;
      }
    }

    [Serializable]
    private class ListVector3
    {
      public List<Vector3> data;
    }

    [SerializeField]
    private Mode outlineMode = Mode.OutlineVisible;

    [SerializeField]
    private Color outlineColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 2.5f;

    [Header("Optional")]

    [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
    + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
    private bool precomputeOutline;

    [SerializeField, HideInInspector]
    private List<Mesh> bakeKeys = new List<Mesh>();

    [SerializeField, HideInInspector]
    private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;

    private bool needsUpdate;

    void Awake()
    {
      // Cache renderers
      renderers = GetComponentsInChildren<Renderer>();

      // Instantiate outline materials
      // Make sure the materials are inside a folder named "Resources" in your Assets directory, e.g.:
      // Assets/FRAMEWORK/Core/VComponents/MatsAndTextures/Resources/OutlineMask.mat
      // Then use the path relative to the Resources folder, without extension:
      outlineMaskMaterial = Instantiate(Resources.Load<Material>("OutlineMask"));
      outlineFillMaterial = Instantiate(Resources.Load<Material>("OutlineFill"));

      if (outlineMaskMaterial == null || outlineFillMaterial == null)
      {
        Debug.LogError("Outline materials not found. Ensure they are placed in a Resources folder and the path is correct.");
      }

      outlineMaskMaterial.name = "OutlineMask";
      outlineFillMaterial.name = "OutlineFill";

      // Retrieve or generate smooth normals
      LoadSmoothNormals();

      // Apply material properties immediately
      needsUpdate = true;
    }

    void OnEnable()
    {
      foreach (var renderer in renderers)
      {
        if (renderer.gameObject.GetComponent<LineRenderer>() != null)
          continue;

        //Trying to operate on a GO that has a VFX on will crash Unity!
        //VFX should be put on a child gameobject instead
        if (renderer.gameObject.GetComponent<VisualEffect>() != null)
          continue;

        // Append outline shaders
        var materials = renderer.sharedMaterials.ToList();

        materials.Add(outlineMaskMaterial);
        materials.Add(outlineFillMaterial);

        renderer.materials = materials.ToArray();
      }
    }

    void OnValidate()
    {

      // Update material properties
      needsUpdate = true;

      // Clear cache when baking is disabled or corrupted
      if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
      {
        bakeKeys.Clear();
        bakeValues.Clear();
      }

      // Generate smooth normals when baking is enabled
      if (precomputeOutline && bakeKeys.Count == 0)
      {
        Bake();
      }
    }

    void Update()
    {
      if (needsUpdate)
      {
        needsUpdate = false;

        UpdateMaterialProperties();
      }
    }

    void OnDisable()
    {
      foreach (var renderer in renderers)
      {

        // Remove outline shaders
        var materials = renderer.sharedMaterials.ToList();

        materials.Remove(outlineMaskMaterial);
        materials.Remove(outlineFillMaterial);

        renderer.materials = materials.ToArray();
      }
    }

    void OnDestroy()
    {

      // Destroy material instances
      Destroy(outlineMaskMaterial);
      Destroy(outlineFillMaterial);
    }

    void Bake()
    {

      // Generate smooth normals for each mesh
      var bakedMeshes = new HashSet<Mesh>();

      foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
      {

        // Skip duplicates
        if (!bakedMeshes.Add(meshFilter.sharedMesh))
        {
          continue;
        }

        // Serialize smooth normals
        var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

        bakeKeys.Add(meshFilter.sharedMesh);
        bakeValues.Add(new ListVector3() { data = smoothNormals });
      }
    }

    void LoadSmoothNormals()
    {

      // Retrieve or generate smooth normals
      foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
      {

        if (meshFilter.sharedMesh == null)
          continue; // skip invalid meshes

        if (!registeredMeshes.Add(meshFilter.sharedMesh))
          continue;

        int index = bakeKeys.IndexOf(meshFilter.sharedMesh);

        List<Vector3> smoothNormals;
        if (index >= 0 && index < bakeValues.Count && bakeValues[index] != null)
        {
          smoothNormals = bakeValues[index].data;
        }
        else
        {
          smoothNormals = SmoothNormals(meshFilter.sharedMesh);
        }

        meshFilter.sharedMesh.SetUVs(3, smoothNormals);

        var renderer = meshFilter.GetComponent<Renderer>();
        if (renderer != null)
        {
          CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
        }

      }

      // Clear UV3 on skinned mesh renderers
      foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
      {

        // Skip if UV3 has already been reset
        if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh))
        {
          continue;
        }

        // Clear UV3
        skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];

        // Combine submeshes
        CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
      }
    }

    List<Vector3> SmoothNormals(Mesh mesh)
    {

      // Group vertices by location
      var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

      // Copy normals to a new list
      var smoothNormals = new List<Vector3>(mesh.normals);

      // Average normals for grouped vertices
      foreach (var group in groups)
      {

        // Skip single vertices
        if (group.Count() == 1)
        {
          continue;
        }

        // Calculate the average normal
        var smoothNormal = Vector3.zero;

        foreach (var pair in group)
        {
          smoothNormal += smoothNormals[pair.Value];
        }

        smoothNormal.Normalize();

        // Assign smooth normal to each vertex
        foreach (var pair in group)
        {
          smoothNormals[pair.Value] = smoothNormal;
        }
      }

      return smoothNormals;
    }

    void CombineSubmeshes(Mesh mesh, Material[] materials)
    {

      // Skip meshes with a single submesh
      if (mesh.subMeshCount == 1)
      {
        return;
      }

      // Skip if submesh count exceeds material count
      if (mesh.subMeshCount > materials.Length)
      {
        return;
      }

      // Append combined submesh
      mesh.subMeshCount++;
      mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }
    static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
    static readonly int OutlineWidthProp = Shader.PropertyToID("_OutlineWidth");

    void UpdateMaterialProperties()
    {
      foreach (var r in renderers)
      {
        if (r == null) continue;

        var mats = r.materials; // per-renderer instances
        for (int i = 0; i < mats.Length; i++)
        {
          // Find the outline fill by comparing shader or name
          if (mats[i].shader == outlineFillMaterial.shader || mats[i].name.Contains("OutlineFill"))
          {
            if (mats[i].HasProperty(OutlineColorProp))
              mats[i].SetColor(OutlineColorProp, outlineColor);

            if (mats[i].HasProperty(OutlineWidthProp))
              mats[i].SetFloat(OutlineWidthProp, outlineWidth);
          }
        }

        // Assign back only if you changed the array structure (here we didn’t),
        // but do it anyway if you want to be explicit:
        r.materials = mats;
      }
    }

  }
}