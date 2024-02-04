using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace z3lx.ACGImporter.Editor
{
    public static class Importer
    {
        public static void Import(string inputPath, string outputPath, Shader shader)
        {
            Texture2D albedo = null;
            Texture2D normal = null;
            Texture2D metallic = null;
            Texture2D roughness = null;
            Texture2D occlusion = null;
            Texture2D height = null;

            inputPath = inputPath.TrimEnd(Path.DirectorySeparatorChar);
            var materialName = Path.GetFileName(inputPath);

            // Read textures
            var files = Directory.EnumerateFiles(inputPath);
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var suffix = fileName.Split('_')[^1];

                switch (suffix)
                {
                    case "Color":
                        albedo = Read(file, true);
                        break;
                    case "NormalGL":
                        normal = Read(file);
                        break;
                    case "Metalness":
                        metallic = Read(file);
                        break;
                    case "Roughness":
                        roughness = Read(file);
                        break;
                    case "AmbientOcclusion":
                        occlusion = Read(file);
                        break;
                    case "Displacement":
                        height = Read(file);
                        break;
                }
            }

            if (!albedo)
            {
                Debug.LogError($"Albedo texture not found for {materialName}.");
                return;
            }

            // Create mask map
            var mask = CreateMaskMap(albedo, metallic, occlusion, roughness);
            Object.DestroyImmediate(metallic);
            Object.DestroyImmediate(occlusion);
            Object.DestroyImmediate(roughness);

            // Write and import textures
            WriteAndImport(ref albedo, Path.Combine(outputPath, materialName + "_Albedo.png"), MapType.Color);
            WriteAndImport(ref normal, Path.Combine(outputPath, materialName + "_Normal.png"), MapType.Normal);
            WriteAndImport(ref height, Path.Combine(outputPath, materialName + "_Height.png"), MapType.Linear);
            WriteAndImport(ref mask, Path.Combine(outputPath, materialName + "_Mask.png"), MapType.Linear);

            // Create material
            var material = CreateMaterial(albedo, normal, height, mask, shader);
            AssetDatabase.CreateAsset(material, Path.Combine(outputPath, materialName + ".mat"));
        }

        private enum MapType
        {
            Color,
            Linear,
            Normal
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
            importer.textureType = type == MapType.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = type == MapType.Color;
            importer.SaveAndReimport();
        }

        private static void WriteAndImport(ref Texture2D source, string filePath, MapType type)
        {
            if (!source)
                return;
            Write(source, filePath);
            Import(filePath, type);
            Object.DestroyImmediate(source);
            source = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        }

        private static readonly Material MaskMapMat = new(Shader.Find("Hidden/MaskMap"));
        private static readonly int MetallicMapProp = Shader.PropertyToID("_MetallicMap");
        private static readonly int OcclusionMapProp = Shader.PropertyToID("_OcclusionMap");
        private static readonly int RoughnessMapProp = Shader.PropertyToID("_RoughnessMap");
        private static Texture2D CreateMaskMap(Texture albedo, Texture metallic, Texture occlusion, Texture roughness)
        {
            var rd = RenderTexture.GetTemporary(
                albedo.width,
                albedo.height,
                0,
                GraphicsFormat.R8G8B8A8_UNorm
            );
            MaskMapMat.SetTexture(MetallicMapProp, metallic);
            MaskMapMat.SetTexture(OcclusionMapProp, occlusion);
            MaskMapMat.SetTexture(RoughnessMapProp, roughness);
            Graphics.Blit(null, rd, MaskMapMat);
            var mask = RenderToTexture(rd);
            RenderTexture.ReleaseTemporary(rd);
            return mask;
        }

        private static Texture2D RenderToTexture(RenderTexture renderTexture)
        {
            var texture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                GraphicsFormatUtility.GetGraphicsFormat(renderTexture.format, renderTexture.sRGB),
                TextureCreationFlags.None
            );
            var oldActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(
                new Rect(0, 0, renderTexture.width, renderTexture.height),
                0,
                0
            );
            texture.Apply();
            RenderTexture.active = oldActive;
            return texture;
        }

        private static readonly int BaseColorMapProp = Shader.PropertyToID("_BaseColorMap");
        private static readonly int NormalMapProp = Shader.PropertyToID("_NormalMap");
        private static readonly int HeightMapProp = Shader.PropertyToID("_HeightMap");
        private static readonly int MaskMapProp = Shader.PropertyToID("_MaskMap");
        private static readonly int DisplacementModeProp = Shader.PropertyToID("_DisplacementMode");
        private static readonly int HeightPoMAmplitudeProp = Shader.PropertyToID("_HeightPoMAmplitude");
        private static Material CreateMaterial(Texture albedo, Texture normal, Texture height, Texture mask, Shader shader)
        {
            var material = new Material(shader);
            material.SetTexture(BaseColorMapProp, albedo);
            material.SetTexture(NormalMapProp, normal);
            material.SetTexture(HeightMapProp, height);
            material.SetTexture(MaskMapProp, mask);
            if (height)
            {
                material.SetInt(DisplacementModeProp, 2);
                material.SetFloat(HeightPoMAmplitudeProp, 1);
            }

            return material;
        }
    }
}
