using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

// Logic for generating a signed distance field (using a shader)
namespace Plugins.SDF.Internal.Editor
{
[FilePath("ProjectSettings/SDFGeneratorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public class SDFSettings : ScriptableSingleton<SDFSettings>
{
    // Optional - should the material be stored in this singleton?
    [FormerlySerializedAs("GeneratorMaterial")] [SerializeField]
    private Material generatorMaterial;

    public Material GeneratorMaterial
    {
        get => generatorMaterial;
        set
        {
            if (generatorMaterial == value) return;
            generatorMaterial = value;
            Save(true);
        }
    }

    public Material CreateGeneratorMaterial()
    {
        Material material;
        if (generatorMaterial)
        {
            material = new Material(generatorMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Internal/SDFGenerator");
            if (!shader) return null;
            material = new Material(shader);
        }

        return material;
    }
}
}