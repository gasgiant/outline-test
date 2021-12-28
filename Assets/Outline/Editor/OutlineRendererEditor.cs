using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutlineRenderer))]
public class OutlineRendererEditor : Editor
{
    private SerializedProperty occlusion;
    private SerializedProperty depthTest;
    private SerializedProperty width;
    private SerializedProperty softness;
    private SerializedProperty colors;

    private SerializedProperty jumpFloodShader;
    private SerializedProperty outlineShader;
    private SerializedProperty silhouetteShader;

    private void OnEnable()
    {
        occlusion = serializedObject.FindProperty("occlusion");
        depthTest = serializedObject.FindProperty("depthTest");
        width = serializedObject.FindProperty("width");
        softness = serializedObject.FindProperty("softness");
        colors = serializedObject.FindProperty("colors");


        jumpFloodShader = serializedObject.FindProperty("jumpFloodShader");
        outlineShader = serializedObject.FindProperty("outlineShader");
        silhouetteShader = serializedObject.FindProperty("silhouetteShader");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();
        DrawProps();
        DrawInfo();
        FindShaders();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProps()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        EditorGUILayout.PropertyField(occlusion);
        using (new EditorGUI.DisabledScope(!occlusion.boolValue))
        {
            EditorGUILayout.PropertyField(depthTest);
        }

        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(softness);
        EditorGUILayout.PropertyField(colors);

        if (colors.arraySize > OutlineRenderer.ColorsCount)
            colors.arraySize = OutlineRenderer.ColorsCount;
    }

    private void DrawInfo()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Info");
        EditorGUI.DrawRect(
            EditorGUILayout.GetControlRect(false, 1),
            EditorStyles.label.normal.textColor * 0.7f);
        EditorGUILayout.LabelField("Jump Flood Passes 1080p: "
            + OutlineRenderer.JumpFloodIterations(
                OutlineRenderer.PixelWidth(width.floatValue, 1080)));
        EditorGUILayout.LabelField("Jump Flood Passes 1440p: "
            + OutlineRenderer.JumpFloodIterations(
                OutlineRenderer.PixelWidth(width.floatValue, 1440)));
        EditorGUILayout.LabelField("Jump Flood Passes 2160p: "
            + OutlineRenderer.JumpFloodIterations(
                OutlineRenderer.PixelWidth(width.floatValue, 2160)));
    }

    private void FindShaders()
    {
        if (outlineShader.objectReferenceValue == null)
            outlineShader.objectReferenceValue = Shader.Find(OutlineRenderer.OutlineShaderName);
        if (silhouetteShader.objectReferenceValue == null)
            silhouetteShader.objectReferenceValue = Shader.Find(OutlineRenderer.SilhouetteShaderName);
        if (jumpFloodShader.objectReferenceValue == null)
        {
            var res = AssetDatabase.FindAssets("t: ComputeShader JumpFloodAlgorithm");
            if (res.Length > 0)
            {
                jumpFloodShader.objectReferenceValue = AssetDatabase.LoadAssetAtPath(
                    AssetDatabase.GUIDToAssetPath(res[0]), typeof(ComputeShader));
            }
            else
                Debug.LogError("JumpFloodAlgorithm compute shader missing!");
        }
    }
}
