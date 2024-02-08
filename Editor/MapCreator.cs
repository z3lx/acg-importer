using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace z3lx.ACGImporter.Editor
{
    /// <summary>
    /// Provides static methods for creating different types of maps from provided textures.
    /// </summary>
    public static class MapCreator
    {
        private static readonly Material SwizzleMat =
            new(Shader.Find("Hidden/acg-importer/Swizzle"));

        private static readonly int Texture1 = Shader.PropertyToID("_MainTex1");
        private static readonly int Texture2 = Shader.PropertyToID("_MainTex2");
        private static readonly int Texture3 = Shader.PropertyToID("_MainTex3");
        private static readonly int Texture4 = Shader.PropertyToID("_MainTex4");

        private static readonly int Swizzle1 = Shader.PropertyToID("_Swizzle1");
        private static readonly int Swizzle2 = Shader.PropertyToID("_Swizzle2");
        private static readonly int Swizzle3 = Shader.PropertyToID("_Swizzle3");
        private static readonly int Swizzle4 = Shader.PropertyToID("_Swizzle4");

        private static readonly int Flip1 = Shader.PropertyToID("_Flip1");
        private static readonly int Flip2 = Shader.PropertyToID("_Flip2");
        private static readonly int Flip3 = Shader.PropertyToID("_Flip3");
        private static readonly int Flip4 = Shader.PropertyToID("_Flip4");

        /// <summary>
        /// Resets the SwizzleMat material to its default state.
        /// </summary>
        private static void ResetMaterial()
        {
            SwizzleMat.SetTexture(Texture1, null);
            SwizzleMat.SetTexture(Texture2, null);
            SwizzleMat.SetTexture(Texture3, null);
            SwizzleMat.SetTexture(Texture4, null);

            SwizzleMat.SetVector(Swizzle1, new Vector4(1, 0, 0, 0));
            SwizzleMat.SetVector(Swizzle2, new Vector4(0, 1, 0, 0));
            SwizzleMat.SetVector(Swizzle3, new Vector4(0, 0, 1, 0));
            SwizzleMat.SetVector(Swizzle4, new Vector4(0, 0, 0, 1));

            SwizzleMat.SetFloat(Flip1, 0);
            SwizzleMat.SetFloat(Flip2, 0);
            SwizzleMat.SetFloat(Flip3, 0);
            SwizzleMat.SetFloat(Flip4, 0);
        }

        /// <summary>
        /// Renders a Texture2D using the SwizzleMat material.
        /// </summary>
        /// <param name="maps">A read-only dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <returns>A rendered Texture2D object.</returns>
        private static Texture2D Render(IReadOnlyDictionary<MapType, Texture2D> maps)
        {
            var map = maps[MapType.Color];
            var rt = RenderTexture.GetTemporary(
                map.width,
                map.height,
                0,
                GraphicsFormat.R8G8B8A8_UNorm
            );
            Graphics.Blit(null, rt, SwizzleMat);
            var t = ToTexture2D(rt);
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }

        /// <summary>
        /// Converts a RenderTexture to a Texture2D.
        /// </summary>
        /// <param name="rt">The RenderTexture to be converted.</param>
        /// <returns>A converted Texture2D object.</returns>
        private static Texture2D ToTexture2D(RenderTexture rt)
        {
            var t = new Texture2D(
                rt.width,
                rt.height,
                GraphicsFormatUtility.GetGraphicsFormat(rt.format, rt.sRGB),
                TextureCreationFlags.None
            );
            var oldActive = RenderTexture.active;
            RenderTexture.active = rt;
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = oldActive;
            return t;
        }

        /// <summary>
        /// Creates a mask map from the provided maps.
        /// </summary>
        /// <param name="maps">A read-only dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <returns>A Texture2D object that represents the created mask map.</returns>
        public static Texture2D CreateMaskMap(IReadOnlyDictionary<MapType, Texture2D> maps)
        {
            ResetMaterial();

            SwizzleMat.SetTexture(Texture1, maps[MapType.Metallic]);
            SwizzleMat.SetTexture(Texture2, maps[MapType.Occlusion] == null
                ? Texture2D.whiteTexture
                : maps[MapType.Occlusion]);
            SwizzleMat.SetTexture(Texture4, maps[MapType.Roughness]);
            SwizzleMat.SetVector(Swizzle4, new Vector4(1, 0, 0, 0));
            SwizzleMat.SetFloat(Flip4, 1);

            return Render(maps);
        }

        /// <summary>
        /// Creates a smoothness map from the provided maps.
        /// </summary>
        /// <param name="maps">A read-only dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <returns>A Texture2D object that represents the created smoothness map.</returns>
        public static Texture2D CreateSmoothnessMap(IReadOnlyDictionary<MapType, Texture2D> maps)
        {
            ResetMaterial();

            SwizzleMat.SetTexture(Texture1, maps[MapType.Roughness]);
            SwizzleMat.SetTexture(Texture2, maps[MapType.Roughness]);
            SwizzleMat.SetTexture(Texture3, maps[MapType.Roughness]);
            SwizzleMat.SetTexture(Texture4, maps[MapType.Roughness]);
            SwizzleMat.SetFloat(Flip1, 1);
            SwizzleMat.SetFloat(Flip2, 1);
            SwizzleMat.SetFloat(Flip3, 1);

            return Render(maps);
        }

        /// <summary>
        /// Creates a metallic gloss map from the provided maps.
        /// </summary>
        /// <param name="maps">A read-only dictionary that holds the mapping between MapType and Texture2D.</param>
        /// <returns>A Texture2D object that represents the created metallic gloss map.</returns>
        public static Texture2D CreateMetallicGlossMap(IReadOnlyDictionary<MapType, Texture2D> maps)
        {
            ResetMaterial();

            SwizzleMat.SetTexture(Texture1, maps[MapType.Metallic]);
            SwizzleMat.SetTexture(Texture2, maps[MapType.Metallic]);
            SwizzleMat.SetTexture(Texture3, maps[MapType.Metallic]);
            SwizzleMat.SetTexture(Texture4, maps[MapType.Roughness]);
            SwizzleMat.SetVector(Swizzle4, new Vector4(1, 0, 0, 0));
            SwizzleMat.SetFloat(Flip4, 1);

            return Render(maps);
        }
    }
}
