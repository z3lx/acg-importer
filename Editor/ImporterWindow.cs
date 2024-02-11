using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    /// <summary>
    /// Provides a custom window in the Unity editor for
    /// importing textures and materials from ambientCG.
    /// </summary>
    public class ImporterWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showPathSettings = true;
        private bool _showMaterialSettings = true;
        private ReorderableList _shaderPropertiesList;

        private ImporterConfig _config;

        [MenuItem("Tools/ACG Importer")]
        private static void ShowWindow()
            => GetWindow<ImporterWindow>(false, "ACG Importer", true);

        /// <summary>
        /// Initializes the importer window.
        /// </summary>
        private void OnEnable()
        {
            _config = GetDefaultConfig();

            _shaderPropertiesList = new ReorderableList(
                _config.ShaderProperties, typeof(string), true, true, true, true)
            {
                drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = DrawElementCallback,
                elementHeightCallback = ElementHeightCallback
            };
        }

        /// <summary>
        /// Handles the GUI for the importer window.
        /// </summary>
        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 220;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Draw path settings
            {
                _showPathSettings = EditorGUILayout.Foldout(
                    _showPathSettings, "Path settings", true, EditorStyles.foldoutHeader);
                if (_showPathSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    _config.InputPath = EditorGUILayout.TextField(
                        new GUIContent("Input Path",
                            "The directory or zip file to import. This can be a single zip file, " +
                            "a single folder, a parent directory containing multiple zip files, " +
                            "or a parent directory containing multiple subfolders with associated " +
                            "textures to import."),
                        _config.InputPath);
                    if (GUILayout.Button("...", GUILayout.Width(20)))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select input path", "", "");
                        if (!string.IsNullOrEmpty(path))
                            _config.InputPath = path;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    _config.OutputPath = EditorGUILayout.TextField(
                        new GUIContent("Output Path",
                            "The relative path within the Unity project where the " +
                            "imported textures and materials will be saved."),
                        _config.OutputPath);
                    if (GUILayout.Button("...", GUILayout.Width(20)))
                    {
                        var dataPath = Application.dataPath;
                        var rootPath = Path.GetDirectoryName(dataPath);
                        var path = EditorUtility.SaveFolderPanel("Select output path", dataPath, "");
                        if (!string.IsNullOrEmpty(path))
                            if (path.StartsWith(dataPath))
                                _config.OutputPath = Path.GetRelativePath(rootPath, path).Replace("\\", "/");
                            else
                                Debug.LogError("Output path must be within the Unity project.");
                    }
                    EditorGUILayout.EndHorizontal();
                    _config.CreateCategoryDirectory = EditorGUILayout.Toggle(
                        new GUIContent("Create Category Directory",
                            "If enabled, a separate directory named after the material category " +
                            "(e.g., 'Bricks' for 'Bricks090' material) " +
                            "will be created within the OutputPath. The imported textures " +
                            "and materials of that category will be saved in this directory."),
                        _config.CreateCategoryDirectory);
                    _config.CreateMaterialDirectory = EditorGUILayout.Toggle(
                        new GUIContent("Create Material Directory",
                            "If enabled, a separate directory named after the material " +
                            "will be created within the OutputPath. The imported textures " +
                            "and materials of that material will be saved in this directory."),
                        _config.CreateMaterialDirectory);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }

            // Draw material settings
            {
                _showMaterialSettings = EditorGUILayout.Foldout(
                    _showMaterialSettings, "Material settings", true, EditorStyles.foldoutHeader);
                if (_showMaterialSettings)
                {
                    EditorGUI.indentLevel++;
                    _config.Shader = (Shader)EditorGUILayout.ObjectField(
                        new GUIContent("Shader",
                            "The shader to be used for the imported materials."),
                        _config.Shader, typeof(Shader), false);
                    _shaderPropertiesList.DoLayoutList();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }

            // Draw import button
            {
                if (GUILayout.Button("Import materials"))
                    Importer.AutoImport(_config);
            }

            EditorGUILayout.EndScrollView();
        }

        #region Configuration Methods

        /// <summary>
        /// Gets the default configuration for the importer
        /// based on the render pipeline in use.
        /// </summary>
        /// <returns>A new instance of the ImporterConfig class.</returns>
        private static ImporterConfig GetDefaultConfig()
        {
            return new ImporterConfig
            {
                InputPath = Application.dataPath,
                OutputPath = GetActiveFolderPath(),
                CreateCategoryDirectory = false,
                CreateMaterialDirectory = false,
#if USING_HDRP
                Shader = Shader.Find("HDRP/Lit"),
                ShaderProperties = new List<ShaderProperty>()
                {
                    new("_BaseColorMap", MapType.Color),
                    new("_NormalMap", MapType.Normal),
                    new("_MaskMap", MapType.Mask),
                    new("_HeightMap", MapType.Height),
                    new("_DisplacementMode", 2),
                    new("_HeightPoMAmplitude", 1f)
                }
#elif USING_URP
                Shader = Shader.Find("Universal Render Pipeline/Lit"),
                ShaderProperties = new List<ShaderProperty>()
                {
                    new("_BaseMap", MapType.Color),
                    new("_MetallicGlossMap", MapType.MetallicGloss),
                    new("_Smoothness", 1.0f),
                    new("_BumpMap", MapType.Normal),
                    new("_ParallaxMap", MapType.Height),
                    new("_OcclusionMap", MapType.Occlusion)
                }
#else
                Shader = Shader.Find("Standard"),
                ShaderProperties = new List<ShaderProperty>()
                {
                    new("_MainTex", MapType.Color),
                    new("_Glossiness", 1.0f),
                    new("_MetallicGlossMap", MapType.MetallicGloss),
                    new("_BumpMap", MapType.Normal),
                    new("_ParallaxMap", MapType.Height),
                    new("_OcclusionMap", MapType.Occlusion)
                }
#endif
            };
        }

        /// <summary>
        /// Gets the active folder path in the Unity project.
        /// </summary>
        /// <returns>The path to the active folder.</returns>
        private static string GetActiveFolderPath()
        {
            var getActiveFolderPath = typeof(ProjectWindowUtil)
                .GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            return getActiveFolderPath == null
                ? "Assets"
                : getActiveFolderPath.Invoke(null, Array.Empty<object>()).ToString();
        }

        #endregion

        #region List Callbacks

        /// <summary>
        /// Draws the header of the shader properties list.
        /// </summary>
        private void DrawHeaderCallback(Rect rect)
        {
            var label = new GUIContent("Shader Properties",
                "The shader properties to be set on the material.");
            EditorGUI.LabelField(rect, label);
        }

        /// <summary>
        /// Draws each element of the shader properties list.
        /// </summary>
        private void DrawElementCallback(Rect rect, int index, bool active, bool focused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            var lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var element = _config.ShaderProperties[index];

            // Draw element label
            {
                var label = string.IsNullOrEmpty(element.Name) ? "New Shader Property" : element.Name;
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
                rect.y += lineHeight;
            }

            // Draw type popup
            {
                var options = new[]
                {
                    new GUIContent("Int"),
                    new GUIContent("Float"),
                    new GUIContent("Vector4"),
                    new GUIContent("Color"),
                    new GUIContent("Texture2D")
                };
                var choice = element.Type switch
                {
                    { } t when t == typeof(int) => 0,
                    { } t when t == typeof(float) => 1,
                    { } t when t == typeof(Vector4) => 2,
                    { } t when t == typeof(Color) => 3,
                    { } t when t == typeof(MapType) => 4,
                    _ => 0
                };
                var label = new GUIContent("Type", "The type of the shader property.");
                choice = EditorGUI.Popup(rect, label, choice, options);
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
            }

            // Draw name field
            {
                var label = new GUIContent("Name", "The name of the shader property.");
                element.Name = EditorGUI.TextField(rect, label, element.Name);
                rect.y += lineHeight;
            }

            // Draw value field
            {
                var label = new GUIContent("Value", "The value of the shader property.");
                if (element.Type == typeof(int))
                {
                    var value = (int)element.Value;
                    value = EditorGUI.IntField(rect, label, value);
                    element.Value = value;
                }
                else if (element.Type == typeof(float))
                {
                    var value = (float)element.Value;
                    value = EditorGUI.FloatField(rect, label, value);
                    element.Value = value;
                }
                else if (element.Type == typeof(Vector4))
                {
                    EditorGUI.LabelField(rect, label);
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width -= EditorGUIUtility.labelWidth;
                    var value = (Vector4)element.Value;
                    value = EditorGUI.Vector4Field(rect, string.Empty, value);
                    element.Value = value;
                }
                else if (element.Type == typeof(Color))
                {
                    var value = (Color)element.Value;
                    value = EditorGUI.ColorField(rect, label, value);
                    element.Value = value;
                }
                else if (element.Type == typeof(MapType))
                {
                    var value = (MapType)element.Value;
                    value = (MapType)EditorGUI.EnumPopup(rect, label, value);
                    element.Value = value;
                }
            }
        }

        /// <summary>
        /// Determines the height of each element in the shader properties list.
        /// </summary>
        /// <param name="index">The index of the element in the list.</param>
        /// <returns>The height of the element in the list.</returns>
        private float ElementHeightCallback(int index)
        {
            if (_config.ShaderProperties.Count == 0)
                return EditorGUIUtility.singleLineHeight;
            return EditorGUIUtility.singleLineHeight * 4 +
                EditorGUIUtility.standardVerticalSpacing * 3;
        }

        #endregion
    }
}
