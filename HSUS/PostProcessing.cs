using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using Studio;
using ToolBox;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace HSUS
{
#if HONEYSELECT
    [HarmonyPatch(typeof(SystemButtonCtrl), "Init")]
    [HarmonyPatch(typeof(SystemButtonCtrl), "OnSelectInitYes")]
    public class SystemButtonCtrl_Init_Patches
    {
        public static void Postfix(SystemButtonCtrl __instance)
        {
            __instance.GetPrivate("dofInfo").CallPrivate("OnValueChangedEnable", HSUS._self._dofEnabled);
            __instance.GetPrivate("ssaoInfo").CallPrivate("OnValueChangedEnable", HSUS._self._ssaoEnabled);
            __instance.GetPrivate("bloomInfo").CallPrivate("OnValueChangedEnable", HSUS._self._bloomEnabled);
            __instance.GetPrivate("ssrInfo").CallPrivate("OnValueChangedEnable", HSUS._self._ssrEnabled);
            __instance.GetPrivate("vignetteInfo").CallPrivate("OnValueChangedEnable", HSUS._self._vignetteEnabled);
            __instance.GetPrivate("fogInfo").CallPrivate("OnValueChangedEnable", HSUS._self._fogEnabled);
            __instance.GetPrivate("sunShaftsInfo").CallPrivate("OnValueChangedEnable", HSUS._self._sunShaftsEnabled);
        }
    }

    public class TonemappingColorGrading_Ctor_Patches
    {
        public static void ManualPatch(HarmonyInstance harmony)
        {
            Type t = Type.GetType("UnityStandardAssets.CinematicEffects.TonemappingColorGrading,4KManager");
            if (t == null)
                return;
            harmony.Patch(t.GetConstructor(new Type[0]), null, new HarmonyMethod(typeof(TonemappingColorGrading_Ctor_Patches), "Postfix"));
        }
        public static void Postfix(object __instance)
        {
            object m_ColorGrading = __instance.GetPrivate("m_ColorGrading");
            m_ColorGrading.SetPrivate("useDithering", HSUS._self._fourKManagerDithering);
            __instance.SetPrivate("m_ColorGrading", m_ColorGrading);
        }
    }

    [HarmonyPatch(typeof(ColorCorrectionCurves), "OnRenderImage", new[] { typeof(RenderTexture), typeof(RenderTexture) }), HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
    public class ColorCorrectionCurves_OnRenderImage_Patches
    {
        private static MethodInfo _smaaMethod;
        private static MethodInfo _bloomMethod;
        private static MethodInfo _tonemappingMethod;
        private static MethodInfo _aberrationMethod;

        public static bool Prepare()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("4KManager"))
                {
                    _smaaMethod = assembly.GetType("UnityStandardAssets.CinematicEffects.SMAA").GetMethod("OnRenderImage", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    _bloomMethod = assembly.GetType("UnityStandardAssets.CinematicEffects.Bloom").GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    _tonemappingMethod = assembly.GetType("UnityStandardAssets.CinematicEffects.TonemappingColorGrading").GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    _aberrationMethod = assembly.GetType("UnityStandardAssets.CinematicEffects.LensAberrations").GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    return true;
                    
                }
            }
            return false;
        }

        public static bool Prefix(ColorCorrectionCurves __instance, RenderTexture source, RenderTexture destination, object ___m_SMAA, object ___CinematicBloom, object ___Tonemapping, object ___LensAberrations)
        {
            RenderTexture temporary1 = RenderTexture.GetTemporary(source.width, source.height, source.depth, source.format);
            RenderTexture temporary2 = RenderTexture.GetTemporary(source.width, source.height, source.depth, source.format);
            RenderTexture temporary3 = RenderTexture.GetTemporary(source.width, source.height, source.depth, source.format);
            _smaaMethod.Invoke(___m_SMAA, new object[] {__instance.cameraComponent, source, temporary1});
            if (___CinematicBloom != null)
                _bloomMethod.Invoke(___CinematicBloom, new object[]{ temporary1, temporary2 });
            else
                Graphics.Blit(temporary1, temporary2);
            if (___Tonemapping != null)
                _tonemappingMethod.Invoke(___Tonemapping, new object[] { temporary2, temporary3 });
            else
                Graphics.Blit(temporary2, temporary3);
            if (___LensAberrations != null)
                _aberrationMethod.Invoke(___LensAberrations, new object[] { temporary3, destination });
            else
                Graphics.Blit(temporary3, destination);
            RenderTexture.ReleaseTemporary(temporary1);
            RenderTexture.ReleaseTemporary(temporary2);
            RenderTexture.ReleaseTemporary(temporary3);
            return false;
        }
    }
#endif
}
