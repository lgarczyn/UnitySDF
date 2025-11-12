namespace Plugins.SDF.Internal.Editor
{
using UnityEditor;
using UnityEngine;

public class SDFSettingsEditor : EditorWindow
{
    [MenuItem("Tools/SDF Generator/Settings")]
    private static void OpenWindow()
    {
        SDFSettingsEditor window = GetWindow<SDFSettingsEditor>("SDF Generator Settings");
        window.minSize = new Vector2(300, 150);
    }

    private void OnGUI()
    {
        GUILayout.Label("SDF Generator Settings", EditorStyles.boldLabel);

        SDFSettings.instance.GeneratorMaterial =
            (Material)EditorGUILayout.ObjectField(
                SDFSettings.instance.GeneratorMaterial,
                typeof(Material),
                false);
    }
}
}