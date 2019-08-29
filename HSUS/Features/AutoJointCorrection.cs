#if HONEYSELECT
using System.Reflection;
#endif
using Harmony;
using Studio;

namespace HSUS
{
    public static class AutoJointCorrection
    {

#if HONEYSELECT
        [HarmonyPatch]
#elif KOIKATSU
    [HarmonyPatch(typeof(OICharInfo), new []{typeof(ChaFileControl), typeof(int)})]
#endif
        public class OICharInfo_Ctor_Patches
        {
#if HONEYSELECT
            internal static MethodBase TargetMethod()
            {
                return typeof(OICharInfo).GetConstructor(new[] { typeof(CharFile), typeof(int) });
            }
#endif

            public static bool Prepare()
            {
                return HSUS._self._autoJointCorrection;
            }

            public static void Postfix(OICharInfo __instance)
            {
                for (int i = 0; i < __instance.expression.Length; i++)
                    __instance.expression[i] = true;
            }
        }
    }
}
