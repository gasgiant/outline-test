using System;
using System.Collections.Generic;
using UnityEngine;

public class Outlined : MonoBehaviour
{
    public Renderer Renderer => mainRenderer;
    public Material SilhouetteMaterial => silhouetteMaterial;

    private Renderer mainRenderer;

    private Material silhouetteMaterial;

    private void OnEnable()
    {
        if (mainRenderer == null)
        {
            mainRenderer = GetComponent<Renderer>();
            silhouetteMaterial = new Material(Shader.Find("Outline/Silhouette"));
            if (mainRenderer.sharedMaterial.HasProperty("_MainTex"))
                silhouetteMaterial.SetTexture("_MainTex",
                    mainRenderer.sharedMaterial.GetTexture("_MainTex"));
            if (mainRenderer.sharedMaterial.HasProperty("_Cutoff"))
                silhouetteMaterial.SetFloat("_Cutoff",
                    mainRenderer.sharedMaterial.GetFloat("_Cutoff"));
        }
    }
}
