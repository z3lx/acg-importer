using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    public class ImporterWindow : EditorWindow
    {
        [MenuItem("Tools/ACG Importer")]
        private static void ShowWindow()
            => GetWindow<ImporterWindow>(false, "ACG Importer", true);

        private Shader _shader;
        private string _inputPath = Application.dataPath;
        private string _outputPath = GetActiveFolderPath();

        private void OnGUI()
        {
            if (!_shader)
                _shader = Shader.Find("HDRP/Lit");
            _shader = (Shader)EditorGUILayout.ObjectField("Shader", _shader, typeof(Shader), false);
            _inputPath = EditorGUILayout.TextField("Input Path", _inputPath);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            if (GUILayout.Button("Import materials"))
            {
                if (Directory.Exists(_inputPath))
                {
                    var inputPaths = Directory.GetDirectories(_inputPath);
                    foreach (var path in inputPaths)
                        Importer.Import(path, _outputPath, _shader);
                }
            }
        }

        private static string GetActiveFolderPath()
        {
            var getActiveFolderPath = typeof(ProjectWindowUtil)
                .GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            return getActiveFolderPath == null
                ? "Assets"
                : getActiveFolderPath.Invoke(null, Array.Empty<object>()).ToString();
        }
    }
}
