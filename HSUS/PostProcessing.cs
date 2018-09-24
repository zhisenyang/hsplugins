using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using Studio;
using ToolBox;
using UnityEngine;

namespace HSUS
{
#if HONEYSELECT
    [HarmonyPatch(typeof(SystemButtonCtrl), "Init")]
    [HarmonyPatch(typeof(SystemButtonCtrl), "OnSelectInitYes")]
    public class SystemButtonCtrl_Init_Patches
    {
        public static void Postfix(SystemButtonCtrl __instance)
        {
            __instance.GetPrivate("dofInfo").CallPrivate("OnValueChangedEnable", HSUS.self.dofEnabled);
            __instance.GetPrivate("ssaoInfo").CallPrivate("OnValueChangedEnable", HSUS.self.ssaoEnabled);
            __instance.GetPrivate("bloomInfo").CallPrivate("OnValueChangedEnable", HSUS.self.bloomEnabled);
            __instance.GetPrivate("ssrInfo").CallPrivate("OnValueChangedEnable", HSUS.self.ssrEnabled);
            __instance.GetPrivate("vignetteInfo").CallPrivate("OnValueChangedEnable", HSUS.self.vignetteEnabled);
            __instance.GetPrivate("fogInfo").CallPrivate("OnValueChangedEnable", HSUS.self.fogEnabled);
            __instance.GetPrivate("sunShaftsInfo").CallPrivate("OnValueChangedEnable", HSUS.self.sunShaftsEnabled);
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
            m_ColorGrading.SetPrivate("useDithering", HSUS.self.fourKManagerDithering);
            __instance.SetPrivate("m_ColorGrading", m_ColorGrading);
        }
    }
#endif
}
