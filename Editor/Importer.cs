using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace z3lx.ACGImporter.Editor
{
    public static class Importer
    {
        public static void Import(ImporterConfig config)
        {
            // Create directories
            var inputPath = config.InputPath.TrimEnd(Path.DirectorySeparatorChar);
            var outputPath = config.OutputPath;
            var materialName = Path.GetFileName(inputPath);
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

        #region Private methods

        private static void InitializeMaps(out Dictionary<MapType, Texture2D> maps)
        {
            maps = new Dictionary<MapType, Texture2D>();
        }

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
                        maps[MapType.Roughness] = null;
                        break;
                    case MapType.MetallicGloss:
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

        private static void Write(Texture2D source, string filePath)
        {
            var data = source.EncodeToPNG();
            File.WriteAllBytes(filePath, data);
        }

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

        private static Material CreateMaterial(
            IReadOnlyDictionary<MapType, Texture2D> maps,
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
