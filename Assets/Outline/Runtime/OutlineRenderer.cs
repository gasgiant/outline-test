using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

//[ExecuteAlways]
public class OutlineRenderer : MonoBehaviour
{
    [SerializeField, Range(0, 50)] private float pixelWidth = 2;
    [SerializeField, Range(0, 1)] private float softness;
    [SerializeField] private Color[] colors = { Color.red };

    [SerializeField, HideInInspector] private ComputeShader jumpFloodShader;
    [SerializeField, HideInInspector] private Shader outlineShader;
    // need it to be serialized, so shader doesn't get stripped out of the build
    [SerializeField, HideInInspector] private Shader silhouetteShader;

    public const string SilhouetteShaderName = "Hidden/Outline/Silhouette";
    public const string OutlineShaderName = "Hidden/Outline/Outline";
    public static int JumpFloodIterations(float outlinePixelWidth)
        => Mathf.CeilToInt(Mathf.Log(outlinePixelWidth + 1.0f, 2f));

    private Camera cam;
    private List<OutlineTag> outlinedObjects;
    private System.Comparison<OutlineTag> distanceCompareFunc;

    private CommandBuffer cmd;
    private RenderTexture silhouetteBuffer;
    private RenderTexture jumpFloodBuffer0;
    private RenderTexture jumpFloodBuffer1;
    private Material outlineMaterial;

    private const CameraEvent cameraEvent = CameraEvent.BeforeForwardAlpha;
    private readonly int SilhouetteParamId = Shader.PropertyToID("Silhouette");
    private readonly int DepthParamId = Shader.PropertyToID("Depth");
    private readonly int InputParamId = Shader.PropertyToID("Input");
    private readonly int OutputParamId = Shader.PropertyToID("Output");

    private int JfaInitKernerlID;
    private int JfaStepKernerlID;

    private void OnEnable()
    {
        Camera.onPreRender += ApplyCommandBuffer;
        Camera.onPostRender += RemoveCommandBuffer;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= ApplyCommandBuffer;
        Camera.onPostRender -= RemoveCommandBuffer;
    }

    private void Awake()
    {
        outlinedObjects = new List<OutlineTag>();
        var found = FindObjectsOfType<OutlineTag>();
        foreach (var outlined in found)
        {
            outlinedObjects.Add(outlined);
        }

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

    private void OnDestroy()
    {
        silhouetteBuffer?.Release();
        jumpFloodBuffer0?.Release();
        jumpFloodBuffer1?.Release();
    }

    private void Update()
    {
        if (cam != null)
            outlinedObjects.Sort(distanceCompareFunc);

        outlineMaterial.SetFloat("_Width", pixelWidth);
        outlineMaterial.SetFloat("_Softness", softness);
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
                return;
            else
                RemoveCommandBuffer(cam);
        }

        this.cam = cam;
        InitRenderTextures(cam.pixelWidth, cam.pixelHeight);
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

    private void SetupCommandBuffer(CommandBuffer cmd)
    {
        cmd.Clear();
        RenderSilhouette(cmd);
        DoJumpFlood(cmd, JumpFloodIterations(pixelWidth));
        cmd.Blit(jumpFloodBuffer0, BuiltinRenderTextureType.CameraTarget, outlineMaterial);
    }

    private void RenderSilhouette(CommandBuffer cmd)
    {
        cmd.SetRenderTarget(silhouetteBuffer.colorBuffer, BuiltinRenderTextureType.Depth);
        cmd.ClearRenderTarget(false, true, Color.clear);
        for (int i = outlinedObjects.Count - 1; i >= 0; i--)
        {
            cmd.SetGlobalFloat("_ObjectID", (i + 1f) / outlinedObjects.Count);
            cmd.DrawRenderer(outlinedObjects[i].Renderer, outlinedObjects[i].SilhouetteMaterial);
        }
    }

    private void DoJumpFlood(CommandBuffer cmd, int passes)
    {
        bool startBuff = passes % 2 == 0;
        RenderTexture bf0 = startBuff ? jumpFloodBuffer0 : jumpFloodBuffer1;
        RenderTexture bf1 = startBuff ? jumpFloodBuffer1 : jumpFloodBuffer0;

        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, SilhouetteParamId, silhouetteBuffer);
        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, DepthParamId, Shader.GetGlobalTexture("_CameraDepthTexture"));
        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, OutputParamId, bf0);
        cmd.DispatchCompute(jumpFloodShader, JfaInitKernerlID,
            ThreadGroupsCount(silhouetteBuffer.width), ThreadGroupsCount(silhouetteBuffer.height), 1);

        bool pingPong = false;
        cmd.SetComputeFloatParam(jumpFloodShader, "OutlineWidth", pixelWidth);
        cmd.SetComputeIntParam(jumpFloodShader, "BufferWidth", silhouetteBuffer.width);
        cmd.SetComputeIntParam(jumpFloodShader, "BufferHeight", silhouetteBuffer.height);

        for (int i = 0; i < passes; i++)
        {
            int stepSize = Mathf.RoundToInt(Mathf.Pow(2, passes - 1 - i));
            cmd.SetComputeIntParam(jumpFloodShader, "StepWidth", stepSize);

            if (pingPong)
            {
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, InputParamId, bf1);
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, OutputParamId, bf0);
            }
            else
            {
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, InputParamId, bf0);
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, OutputParamId, bf1);
            }

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
            silhouetteBuffer?.Release();
            jumpFloodBuffer0?.Release();
            jumpFloodBuffer0?.Release();

            var silhouetteDescriptor = new RenderTextureDescriptor(width, height)
            {
                dimension = TextureDimension.Tex2D,
                graphicsFormat = GraphicsFormat.R8_UNorm,

                msaaSamples = 1,
                depthBufferBits = 0,

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
}
