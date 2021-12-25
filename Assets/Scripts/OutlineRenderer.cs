using UnityEngine;
using UnityEngine.Rendering;

public class OutlineRenderer : MonoBehaviour
{
    [SerializeField, Range(0, 20)] float width;
    [SerializeField, Range(0, 1)] float softness;

    [SerializeField] ComputeShader jumpFloodShader;

    private Camera cam;
    private Outlined[] outlinedObjects;

    private CommandBuffer cmd;
    private RenderTexture silhouetteBuffer;
    private RenderTexture jumpFloodBuffer0;
    private RenderTexture jumpFloodBuffer1;

    private Material silhouetteMaterial;
    private Material outlineMaterial;

    private readonly int SilhouetteParamId = Shader.PropertyToID("Silhouette");
    private readonly int InputParamId = Shader.PropertyToID("Input");
    private readonly int OutputParamId = Shader.PropertyToID("Output");

    private int JfaInitKernerlID;
    private int JfaStepKernerlID;

    private void Start()
    {
        cam = Camera.main;
        outlinedObjects = FindObjectsOfType<Outlined>();
        cmd = new CommandBuffer();
        cmd.name = "Outline";
        cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, cmd);

        silhouetteMaterial = new Material(Shader.Find("Outline/Silhouette"));
        outlineMaterial = new Material(Shader.Find("Outline/Outline"));

        JfaInitKernerlID = jumpFloodShader.FindKernel("Init");
        JfaStepKernerlID = jumpFloodShader.FindKernel("JumpFloodStep");
    }

    private void Update()
    {
        outlineMaterial.SetFloat("_Width", width);
        outlineMaterial.SetFloat("_Softness", softness);

        InitRenderTextures();
        cmd.Clear();
        cmd.SetRenderTarget(silhouetteBuffer);
        cmd.ClearRenderTarget(true, true, Color.clear);
        for (int i = 0; i < outlinedObjects.Length; i++)
        {
            cmd.DrawRenderer(outlinedObjects[i].Renderer, silhouetteMaterial);
        }
        
        DoJumpFlood(cmd, 3);
        cmd.Blit(jumpFloodBuffer0, BuiltinRenderTextureType.CameraTarget, outlineMaterial);
    }

    private void DoJumpFlood(CommandBuffer cmd, int passes)
    {
        bool startBuff = passes % 2 == 0;
        RenderTexture bf0 = startBuff ? jumpFloodBuffer0 : jumpFloodBuffer1;
        RenderTexture bf1 = startBuff ? jumpFloodBuffer1 : jumpFloodBuffer0;


        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, SilhouetteParamId, silhouetteBuffer);
        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, OutputParamId, bf0);
        cmd.DispatchCompute(jumpFloodShader, JfaInitKernerlID,
            ThreadGroups(Screen.width), ThreadGroups(Screen.height), 1);

        bool pingPong = false;
        cmd.SetComputeIntParam(jumpFloodShader, "BufferWidth", Screen.width);
        cmd.SetComputeIntParam(jumpFloodShader, "BufferHeight", Screen.height);

        for (int i = 0; i < passes; i++)
        {
            int stepSize = Mathf.RoundToInt(Mathf.Pow(2, i));
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
                ThreadGroups(Screen.width), ThreadGroups(Screen.height), 1);

            pingPong = !pingPong;
        }
    }

    private void InitRenderTextures()
    {
        if (silhouetteBuffer == null 
            || silhouetteBuffer.height != Screen.height 
            || silhouetteBuffer.width != Screen.width)
        {
            silhouetteBuffer?.Release();
            jumpFloodBuffer0?.Release();
            jumpFloodBuffer0?.Release();

            silhouetteBuffer = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.R8);
            silhouetteBuffer.antiAliasing = 1;
            silhouetteBuffer.Create();

            jumpFloodBuffer0 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RGHalf);
            silhouetteBuffer.antiAliasing = 1;
            jumpFloodBuffer0.enableRandomWrite = true;
            jumpFloodBuffer0.Create();

            jumpFloodBuffer1 = new RenderTexture(jumpFloodBuffer0);
            jumpFloodBuffer1.Create();
        }
    }

    private int ThreadGroups(int threadsCount) => Mathf.CeilToInt(threadsCount / 8);
}
