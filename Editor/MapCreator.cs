using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace z3lx.ACGImporter.Editor
{
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

        public static Texture2D CreateMaskMap(Dictionary<MapType, Texture2D> maps)
        {
            ResetMaterial();

            SwizzleMat.SetTexture(Texture1, maps[MapType.Metallic]);
            SwizzleMat.SetTexture(Texture2, maps[MapType.Occlusion]);
            SwizzleMat.SetTexture(Texture4, maps[MapType.Roughness]);
            SwizzleMat.SetVector(Swizzle4, new Vector4(1, 0, 0, 0));
            SwizzleMat.SetFloat(Flip4, 1);

            return Render(maps);
        }

        public static Texture2D CreateMetallicSmoothnessMap(Dictionary<MapType, Texture2D> maps)
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
