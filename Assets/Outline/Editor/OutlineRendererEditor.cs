using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutlineRenderer))]
public class OutlineRendererEditor : Editor
{
    private SerializedProperty pixelWidth;
    private SerializedProperty jumpFloodShader;
    private SerializedProperty outlineShader;
    private SerializedProperty silhouetteShader;


    private void OnEnable()
    {
        pixelWidth = serializedObject.FindProperty("pixelWidth");
        jumpFloodShader = serializedObject.FindProperty("jumpFloodShader");
        outlineShader = serializedObject.FindProperty("outlineShader");
        silhouetteShader = serializedObject.FindProperty("silhouetteShader");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();
        DrawDefaultInspector();
        DrawInfo();
        FindShaders();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInfo()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Info");
        EditorGUI.DrawRect(
            EditorGUILayout.GetControlRect(false, 1),
            EditorStyles.label.normal.textColor * 0.7f);
        EditorGUILayout.LabelField("Jump Flood Passes: "
            + OutlineRenderer.JumpFloodIterations(pixelWidth.floatValue));
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
