using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace Plugins.SDF.Internal
{
[System.Flags]
public enum TextureModes
{
    R = 0x01,
    G = 0x02,
    B = 0x04,
    A = 0x08,
    RGB = R | G | B,
    RGBA = R | G | B | A,
}

[CreateAssetMenu(menuName = "2D/SDF Generator")]
public class SDFGenerator : ScriptableObject
{
    [FormerlySerializedAs("Mode")]
    [Header("Settings")]
    [Tooltip("Process only the Alpha channel, or process all channels")]
    public TextureModes mode = TextureModes.A;

    [FormerlySerializedAs("GradientSizePX")] [Tooltip("How far the SDF should spread (in percentage of texture size)")]
    public float gradientSizePx = 20;

    [FormerlySerializedAs("SetImportSettings")] [Tooltip("Set the import settings to be optimal for an SDF")]
    public bool setImportSettings = true;

    //public TextureImporterFormat FormatOverride = TextureImporterFormat.Automatic;

    [FormerlySerializedAs("Targets")] [Header("Source Textures")]
    public Texture2D[] targets;

    static readonly int Spread = Shader.PropertyToID("_Spread");
    static readonly int Channel = Shader.PropertyToID("_Channel");
    static readonly int SourceTex = Shader.PropertyToID("_SourceTex");

    public static Texture2D Generate(
        Texture2D    texture,
        Material     material,
        TextureModes mode,
        int          width  = -1,
        int          height = -1
    )
    {
        Texture2D result = null;
        Color32[] pixels = null;
        for (int c = 3; c >= 0; c--)
        {
            if (((int)mode & (1 << c)) == 0) continue;
            material.SetFloat(Channel, c);
            Texture2D resultC = Generate(texture, material, width, height);
            if (result == null)
            {
                // We can use alpha directly (generator outputs in A channel)
                result = resultC;
            }
            else
            {
                // Otherwise we'll just pack on CPU
                pixels ??= result.GetPixels32();
                Color32[] resPx = resultC.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i][c] = resPx[i][c];
                }

                DestroyImmediate(resultC);
            }
        }

        if (pixels != null)
            result.SetPixels32(pixels);
        return result;
    }

    // Generate a distance field
    // The "material" must be a SDF generating material (ie. the one at UnitySDF/SDFGenerator.mat)
    // Optionally push the results to the specified texture (must be a compatible format)
    public static Texture2D Generate(Texture2D texture, Material material, int width = -1, int height = -1)
    {
        // Allocate some temporary buffers
        RenderTextureDescriptor stepFormat = new(texture.width, texture.height, GraphicsFormat.R16G16B16A16_UNorm, 0, 0)
        {
            sRGB = false,
        };
        RenderTexture target1 = RenderTexture.GetTemporary(stepFormat);
        RenderTexture target2 = RenderTexture.GetTemporary(stepFormat);
        target1.filterMode = FilterMode.Point;
        target2.filterMode = FilterMode.Point;
        target1.wrapMode = TextureWrapMode.Clamp;
        target2.wrapMode = TextureWrapMode.Clamp;

        const int firstPass = 0;
        int finalPass = material.FindPass("FinalPass");

        // Detect edges of image
        material.EnableKeyword("FIRSTPASS");
        material.SetFloat(Spread, 1);
        Graphics.Blit(texture, target1, material, firstPass);
        material.DisableKeyword("FIRSTPASS");
        (target1, target2) = (target2, target1);

        // Gather nearest edges with varying spread values
        for (int i = 11; i >= 0; i--)
        {
            material.SetFloat(Spread, Mathf.Pow(2, i));
            Graphics.Blit(target2, target1, material, firstPass);
            (target1, target2) = (target2, target1);
        }

        RenderTextureDescriptor resultFormat = new RenderTextureDescriptor(
            texture.width,
            texture.height,
            GraphicsFormat.R8G8B8A8_UNorm,
            0,
            0);
        resultFormat.sRGB = GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat);
        RenderTexture resultTarget = RenderTexture.GetTemporary(resultFormat);
        resultTarget.wrapMode = TextureWrapMode.Clamp;

        // Compute the final distance from nearest edge value
        material.SetTexture(SourceTex, texture);
        Graphics.Blit(target2, resultTarget, material, finalPass);

        if (width == -1) width = texture.width;
        if (height == -1) height = texture.height;
        Texture2D result = new(width, height, GraphicsFormat.R8G8B8A8_UNorm, 0, TextureCreationFlags.None);

        // If the texture needs to be resized, resize it here
        if (result.width != texture.width || result.height != texture.height)
        {
            RenderTexture resultTarget2 = RenderTexture.GetTemporary(
                result.width,
                result.height,
                0,
                GraphicsFormat.R8G8B8A8_UNorm);
            resultTarget2.wrapMode = TextureWrapMode.Clamp;
            Graphics.Blit(resultTarget, resultTarget2);
            (resultTarget, resultTarget2) = (resultTarget2, resultTarget);
            RenderTexture.ReleaseTemporary(resultTarget2);
        }

        // Copy to CPU
        result.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);

        // Clean up
        RenderTexture.ReleaseTemporary(resultTarget);
        RenderTexture.ReleaseTemporary(target2);
        RenderTexture.ReleaseTemporary(target1);

        return result;
    }
}
}