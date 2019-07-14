using System;
using System.IO;
using Harmony;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using Object = UnityEngine.Object;

namespace HSLRE
{
    internal static class VariousPatches
    {
        [HarmonyPatch(typeof(DepthOfField), "OnEnable")]
        private static class DepthOfField_OnEnable_Patches
        {
            private static void Postfix(DepthOfField __instance)
            {
                __instance.highResolution = true;
            }
        }

        [HarmonyPatch(typeof(CustomTextureControl), "Initialize")]
        private static class CustomTextureControl_Initialize_Patches
        {
            private static void Prefix(ref int width, ref int height)
            {
                width = 4096;
                height = 4096;
            }
        }

        [HarmonyPatch(typeof(CustomTextureControl), "SetMainTexture", typeof(Texture))]
        private static class CustomTextureControl_SetMainTexture_Patches
        {
            private static void Prefix(Texture tex, ref RenderTexture ___createTex)
            {
                if (___createTex.width != tex.width || ___createTex.height != tex.height)
                {
                    int width = tex.width;
                    int height = tex.height;
                    if (Mathf.IsPowerOfTwo(width) == false)
                        width = Mathf.Min(Mathf.NextPowerOfTwo(width), 8192);
                    if (Mathf.IsPowerOfTwo(height) == false)
                        height = Mathf.Min(Mathf.NextPowerOfTwo(height), 8192);
                    RenderTexture newRenderTexture = new RenderTexture(width, height, 0);
                    ___createTex.Release();
                    ___createTex = newRenderTexture;
                }
            }
        }

        [HarmonyPatch(typeof(CustomTextureControl), "SetOffsetAndTiling", typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float))]
        private static class CustomTextureControl_SetOffsetAndTiling_Patches
        {
            private static void Prefix(string propertyName, ref int baseW, ref int baseH, int addW, int addH, ref float addPx, ref float addPy)
            {
                if (addPx >= 0f & addPy >= 0f & (float)addW + addPx <= 1024f & (float)addH + addPy <= 1024f)
                {
                    baseW = 1024;
                    baseH = 1024;
                }
                else
                {
                    addPx = Mathf.Abs(addPx);
                    addPy = Mathf.Abs(addPy);
                    if (addW > 4096f || addH > 4096f)
                    {
                        baseH = 8192;
                        baseW = 8192;
                    }
                    else
                    {
                        baseH = 4096;
                        baseW = 4096;
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(ColorCorrectionCurves), "UpdateParameters")]
        private static class ColorCorrectionCurves_UpdateParameters_Patches
        {
            private static bool Prefix(ColorCorrectionCurves __instance, ref Texture2D ___rgbChannelTex)
            {
                __instance.CheckResources();
                if (Settings.curveSettings == null ||
                    Settings.curveSettings.Curve == 0 ||
                    File.Exists(Path.Combine(Application.dataPath, "../UserData/curve/" + Settings.curveSettings.CurveName + ".dds")) == false)
                {
                    if (___rgbChannelTex.width != 2048)
                    {
                        Object.Destroy(___rgbChannelTex);
                        ___rgbChannelTex = new Texture2D(2048, 4, TextureFormat.RGBAFloat, false, true);
                        ___rgbChannelTex.wrapMode = TextureWrapMode.Clamp;
                        ___rgbChannelTex.hideFlags = HideFlags.DontSave;
                    }


                    for (int i = 0; i < 2048; i++)
                    {
                        float num = i / 2047f;
                        float num2 = Mathf.Clamp(__instance.redChannel.Evaluate(num), 0f, 1f);
                        float num3 = Mathf.Clamp(__instance.greenChannel.Evaluate(num), 0f, 1f);
                        float num4 = Mathf.Clamp(__instance.blueChannel.Evaluate(num), 0f, 1f);
                        ___rgbChannelTex.SetPixel(i, 0, new Color(num2, num2, num2));
                        ___rgbChannelTex.SetPixel(i, 1, new Color(num3, num3, num3));
                        ___rgbChannelTex.SetPixel(i, 2, new Color(num4, num4, num4));

                    }
                    ___rgbChannelTex.Apply(false);
                }
                else
                {
                    if (___rgbChannelTex.width != 1024)
                    {
                        Object.Destroy(___rgbChannelTex);
                        ___rgbChannelTex = new Texture2D(1024, 4, TextureFormat.RGBAFloat, false, true);
                        ___rgbChannelTex.wrapMode = TextureWrapMode.Clamp;
                        ___rgbChannelTex.hideFlags = HideFlags.DontSave;
                    }

                    string text = Path.Combine(Application.dataPath, "../UserData/curve/" + Settings.curveSettings.CurveName + ".dds");
                    UnityEngine.Debug.Log("Using custom curve " + text);
                    byte[] array = File.ReadAllBytes(text);
                    int num5 = 128;
                    byte[] array2 = new byte[array.Length - num5];
                    Buffer.BlockCopy(array, num5, array2, 0, array2.Length - num5);
                    ___rgbChannelTex.LoadRawTextureData(array2);
                    ___rgbChannelTex.Apply(false);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(NoiseAndGrain), "CheckResources")]
        private static class NoiseAndGrain_CheckResources_Patches
        {
            private static void Prefix(NoiseAndGrain __instance)
            {
                if (__instance.noiseShader == null)
                    __instance.noiseShader = HSLRE.self._resources.LoadAsset<Shader>("NoiseAndGrain");
                if (__instance.dx11NoiseShader == null)
                    __instance.dx11NoiseShader = HSLRE.self._resources.LoadAsset<Shader>("NoiseAndGrainDX11");
                if (__instance.noiseTexture == null)
                    __instance.noiseTexture = HSLRE.self._resources.LoadAsset<Texture2D>("NoiseAndGrain");
            }
        }

        [HarmonyPatch(typeof(BlurOptimized), "CheckResources")]
        private static class BlurOptimized_CheckResources_Patches
        {
            private static void Prefix(BlurOptimized __instance)
            {
                if (__instance.blurShader == null)
                    __instance.blurShader = HSLRE.self._resources.LoadAsset<Shader>("MobileBlur");
            }
        }

        [HarmonyPatch(typeof(CameraMotionBlur), "CheckResources")]
        private static class CameraMotionBlur_CheckResources_Patches
        {
            private static void Prefix(CameraMotionBlur __instance)
            {
                if (__instance.shader == null)
                    __instance.shader = HSLRE.self._resources.LoadAsset<Shader>("CameraMotionBlur");
                if (__instance.replacementClear == null)
                    __instance.replacementClear = HSLRE.self._resources.LoadAsset<Shader>("MotionBlurClear");
                if (__instance.dx11MotionBlurShader == null)
                    __instance.dx11MotionBlurShader = HSLRE.self._resources.LoadAsset<Shader>("CameraMotionBlurDX11");
                if (__instance.noiseTexture == null)
                    __instance.noiseTexture = HSLRE.self._resources.LoadAsset<Texture2D>("MotionBlurJitter");
            }
        }


    }
}
