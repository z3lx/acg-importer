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

        private ImporterConfig _config;

        private void OnEnable()
        {
            _config = new ImporterConfig
            {
                inputPath = Application.dataPath,
                outputPath = GetActiveFolderPath(),
                createCategoryDirectory = false,
                createMaterialDirectory = false,
#if USING_HDRP
                shader = Shader.Find("HDRP/Lit"),
                shaderProperties = new List<ShaderProperty>()
                {
                    new("_BaseColorMap", MapType.Color),
                    new("_NormalMap", MapType.Normal),
                    new("_MaskMap", MapType.Mask),
                    new("_HeightMap", MapType.Height),
                    new("_DisplacementMode", 2),
                    new("_HeightPoMAmplitude", 1f)
                }
#elif USING_URP
                shader = Shader.Find("Universal Render Pipeline/Lit"),
                shaderProperties = new List<ShaderProperty>()
                {
                    new("_BaseMap", MapType.Color),
                    new("_MetallicGlossMap", MapType.MetallicSmoothness),
                    new("_Smoothness", 1.0f),
                    new("_BumpMap", MapType.Normal),
                    new("_ParallaxMap", MapType.Height),
                    new("_OcclusionMap", MapType.Occlusion)
                }
#else
#endif
            };

            _shaderPropertiesList = new ReorderableList(_config.shaderProperties, typeof(string), true, true, true, true)
            {
                drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = DrawElementCallback,
                elementHeight = 4 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)
            };
        }

        private Vector2 _scrollPosition;
        private bool _showPathSettings = true;
        private bool _showMaterialSettings = true;
        private ReorderableList _shaderPropertiesList;

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 220;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            _showPathSettings = EditorGUILayout.Foldout(
                _showPathSettings, "Path settings", true, EditorStyles.foldoutHeader);
            if (_showPathSettings)
            {
                EditorGUI.indentLevel++;
                _config.inputPath = EditorGUILayout.TextField("Input Path", _config.inputPath);
                _config.outputPath = EditorGUILayout.TextField("Output Path", _config.outputPath);
                _config.createCategoryDirectory = EditorGUILayout.Toggle(
                    "Create Category Directory", _config.createCategoryDirectory);
                _config.createMaterialDirectory = EditorGUILayout.Toggle(
                    "Create Material Directory", _config.createMaterialDirectory);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            _showMaterialSettings = EditorGUILayout.Foldout(
                _showMaterialSettings, "Material settings", true, EditorStyles.foldoutHeader);
            if (_showMaterialSettings)
            {
                EditorGUI.indentLevel++;
                _config.shader = (Shader)EditorGUILayout.ObjectField(
                    "Shader", _config.shader, typeof(Shader), false);
                _shaderPropertiesList.DoLayoutList();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Import materials") &&
                Directory.Exists(_config.inputPath))
            {
                var originalInputPath = _config.inputPath;
                var inputPaths = Directory.GetDirectories(_config.inputPath);
                foreach (var inputPath in inputPaths)
                {
                    _config.inputPath = inputPath;
                    Importer.Import(_config);
                }
                _config.inputPath = originalInputPath;
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

            var element = _config.shaderProperties[index];

            // Draw label
            var label = string.IsNullOrEmpty(element.Name) ? "New Shader Property" : element.Name;
            EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            rect.y += lineHeight;

            // Draw type popup
            var options = new[] {"Int", "Float", "Vector4", "Color", "Texture2D"};
            var choice = element.Type switch
            {
                { } t when t == typeof(int) => 0,
                { } t when t == typeof(float) => 1,
                { } t when t == typeof(Vector4) => 2,
                { } t when t == typeof(Color) => 3,
                { } t when t == typeof(MapType) => 4,
                _ => 0
            };
            choice = EditorGUI.Popup(rect, "Type", choice, options);
            element.Type = choice switch
            {
                0 => typeof(int),
                1 => typeof(float),
                2 => typeof(Vector4),
                3 => typeof(Color),
                4 => typeof(MapType),
                _ => element.Type
            };
            rect.y += lineHeight;

            // Draw name field
            element.Name = EditorGUI.TextField(rect, "Name", element.Name);
            rect.y += lineHeight;

            // Draw value field
            if (element.Type == typeof(int))
            {
                var value = (int)element.Value;
                value = EditorGUI.IntField(rect, "Value", value);
                element.Value = value;
            }
            else if (element.Type == typeof(float))
            {
                var value = (float)element.Value;
                value = EditorGUI.FloatField(rect, "Value", value);
                element.Value = value;
            }
            else if (element.Type == typeof(Vector4))
            {
                EditorGUI.LabelField(rect, "Value");
                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                var value = (Vector4)element.Value;
                value = EditorGUI.Vector4Field(rect, "", value);
                element.Value = value;
            }
            else if (element.Type == typeof(Color))
            {
                var value = (Color)element.Value;
                value = EditorGUI.ColorField(rect, "Value", value);
                element.Value = value;
            }
            else if (element.Type == typeof(MapType))
            {
                var value = (MapType)element.Value;
                value = (MapType)EditorGUI.EnumPopup(rect, "Value", value);
                element.Value = value;
            }
        }
    }
}
