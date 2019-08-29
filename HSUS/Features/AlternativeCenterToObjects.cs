#if HONEYSELECT
using System;
using System.Reflection;
using Harmony;
using Manager;
using Studio;

namespace HSUS
{
    public static class AlternativeCenterToObjects
    {
        [HarmonyPatch]
        public static class HSSNAShortcutKeyCtrlOverride_Update_Patches
        {
            private static MethodInfo _getKeyDownMethod;
            private static readonly object[] _params = { 4 };

            private static bool Prepare()
            {
                Type t = Type.GetType("HSStudioNEOAddon.ShortcutKey.HSSNAShortcutKeyCtrlOverride,HSStudioNEOAddon");
                if (HSUS._self._binary == HSUS.Binary.Neo && HSUS._self._alternativeCenterToObject && t != null)
                {
                    _getKeyDownMethod = t.GetMethod("GetKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
                    return true;
                }
                return false;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("HSStudioNEOAddon.ShortcutKey.HSSNAShortcutKeyCtrlOverride,HSStudioNEOAddon").GetMethod("Update");
            }

            public static void Postfix(object __instance, Studio.CameraControl ___cameraControl)
            {
                if (!Studio.Studio.IsInstance() && Studio.Studio.Instance.isInputNow && !Scene.IsInstance() && Scene.Instance.AddSceneName != string.Empty)
                    return;
                if (!Studio.Studio.Instance.isVRMode && _getKeyDownMethod != null && (bool)_getKeyDownMethod.Invoke(__instance, _params) && GuideObjectManager.Instance.selectObject != null)
                    ___cameraControl.targetPos = GuideObjectManager.Instance.selectObject.transformTarget.position;
            }
        }
    }
}
#endif
