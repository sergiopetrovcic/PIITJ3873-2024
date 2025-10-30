using UnityEngine;
using UnityEditor;
using System.IO;

public class FlowMapRemap : EditorWindow
{
    Texture2D source;
    string outFilename = "flowmap_remapped.png";

    // parâmetros de remapeamento (ajuste conforme necessário)
    float biasX = -0.12f; // empurra X para compensar viés X+
    float scaleX = 1.0f;
    float biasZ = +0.05f; // exemplo
    float scaleZ = 1.0f;

    [MenuItem("Tools/FlowMap Remap")]
    static void Open() => GetWindow<FlowMapRemap>("FlowMap Remap");

    void OnGUI()
    {
        GUILayout.Label("Remap Flow Map (R->X, B->Z)", EditorStyles.boldLabel);
        source = (Texture2D)EditorGUILayout.ObjectField("Source Texture", source, typeof(Texture2D), false);
        outFilename = EditorGUILayout.TextField("Output Filename", outFilename);
        biasX = EditorGUILayout.Slider("biasX", biasX, -0.5f, 0.5f);
        scaleX = EditorGUILayout.Slider("scaleX", scaleX, 0f, 3f);
        biasZ = EditorGUILayout.Slider("biasZ", biasZ, -0.5f, 0.5f);
        scaleZ = EditorGUILayout.Slider("scaleZ", scaleZ, 0f, 3f);

        if (GUILayout.Button("Generate remapped PNG") && source != null)
        {
            string path = AssetDatabase.GetAssetPath(source);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            bool prevReadable = ti.isReadable;
            if (!prevReadable) { ti.isReadable = true; ti.SaveAndReimport(); }

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.SetPixels(source.GetPixels());
            readable.Apply();

            Texture2D outTex = new Texture2D(readable.width, readable.height, TextureFormat.RGBA32, false);
            for (int y = 0; y < readable.height; y++)
                for (int x = 0; x < readable.width; x++)
                {
                    Color c = readable.GetPixel(x, y);
                    float rx = Mathf.Clamp01((c.r - 0.5f) * scaleX + 0.5f + biasX);
                    float gz = Mathf.Clamp01((c.b - 0.5f) * scaleZ + 0.5f + biasZ);
                    Color outC = new Color(rx, c.g, gz, c.a); // preserva G se quiser
                    outTex.SetPixel(x, y, outC);
                }
            outTex.Apply();

            byte[] png = outTex.EncodeToPNG();
            string outPath = Path.Combine(Application.dataPath, "..", outFilename);
            File.WriteAllBytes(outPath, png);
            Debug.Log("Remapped flow map saved to: " + outPath);
            AssetDatabase.Refresh();

            if (!prevReadable) { ti.isReadable = prevReadable; ti.SaveAndReimport(); }
        }
    }
}
