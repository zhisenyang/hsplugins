#if HONEYSELECT
using System;
using System.Reflection;
#endif
using Harmony;

namespace HSUS
{
    public static class EyesBlink
    {
#if HONEYSELECT
        [HarmonyPatch]
#elif KOIKATSU
    [HarmonyPatch(typeof(ChaFileStatus))]
#endif
        public static class CharFileInfoStatus_Ctor_Patches
        {
#if HONEYSELECT
            internal static MethodBase TargetMethod()
            {
                return typeof(CharFileInfoStatus).GetConstructor(new Type[] { });
            }

            public static void Postfix(CharFileInfoStatus __instance)
#elif KOIKATSU
        public static void Postfix(ChaFileStatus __instance)
#endif
            {
                __instance.eyesBlink = HSUS._self._eyesBlink;
            }
        }
    }
}
