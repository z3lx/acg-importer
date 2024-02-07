using System;
using System.Collections.Generic;
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
        public static void Import(string inputPath, string outputPath, Shader shader, ShaderProperty[] properties)
        {
            inputPath = inputPath.TrimEnd(Path.DirectorySeparatorChar);
            var materialName = Path.GetFileName(inputPath);

            // Create textures
            InitializeMaps(out var maps);
            ResolveMaps(ref maps, properties);
            if (!ReadMaps(ref maps, inputPath))
                return;
            WriteAndImportMaps(maps, outputPath, materialName, properties);

            // Create material
            var material = CreateMaterial(maps, shader, properties);
            AssetDatabase.CreateAsset(material, Path.Combine(outputPath, materialName + ".mat"));
        }

        private static void InitializeMaps(out Dictionary<MapType, Texture2D> maps)
        {
            maps = new Dictionary<MapType, Texture2D>();
        }

        private static void ResolveMaps(ref Dictionary<MapType, Texture2D> maps, IEnumerable<ShaderProperty> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Type != typeof(MapType))
                    continue;
                switch ((MapType)prop.Value)
                {
                    case MapType.Color:
                        maps[MapType.Color] = null;
                        break;
                    case MapType.Normal:
                        maps[MapType.Normal] = null;
                        break;
                    case MapType.Metallic:
                        maps[MapType.Metallic] = null;
                        break;
                    case MapType.Roughness:
                        maps[MapType.Roughness] = null;
                        break;
                    case MapType.Occlusion:
                        maps[MapType.Occlusion] = null;
                        break;
                    case MapType.Height:
                        maps[MapType.Height] = null;
                        break;
                    case MapType.MetallicSmoothness:
                        maps[MapType.MetallicSmoothness] = null;
                        maps[MapType.Metallic] = null;
                        maps[MapType.Roughness] = null;
                        break;
                    case MapType.Mask:
                        maps[MapType.Mask] = null;
                        maps[MapType.Color] = null;
                        maps[MapType.Metallic] = null;
                        maps[MapType.Occlusion] = null;
                        maps[MapType.Roughness] = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static bool ReadMaps(ref Dictionary<MapType, Texture2D> maps, string inputPath)
        {
            var mapTypes = new Dictionary<string, MapType>
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
                if (mapTypes.TryGetValue(suffix, out var mapType) && maps.ContainsKey(mapType))
                    maps[mapType] = Read(file, mapType == MapType.Color);
            }

            if (maps.ContainsKey(MapType.Color) &&
                maps[MapType.Color] == null)
            {
                Debug.LogError($"Color map not found at {inputPath}.");
                return false;
            }

            if (maps.ContainsKey(MapType.Mask))
                maps[MapType.Mask] = MapCreator.CreateMaskMap(maps);
            if (maps.ContainsKey(MapType.MetallicSmoothness))
                maps[MapType.MetallicSmoothness] = MapCreator.CreateMetallicSmoothnessMap(maps);
            return true;
        }

        private static void WriteAndImportMaps(
            IDictionary<MapType, Texture2D> maps, string outputPath, string materialName, ShaderProperty[] properties)
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
            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(
            Dictionary<MapType, Texture2D> maps,
            Shader shader, IEnumerable<ShaderProperty> properties)
        {
            var material = new Material(shader);
            foreach (var property in properties)
            {
                switch (property.Type)
                {
                    case { } t when t == typeof(MapType):
                        material.SetTexture(property.Id, maps[(MapType)property.Value]);
                        break;
                    case { } t when t == typeof(float):
                        material.SetFloat(property.Id, (float)property.Value);
                        break;
                    case { } t when t == typeof(int):
                        material.SetInt(property.Id, (int)property.Value);
                        break;
                }
            }
            return material;
        }
    }
}
