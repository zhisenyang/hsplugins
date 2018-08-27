using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Studio;
using ToolBox;

namespace HSUS
{
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

}
