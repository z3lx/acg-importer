using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    public class ImporterWindow : EditorWindow
    {
        [MenuItem("Tools/ACG Importer")]
        private static void ShowWindow()
            => GetWindow<ImporterWindow>(false, "ACG Importer", true);

        private string _inputPath = Application.dataPath;
        private string _outputPath = GetActiveFolderPath();
        private Shader _shader;
        private readonly List<ShaderProperty> _shaderProperties = new()
        {
            new ShaderProperty(ShaderProperty.Type.Texture, "_BaseColorMap", "color"),
            new ShaderProperty(ShaderProperty.Type.Texture, "_NormalMap", "normal"),
            new ShaderProperty(ShaderProperty.Type.Texture, "_MaskMap", "mask"),
            new ShaderProperty(ShaderProperty.Type.Texture, "_HeightMap", "height"),
            new ShaderProperty(ShaderProperty.Type.Int, "_DisplacementMode", "2"),
            new ShaderProperty(ShaderProperty.Type.Float, "_HeightPoMAmplitude", "1"),
        };
        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            if (!_shader)
                _shader = Shader.Find("HDRP/Lit");

            _reorderableList = new ReorderableList(_shaderProperties, typeof(string), true, true, true, true)
            {
                drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = DrawElementCallback
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Path settings", EditorStyles.boldLabel);
            _inputPath = EditorGUILayout.TextField("Input Path", _inputPath);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material settings", EditorStyles.boldLabel);

            _shader = (Shader)EditorGUILayout.ObjectField("Shader", _shader, typeof(Shader), false);
            _reorderableList.DoLayoutList();

            if (!GUILayout.Button("Import materials")) return;
            if (!Directory.Exists(_inputPath)) return;
            var inputPaths = Directory.GetDirectories(_inputPath);
            foreach (var path in inputPaths)
                Importer.Import(path, _outputPath, _shader, _shaderProperties.ToArray());
        }

        private static string GetActiveFolderPath()
        {
            var getActiveFolderPath = typeof(ProjectWindowUtil)
                .GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            return getActiveFolderPath == null
                ? "Assets"
                : getActiveFolderPath.Invoke(null, Array.Empty<object>()).ToString();
        }

        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Shader Properties");
        }

        private void DrawElementCallback(Rect rect, int index, bool active, bool focused)
        {
            const int padding = 4;
            const int typeWidth = 96;
            const int valueWidth = 96;

            rect.y += (rect.height - EditorGUIUtility.singleLineHeight) / 2;
            var element = _shaderProperties[index];
            element.type = (ShaderProperty.Type)EditorGUI.EnumPopup(
                new Rect(
                    rect.x,
                    rect.y,
                    typeWidth,
                    EditorGUIUtility.singleLineHeight
                ),
                element.type
            );
            element.name = EditorGUI.TextField(
                new Rect(
                    rect.x + typeWidth + padding,
                    rect.y,
                    rect.width - typeWidth - valueWidth - 2 * padding,
                    EditorGUIUtility.singleLineHeight
                ),
                element.name
            );
            element.value = EditorGUI.TextField(
                new Rect(
                    rect.x + rect.width - valueWidth,
                    rect.y,
                    valueWidth,
                    EditorGUIUtility.singleLineHeight
                ),
                element.value
            );
        }
    }
}
