using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Plugins.SDF.Internal.Editor
{
[CustomEditor(typeof(SDFGenerator))]
public class SDFGeneratorEditor : UnityEditor.Editor
{
    private static readonly int Feather = Shader.PropertyToID("_Feather");

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            new GUIContent(
                "Drag an image into the slot below and click 'Generate'"
              + " or append '+sdf' to the end of the filename.\n"
              + "(ie. 'TestImage+sdf.png')"));
        base.OnInspectorGUI();
        SDFGenerator generator = (SDFGenerator)target;
        if (GUI.Button(EditorGUILayout.GetControlRect(), "Generate"))
        {
            GenerateStatic(generator);
        }
    }

    private static void GenerateStatic(SDFGenerator generator)
    {
        // Validate the input
        if ((generator.mode & (TextureModes.RGB)) != 0)
        {
            foreach (Texture2D target in generator.targets)
            {
                if (GraphicsFormatUtility.IsSRGBFormat(target.graphicsFormat))
                {
                    Debug.LogWarning(
                        "Texture "
                      + target
                      + " is sRGB but is being used as an RGB distance field. Consider importing it as linear.");
                }
            }
        }

        // Configure material
        Material material = SDFSettings.instance.CreateGeneratorMaterial();
        // Generate based on source textures
        foreach (Texture2D target in generator.targets)
        {
            material.SetFloat(Feather, generator.gradientSizePx / Mathf.Max(target.width, target.height));
            GenerateAsset(generator, target, material);
        }

        // Cleanup
        DestroyImmediate(material);
    }

    private static void GenerateAsset(SDFGenerator generator, Texture2D texture, Material material)
    {
        // Generate SDF data
        Texture2D result = SDFGenerator.Generate(texture, material, generator.mode);

        // Generate the new asset
        string path = AssetDatabase.GetAssetPath(texture);
        path = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".sdf.png";
        File.WriteAllBytes(path, result.EncodeToPNG());
        AssetDatabase.Refresh();
        DestroyImmediate(result);

        // Disable compression and use simple format
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer || !generator.setImportSettings) return;
        SDFImporter.SetImportParameters(importer, generator.mode);
        string originalPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter originalImporter = string.IsNullOrEmpty(originalPath)
                                               ? default
                                               : AssetImporter.GetAtPath(path) as TextureImporter;
        if (originalImporter == null) return;
        // Preserve sRGB if not processing any RGB
        if ((generator.mode & TextureModes.RGB) == 0)
        {
            importer.sRGBTexture = originalImporter.sRGBTexture;
        }
    }
}
}