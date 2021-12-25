using UnityEngine;
using UnityEngine.Rendering;

public class OutlineRenderer : MonoBehaviour
{
    [SerializeField] ComputeShader jumpFloodShader;

    private Camera cam;
    private Outlined[] outlinedObjects;

    private CommandBuffer cmd;
    private RenderTexture silhouetteBuffer;
    private RenderTexture jumpFloodBuffer0;
    private RenderTexture jumpFloodBuffer1;

    private Material silhouetteMaterial;

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

        JfaInitKernerlID = jumpFloodShader.FindKernel("Init");
        JfaStepKernerlID = jumpFloodShader.FindKernel("JumpFloodStep");
    }

    private void Update()
    {
        InitRenderTextures();
        cmd.Clear();

        cmd.SetRenderTarget(silhouetteBuffer);
        for (int i = 0; i < outlinedObjects.Length; i++)
        {
            cmd.DrawRenderer(outlinedObjects[i].Renderer, silhouetteMaterial);
        }

        DoJumpFlood(cmd);

        
    }

    private void DoJumpFlood(CommandBuffer cmd)
    {
        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, SilhouetteParamId, silhouetteBuffer);
        cmd.SetComputeTextureParam(jumpFloodShader, JfaInitKernerlID, OutputParamId, jumpFloodBuffer0);
        cmd.DispatchCompute(jumpFloodShader, JfaInitKernerlID,
            ThreadGroups(Screen.width), ThreadGroups(Screen.height), 1);

        bool pingPong = false;
        cmd.SetComputeIntParam(jumpFloodShader, "BufferWidth", Screen.width);
        cmd.SetComputeIntParam(jumpFloodShader, "BufferHeight", Screen.height);

        for (int i = 0; i < 5; i++)
        {
            int stepSize = Mathf.RoundToInt(Mathf.Pow(2, i));
            cmd.SetComputeIntParam(jumpFloodShader, "StepWidth", stepSize);

            if (pingPong)
            {
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, InputParamId, jumpFloodBuffer1);
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, OutputParamId, jumpFloodBuffer0);
            }
            else
            {
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, InputParamId, jumpFloodBuffer0);
                cmd.SetComputeTextureParam(jumpFloodShader, JfaStepKernerlID, OutputParamId, jumpFloodBuffer1);
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
