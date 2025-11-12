// Usage:
// Name a texture 'texturename+sdf'

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Plugins.SDF.Internal.Editor
{
public class SDFImporter : AssetPostprocessor
{
    private static readonly Regex SDFPattern = new("(?:\\+sdf)(f)?(4)?");
    private static readonly int Feather = Shader.PropertyToID("_Feather");

    private TextureModes GetMode()
    {
        Match match = SDFPattern.Match(assetPath);
        if (!match.Success) return 0;
        return match.Groups[2].Success ? TextureModes.RGBA : TextureModes.A;
    }

    private void OnPreprocessTexture()
    {
        // Check if the name ends with +sdf
        Match match = SDFPattern.Match(assetPath);
        if (!match.Success) return;
        // Check if we should change import format
        if (match.Groups[1].Success)
        {
            if (assetImporter is TextureImporter importer)
            {
                SetImportParameters(importer, GetMode());
            }
        }
    }

    // After the texture is imported
    public void OnPostprocessTexture(Texture2D texture)
    {
        // Check if the name ends with +sdf
        Match match = SDFPattern.Match(assetPath);
        if (!match.Success) return;

        Material material = SDFSettings.instance.CreateGeneratorMaterial();
        if (material == null)
        {
            Debug.LogError("Failed to find SDF material", texture);
            return;
        }

        // Load uncompressed texture data
        bool linear = false;
        if (assetImporter is TextureImporter importer) linear = !importer.sRGBTexture;
        Texture2D original = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
        if (!original.LoadImage(File.ReadAllBytes(assetPath)))
        {
            Debug.LogError("Failed to load image. It might be the wrong format", texture);
        }

        float gradient = Mathf.Min(20f / Mathf.Max(texture.width, texture.height), 0.25f);
        material.SetFloat(Feather, gradient);
        Texture2D result = SDFGenerator.Generate(original, material, GetMode(), texture.width, texture.height);
        Color32[] pixels = result.GetPixels32();
        texture.SetPixels32(pixels);
        texture.Apply(true);

        // Cleanup
        Object.DestroyImmediate(original);
        Object.DestroyImmediate(material);
    }

    public static void SetImportParameters(TextureImporter importer, TextureModes mode)
    {
        importer.sRGBTexture = (mode & TextureModes.RGB) == 0;
        TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
        settings.format =
            mode switch
            {
                TextureModes.A   => TextureImporterFormat.Alpha8,
                TextureModes.RGB => TextureImporterFormat.RGB24,
                _                => TextureImporterFormat.RGBA32,
            };
        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
}