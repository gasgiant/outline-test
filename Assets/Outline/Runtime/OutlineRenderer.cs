using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[ExecuteAlways]
public class OutlineRenderer : MonoBehaviour
{
    [Tooltip("If enabled, outline won't be rendered for occluded geometry.")]
    [SerializeField] bool occlusion;
    [Tooltip("If enabled, outline will disappear behind occluders.")]
    [SerializeField] bool depthTest;
    [Tooltip("Outline width in percents of screen height.")]
    [SerializeField, Range(0, 3)] private float width = 1;
    [SerializeField, Range(0, 1)] private float softness;
    [Tooltip("Colors of the outline. Up to 16 colors supported.")]
    [SerializeField] private Color[] colors = { Color.red };

    [SerializeField, HideInInspector] private ComputeShader jumpFloodShader;
    [SerializeField, HideInInspector] private Shader outlineShader;
    // need it to be serialized, so shader doesn't get stripped out of the build
    [SerializeField, HideInInspector] private Shader silhouetteShader;

    public void AddOutlinedObject(OutlineTag obj)
    {
        if (!outlinedObjects.Contains(obj))
            outlinedObjects.Add(obj);
    }

    public void RemoveOutlinedObject(OutlineTag obj)
    {
        if (!outlinedObjects.Contains(obj))
            outlinedObjects.Add(obj);
    }

    public static float PixelWidth(float width, float resolution) => width * resolution * 0.01f;
    public static int JumpFloodIterations(float pixelWidth)
        => Mathf.CeilToInt(Mathf.Log(pixelWidth + 1f, 2f));

    public const string DepthTestKeyword = "OUTLINE_DEPTH_TEST";
    public const string SilhouetteShaderName = "Hidden/Outline/Silhouette";
    public const string OutlineShaderName = "Hidden/Outline/Outline";
    public const int ColorsCount = 16;
    private const CameraEvent cameraEvent = CameraEvent.BeforeForwardAlpha;

    private Camera cam;
    private List<OutlineTag> outlinedObjects = new List<OutlineTag>();
    private System.Comparison<OutlineTag> distanceCompareFunc;

    private CommandBuffer cmd;
    private RenderTexture silhouetteBuffer;
    private RenderTexture jumpFloodBuffer0;
    private RenderTexture jumpFloodBuffer1;
    private Material outlineMaterial;
    private Vector4[] colorsArray = new Vector4[ColorsCount];
    private float pixelWidth;

    private int JfaInitKernerlID;
    private int JfaStepKernerlID;

    private void OnEnable()
    {
        Initialize();
        Camera.onPreRender += ApplyCommandBuffer;
        Camera.onPostRender += RemoveCommandBuffer;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= ApplyCommandBuffer;
        Camera.onPostRender -= RemoveCommandBuffer;
    }

    private void OnDestroy()
    {
        if (silhouetteBuffer) silhouetteBuffer.Release();
        if (jumpFloodBuffer0) jumpFloodBuffer0.Release();
        if (jumpFloodBuffer1) jumpFloodBuffer1.Release();
    }

    private void Initialize()
    {
        cmd = new CommandBuffer();
        cmd.name = "Outline";

        outlineMaterial = new Material(outlineShader);
        JfaInitKernerlID = jumpFloodShader.FindKernel("Init");
        JfaStepKernerlID = jumpFloodShader.FindKernel("JumpFloodStep");

        distanceCompareFunc = (x, y) =>
        {
            float dz = Vector3.Dot((x.transform.position - y.transform.position), cam.transform.forward);

            if (dz == 0) return 0;
            return dz < 0 ? 1 : -1;
        };
    }

    private void ApplyCommandBuffer(Camera cam)
    {
#if UNITY_EDITOR
        if (cam.gameObject.name == "Preview Scene Camera")
            return;
#endif

        if (this.cam != null)
        {
            if (this.cam == cam)
            {
                SetupRender();
                return;
            }
            else
                RemoveCommandBuffer(cam);
        }

        this.cam = cam;
        SetupRender();

        SetupCommandBuffer(cmd);
        cam.AddCommandBuffer(cameraEvent, cmd);

    }

    private void RemoveCommandBuffer(Camera cam)
    {
        if (cam != null && cmd != null)
        {
            cam.RemoveCommandBuffer(cameraEvent, cmd);
            this.cam = null;
        }
    }

    private void SetupRender()
    {
        pixelWidth = PixelWidth(width, cam.pixelHeight);
        outlinedObjects.Sort(distanceCompareFunc);
        InitRenderTextures(cam.pixelWidth, cam.pixelHeight);
        SetMaterialProperties();
    }

    private void SetMaterialProperties()
    {
        outlineMaterial.SetFloat(PropIDs.OutlinePixelWidth, pixelWidth);
        outlineMaterial.SetFloat(PropIDs.OutlineSoftness, softness);
        if (depthTest)
            outlineMaterial.EnableKeyword(DepthTestKeyword);
        else
            outlineMaterial.DisableKeyword(DepthTestKeyword);

        for (int i = 0; i < Mathf.Min(ColorsCount, colors.Length); i++)
        {
            colorsArray[i] = colors[i].linear;
        }

        outlineMaterial.SetVectorArray(PropIDs.OutlineColors, colorsArray);
    }

    private void SetupCommandBuffer(CommandBuffer cmd)
    {
        cmd.Clear();
        RenderSilhouette(cmd);
        DoJumpFlood(cmd, JumpFloodIterations(pixelWidth));
        
        cmd.Blit(jumpFloodBuffer0, BuiltinRenderTextureType.CameraTarget, outlineMaterial);
    }

    private void RenderSilhouette(CommandBuffer cmd)
    {
        if (occlusion)
        {
            cmd.SetRenderTarget(silhouetteBuffer.colorBuffer, BuiltinRenderTextureType.Depth);
            cmd.ClearRenderTarget(false, true, Color.clear);
        }
        else
        {
            cmd.SetRenderTarget(silhouetteBuffer);
            cmd.ClearRenderTarget(true, true, Color.clear);
        }
        
        for (int i = outlinedObjects.Count - 1; i >= 0; i--)
        {
            cmd.SetGlobalFloat(PropIDs.ObjectID, (i + 1f) / outlinedObjects.Count);
            cmd.SetGlobalFloat(PropIDs.ColorID, outlinedObjects[i].ColorID / (float)ColorsCount);
            cmd.DrawRenderer(outlinedObjects[i].Renderer, outlinedObjects[i].SilhouetteMaterial);
        }
    }

    private void DoJumpFlood(CommandBuffer cmd, int passes)
    {
        bool startBuff = passes % 2 == 0;
        RenderTexture bf0 = startBuff ? jumpFloodBuffer0 : jumpFloodBuffer1;
        RenderTexture bf1 = startBuff ? jumpFloodBuffer1 : jumpFloodBuffer0;

        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, PropIDs.Silhouette, silhouetteBuffer);
        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, PropIDs.Output, bf0);
        cmd.DispatchCompute(jumpFloodShader, JfaInitKernerlID,
            ThreadGroupsCount(silhouetteBuffer.width), ThreadGroupsCount(silhouetteBuffer.height), 1);

        bool pingPong = false;
        cmd.SetComputeIntParam(jumpFloodShader, PropIDs.TargetWidth, silhouetteBuffer.width);
        cmd.SetComputeIntParam(jumpFloodShader, PropIDs.TargetHeight, silhouetteBuffer.height);
        cmd.SetComputeFloatParam(jumpFloodShader, PropIDs.OutlineWidthSquared, pixelWidth * pixelWidth);
        cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, PropIDs.Silhouette, silhouetteBuffer);

        for (int i = 0; i < passes; i++)
        {
            int stepSize = Mathf.RoundToInt(Mathf.Pow(2, passes - 1 - i));
            cmd.SetComputeIntParam(jumpFloodShader, PropIDs.StepWidth, stepSize);
            cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, 
                PropIDs.Input, pingPong ? bf1 : bf0);
            cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, 
                PropIDs.Output, pingPong ? bf0 : bf1);

            cmd.DispatchCompute(jumpFloodShader, JfaStepKernerlID,
                ThreadGroupsCount(silhouetteBuffer.width), ThreadGroupsCount(silhouetteBuffer.height), 1);
            pingPong = !pingPong;
        }
    }

    private void InitRenderTextures(int width, int height)
    {
        if (silhouetteBuffer == null 
            || silhouetteBuffer.height != height
            || silhouetteBuffer.width != width)
        {
            if (silhouetteBuffer) silhouetteBuffer.Release();
            if (jumpFloodBuffer0) jumpFloodBuffer0.Release();
            if (jumpFloodBuffer1) jumpFloodBuffer1.Release();

            var silhouetteDescriptor = new RenderTextureDescriptor(width, height)
            {
                dimension = TextureDimension.Tex2D,
                graphicsFormat = GraphicsFormat.R8G8_UNorm,

                msaaSamples = 1,
                depthBufferBits = 16,

                sRGB = false,

                useMipMap = false,
                autoGenerateMips = false
            };

            silhouetteBuffer = new RenderTexture(silhouetteDescriptor);
            silhouetteBuffer.Create();

            var jfaDescriptor = silhouetteDescriptor;
            jfaDescriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            jfaDescriptor.enableRandomWrite = true;
            jumpFloodBuffer0 = new RenderTexture(jfaDescriptor);
            jumpFloodBuffer1 = new RenderTexture(jfaDescriptor);
            jumpFloodBuffer0.Create();
            jumpFloodBuffer1.Create();
        }
    }

    private static int ThreadGroupsCount(int threadsCount) => Mathf.CeilToInt(threadsCount / 8);

    private static class PropIDs
    {
        public static readonly int Silhouette = Shader.PropertyToID("Silhouette");
        public static readonly int Input = Shader.PropertyToID("Input");
        public static readonly int Output = Shader.PropertyToID("Output");
        public static readonly int TargetWidth = Shader.PropertyToID("TargetWidth");
        public static readonly int TargetHeight = Shader.PropertyToID("TargetHeight");
        public static readonly int StepWidth = Shader.PropertyToID("StepWidth");
        public static readonly int OutlineWidthSquared = Shader.PropertyToID("OutlineWidthSquared");


        public static readonly int OutlinePixelWidth = Shader.PropertyToID("_OutlinePixelWidth");
        public static readonly int OutlineSoftness = Shader.PropertyToID("_OutlineSoftness");
        public static readonly int OutlineColors = Shader.PropertyToID("_OutlineColors");
        public static readonly int ObjectID = Shader.PropertyToID("_ObjectID");
        public static readonly int ColorID = Shader.PropertyToID("_ColorID");


    }
}
