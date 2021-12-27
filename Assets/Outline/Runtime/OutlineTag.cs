using UnityEngine;

public class OutlineTag : MonoBehaviour
{
    [SerializeField] private int colorId;

    public Renderer Renderer
    {
        get
        { 
            if (mainRenderer == null)
            {
                mainRenderer = GetComponent<Renderer>();
                if (mainRenderer == null)
                    mainRenderer = GetComponentInChildren<Renderer>();
            }
            return mainRenderer;
        }
    }
    public Material SilhouetteMaterial
    {
        get
        { 
            if (silhouetteMaterial == null)
            {
                if (silhouetteShader == null)
                    silhouetteShader = Shader.Find(OutlineRenderer.SilhouetteShaderName);
                silhouetteMaterial = new Material(silhouetteShader);
                if (Renderer.sharedMaterial.HasProperty(PropIds.MainTex))
                    silhouetteMaterial.SetTexture(PropIds.MainTex,
                        Renderer.sharedMaterial.GetTexture(PropIds.MainTex));
                if (Renderer.sharedMaterial.HasProperty(PropIds.Cutoff))
                    silhouetteMaterial.SetFloat(PropIds.Cutoff,
                        Renderer.sharedMaterial.GetFloat(PropIds.Cutoff));
            }
            return silhouetteMaterial;
        }
    }

    private static Shader silhouetteShader;
    private Renderer mainRenderer;
    private Material silhouetteMaterial;

    private static class PropIds
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
    }
}
