using UnityEngine;

[ExecuteAlways]
public class OutlineTag : MonoBehaviour
{
    [Tooltip("Index of the outline color for the object. Edit colors in OutlineRenderer.")]
    [SerializeField] private int colorId;
    [SerializeField, HideInInspector] private OutlineRenderer outlineRenderer;

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

    private void OnEnable()
    {
        outlineRenderer.AddOutlinedObject(this);
    }

    private void OnDisable()
    {
        outlineRenderer.RemoveOutlinedObject(this);
    }

    private void OnValidate()
    {
        if (!outlineRenderer)
            outlineRenderer = FindObjectOfType<OutlineRenderer>();
        colorId = Mathf.Clamp(colorId, 0, OutlineRenderer.ColorsCount);
    }

    private static class PropIDs
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
    }
}
