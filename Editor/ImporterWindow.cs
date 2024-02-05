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
            new ShaderProperty("_BaseColorMap", MapType.Color),
            new ShaderProperty("_NormalMap", MapType.Normal),
            new ShaderProperty("_MaskMap", MapType.Mask),
            new ShaderProperty("_HeightMap", MapType.Height),
            new ShaderProperty("_DisplacementMode", 2),
            new ShaderProperty("_HeightPoMAmplitude", 1f)
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

        private Vector2 _scrollPosition;
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Path settings", EditorStyles.boldLabel);
            _inputPath = EditorGUILayout.TextField("Input Path", _inputPath);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material settings", EditorStyles.boldLabel);

            _shader = (Shader)EditorGUILayout.ObjectField("Shader", _shader, typeof(Shader), false);
            _reorderableList.DoLayoutList();

            if (GUILayout.Button("Import materials"))
            {
                if (!Directory.Exists(_inputPath)) return;
                var inputPaths = Directory.GetDirectories(_inputPath);
                foreach (var path in inputPaths)
                    Importer.Import(path, _outputPath, _shader, _shaderProperties.ToArray());
            }

            EditorGUILayout.EndScrollView();
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
            var lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var element = _shaderProperties[index];

            // Draw label
            var label = string.IsNullOrEmpty(element.Name) ? "New Shader Property" : element.Name;
            EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            rect.y += lineHeight;

            // Draw type popup
            var options = new[] {"Texture", "Float", "Int"};
            var choice = element.Type switch
            {
                { } t when t == typeof(MapType) => 0,
                { } t when t == typeof(float) => 1,
                { } t when t == typeof(int) => 2,
                _ => 0
            };
            choice = EditorGUI.Popup(rect, "Type", choice, options);
            element.Type = choice switch
            {
                0 => typeof(MapType),
                1 => typeof(float),
                2 => typeof(int),
                _ => element.Type
            };
            rect.y += lineHeight;

            // Draw name field
            element.Name = EditorGUI.TextField(rect, "Name", element.Name);
            rect.y += lineHeight;

            // Draw value field
            if (element.Type == typeof(MapType))
            {
                var value = (MapType)element.Value;
                value = (MapType)EditorGUI.EnumPopup(rect, "Value", value);
                element.Value = value;
            }
            else if (element.Type == typeof(float))
            {
                var value = (float)element.Value;
                value = EditorGUI.FloatField(rect, "Value", value);
                element.Value = value;
            }
            else if (element.Type == typeof(int))
            {
                var value = (int)element.Value;
                value = EditorGUI.IntField(rect, "Value", value);
                element.Value = value;
            }
        }
    }
}
