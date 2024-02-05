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
            new ShaderProperty(ShaderProperty.PropertyType.Texture, "_BaseColorMap", "color"),
            new ShaderProperty(ShaderProperty.PropertyType.Texture, "_NormalMap", "normal"),
            new ShaderProperty(ShaderProperty.PropertyType.Texture, "_MaskMap", "mask"),
            new ShaderProperty(ShaderProperty.PropertyType.Texture, "_HeightMap", "height"),
            new ShaderProperty(ShaderProperty.PropertyType.Int, "_DisplacementMode", "2"),
            new ShaderProperty(ShaderProperty.PropertyType.Float, "_HeightPoMAmplitude", "1"),
        };
        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            if (!_shader)
                _shader = Shader.Find("HDRP/Lit");

            _reorderableList = new ReorderableList(_shaderProperties, typeof(string), true, true, true, true)
            {
                drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = DrawElementCallback,
                elementHeight = 4 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)
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
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += EditorGUIUtility.standardVerticalSpacing;

            var element = _shaderProperties[index];
            var label = string.IsNullOrEmpty(element.name) ? "New Shader Property" : element.name;
            EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            element.type = (ShaderProperty.PropertyType)EditorGUI.EnumPopup(rect, "Type", element.type);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            element.name = EditorGUI.TextField(rect, "Name", element.name);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            element.value = EditorGUI.TextField(rect, "Value", element.value);
        }
    }
}
