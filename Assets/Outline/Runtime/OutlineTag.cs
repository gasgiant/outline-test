using UnityEngine;

public class OutlineTag : MonoBehaviour
{
    [SerializeField] private int colorId;

    public int ColorID => colorId;

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
                if (Renderer.sharedMaterial.HasProperty(PropIDs.MainTex))
                    silhouetteMaterial.SetTexture(PropIDs.MainTex,
                        Renderer.sharedMaterial.GetTexture(PropIDs.MainTex));
                if (Renderer.sharedMaterial.HasProperty(PropIDs.Cutoff))
                    silhouetteMaterial.SetFloat(PropIDs.Cutoff,
                        Renderer.sharedMaterial.GetFloat(PropIDs.Cutoff));
            }
            return silhouetteMaterial;
        }
    }

    private static Shader silhouetteShader;
    private Renderer mainRenderer;
    private Material silhouetteMaterial;

    private static class PropIDs
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
    }
}
