using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace z3lx.ACGImporter.Editor
{
    /// <summary>
    /// Provides the functionality for importing textures and materials from ambientCG.
    /// </summary>
    public static class Importer
    {
        /// <summary>
        /// Automatically imports textures and materials based on the provided configuration
        /// and on the contents of the input path.
        /// </summary>
        /// <param name="config">The configuration for the import process.</param>
        public static void AutoImport(ImporterConfig config)
        {
            if (File.Exists(config.InputPath) &&
                Path.GetExtension(config.InputPath) == ".zip")
                ImportSingleZip(config);
            else if (Directory.Exists(config.InputPath))
                if (Directory.EnumerateFiles(config.InputPath, "*.zip").Any())
                    ImportBulkZip(config);
                else if (Directory.EnumerateFiles(config.InputPath, "*.mtlx").Any())
                    ImportSingleDirectory(config);
                else if (Directory.EnumerateDirectories(config.InputPath).Any())
                    ImportBulkDirectory(config);
                else
                    Debug.LogError($"No suitable files found in the directory {config.InputPath}.");
            else
                Debug.LogError($"Input path {config.InputPath} does not exist.");
        }

        /// <summary>
        /// Imports a single directory containing textures based on the provided configuration.
        /// </summary>
        /// <param name="config">The configuration for the import process.</param>
        private static void ImportSingleDirectory(ImporterConfig config)
        {
            // Create directories
            var inputPath = Path.GetFullPath(config.InputPath);
            var outputPath = config.OutputPath;
            var materialName = Path.GetFileName(inputPath).Split("_")[0];
            var materialCategory = string.Concat(materialName.TakeWhile(char.IsLetter));
            if (config.CreateCategoryDirectory)
                outputPath = Path.Combine(outputPath, materialCategory);
            if (config.CreateMaterialDirectory)
                outputPath = Path.Combine(outputPath, materialName);
            Directory.CreateDirectory(outputPath);

            // Create textures
            InitializeMaps(out var maps);
            ResolveMaps(maps, config.ShaderProperties);
            if (!ReadMaps(maps, inputPath))
                return;
            CreateMaps(maps);
            ImportMaps(maps, outputPath, materialName, config.ShaderProperties);

            // Create material
            var material = CreateMaterial(maps, config.Shader, config.ShaderProperties);
            AssetDatabase.CreateAsset(material, Path.Combine(outputPath, materialName + ".mat"));
        }

        /// <summary>
        /// Imports a single zip file containing textures based on the provided configuration.
        /// </summary>
        /// <param name="config">The configuration for the import process.</param>
        private static void ImportSingleZip(ImporterConfig config)
        {
            var originalInputPath = config.InputPath;
            var extractPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString(),
                Path.GetFileNameWithoutExtension(config.InputPath));
            ZipFile.ExtractToDirectory(config.InputPath, extractPath);
            config.InputPath = extractPath;
            ImportSingleDirectory(config);
            config.InputPath = originalInputPath;
            Directory.Delete(Path.GetDirectoryName(extractPath)!, true);
        }

        /// <summary>
        /// Imports multiple directories containing textures based on the provided configuration.
        /// </summary>
        /// <param name="config">The configuration for the import process.</param>
        private static void ImportBulkDirectory(ImporterConfig config)
        {
            var originalInputPath = config.InputPath;
            var inputPaths = Directory.GetDirectories(config.InputPath);
            foreach (var inputPath in inputPaths)
            {
                config.InputPath = inputPath;
                ImportSingleDirectory(config);
            }
            config.InputPath = originalInputPath;
        }

        /// <summary>
        /// Imports multiple zip files containing textures based on the provided configuration.
        /// </summary>
        /// <param name="config">The configuration for the import process.</param>
        private static void ImportBulkZip(ImporterConfig config)
        {
            var originalInputPath = config.InputPath;
            var inputFiles = Directory.GetFiles(config.InputPath, "*.zip");
            foreach (var inputFile in inputFiles)
            {
                config.InputPath = inputFile;
                ImportSingleZip(config);
            }
            config.InputPath = originalInputPath;
        }

        #region Private methods

        /// <summary>
        /// Initializes the dictionary that will hold the mapping between MapType and Texture2D.
        /// </summary>
        /// <param name="maps">The dictionary to be initialized.</param>
        private static void InitializeMaps(out Dictionary<MapType, Texture2D> maps)
        {
            maps = new Dictionary<MapType, Texture2D>();
        }

        /// <summary>
        /// Resolves the maps based on the provided shader properties.
        /// </summary>
        /// <param name="maps">The dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <param name="properties">The shader properties to be used for resolving the maps.</param>
        private static void ResolveMaps(IDictionary<MapType, Texture2D> maps,
            IEnumerable<ShaderProperty> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Type != typeof(MapType))
                    continue;
                var mapType = (MapType)prop.Value;
                maps[mapType] = null;
                switch (mapType)
                {
                    case MapType.Smoothness:
                        maps[MapType.Color] = null;
                        maps[MapType.Roughness] = null;
                        break;
                    case MapType.MetallicGloss:
                        maps[MapType.Color] = null;
                        maps[MapType.Metallic] = null;
                        maps[MapType.Roughness] = null;
                        break;
                    case MapType.Mask:
                        maps[MapType.Color] = null;
                        maps[MapType.Metallic] = null;
                        maps[MapType.Occlusion] = null;
                        maps[MapType.Roughness] = null;
                        break;
                }
            }
        }

        /// <summary>
        /// Reads the maps from the provided input path.
        /// </summary>
        /// <param name="maps">The dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <param name="inputPath">The input path where the maps are located.</param>
        /// <returns>True if the maps were found and read successfully, false otherwise.</returns>
        private static bool ReadMaps(IDictionary<MapType, Texture2D> maps, string inputPath)
        {
            var mapTypesToRead = new Dictionary<string, MapType>
            {
                {"Color", MapType.Color},
                {"NormalGL", MapType.Normal},
                {"Metalness", MapType.Metallic},
                {"Roughness", MapType.Roughness},
                {"AmbientOcclusion", MapType.Occlusion},
                {"Displacement", MapType.Height}
            };

            var files = Directory.EnumerateFiles(inputPath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var suffix = fileName.Split('_')[^1];
                if (mapTypesToRead.TryGetValue(suffix, out var mapType) && maps.ContainsKey(mapType))
                    maps[mapType] = Read(file, mapType == MapType.Color);
            }

            if (maps.ContainsKey(MapType.Color) &&
                maps[MapType.Color] == null)
            {
                Debug.LogError($"Color map not found at {inputPath}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates swizzled maps based on the provided maps.
        /// </summary>
        /// <param name="maps">The dictionary that holds the mapping between MapType and Texture2D.</param>
        private static void CreateMaps(IDictionary<MapType, Texture2D> maps)
        {
            var mapTypesToCreate = new[]
            {
                MapType.Mask,
                MapType.MetallicGloss,
                MapType.Smoothness
            };

            var readOnlyMaps = new ReadOnlyDictionary<MapType, Texture2D>(maps);
            foreach (var mapType in mapTypesToCreate)
            {
                if (!maps.ContainsKey(mapType))
                    continue;
                maps[mapType] = mapType switch
                {
                    MapType.Mask => MapCreator.CreateMaskMap(readOnlyMaps),
                    MapType.MetallicGloss => MapCreator.CreateMetallicGlossMap(readOnlyMaps),
                    MapType.Smoothness => MapCreator.CreateSmoothnessMap(readOnlyMaps),
                    _ => maps[mapType]
                };
            }
        }

        /// <summary>
        /// Imports the maps to the specified output path.
        /// </summary>
        /// <param name="maps">The dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <param name="outputPath">The output path where the maps will be imported.</param>
        /// <param name="materialName">The name of the material associated with the maps.</param>
        /// <param name="properties">The shader properties associated with the maps.</param>
        private static void ImportMaps(IDictionary<MapType, Texture2D> maps,
            string outputPath, string materialName, IEnumerable<ShaderProperty> properties)
        {
            var mapTypes = properties
                .Where(property => property.Type == typeof(MapType))
                .Select(property => (MapType)property.Value)
                .Distinct()
                .ToList();
            foreach (var mapType in mapTypes)
            {
                if (!maps.ContainsKey(mapType) ||
                    maps[mapType] == null)
                    continue;
                var filePath = Path.Combine(outputPath, $"{materialName}_{mapType}.png");
                Write(maps[mapType], filePath);
                Import(filePath, mapType);
                Object.DestroyImmediate(maps[mapType]);
                maps[mapType] = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            }
        }

        /// <summary>
        /// Reads a texture from the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file where the texture is located.</param>
        /// <param name="sRGB">A boolean value indicating whether the texture uses sRGB color space.</param>
        /// <returns>A Texture2D object representing the texture read from the file.</returns>
        private static Texture2D Read(string filePath, bool sRGB = false)
        {
            var texture = new Texture2D(
                1,
                1,
                sRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm,
                TextureCreationFlags.None
            );
            var data = File.ReadAllBytes(filePath);
            texture.LoadImage(data, false);
            return texture;
        }

        /// <summary>
        /// Writes the provided Texture2D to a file at the specified path.
        /// </summary>
        /// <param name="source">The texture to be written to a file.</param>
        /// <param name="filePath">The path to the written file.</param>
        private static void Write(Texture2D source, string filePath)
        {
            var data = source.EncodeToPNG();
            File.WriteAllBytes(filePath, data);
        }

        /// <summary>
        /// Imports the texture from the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the file where the texture is located.</param>
        /// <param name="type">The texture map type.</param>
        private static void Import(string filePath, MapType type)
        {
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            importer.textureType = type == MapType.Normal
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
            importer.sRGBTexture = type == MapType.Color;
            importer.alphaIsTransparency = type == MapType.Color;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Creates a material with the provided shader and properties.
        /// </summary>
        /// <param name="maps">A read-only dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <param name="shader">The shader to be used for the material.</param>
        /// <param name="properties">The shader properties to be set on the material.</param>
        /// <returns>A Material object with the specified shader, properties and textures.</returns>
        private static Material CreateMaterial(IReadOnlyDictionary<MapType, Texture2D> maps,
            Shader shader, IEnumerable<ShaderProperty> properties)
        {
            var material = new Material(shader);
            foreach (var property in properties)
            {
                switch (property.Type)
                {
                    case { } t when t == typeof(int):
                        material.SetInt(property.Id, (int)property.Value);
                        break;
                    case { } t when t == typeof(float):
                        material.SetFloat(property.Id, (float)property.Value);
                        break;
                    case { } t when t == typeof(Vector4):
                        material.SetVector(property.Id, (Vector4)property.Value);
                        break;
                    case { } t when t == typeof(Color):
                        material.SetColor(property.Id, (Color)property.Value);
                        break;
                    case { } t when t == typeof(MapType):
                        material.SetTexture(property.Id, maps[(MapType)property.Value]);
                        break;
                }
            }
            return material;
        }

        #endregion
    }
}
