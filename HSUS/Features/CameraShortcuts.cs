using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using Studio;
#if KOIKATSU
using ToolBox;
#endif
using UnityEngine;

namespace HSUS
{
    public static class CameraShortcuts
    {
        [HarmonyPatch(typeof(Studio.CameraControl), "InputMouseProc")]
        public static class CameraControl_InputMouseProc_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._cameraSpeedShortcuts;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (inst.opcode == OpCodes.Ldstr &&
                        instructionsList[i + 1].opcode == OpCodes.Call)
                    {
                        ++i;
                        yield return instructionsList[i]; //Call
                        yield return new CodeInstruction(OpCodes.Call, typeof(CameraControl_InputMouseProc_Patches).GetMethod(nameof(GetCameraMultiplier)));
                        yield return new CodeInstruction(OpCodes.Mul);
                    }

                }
            }

            public static float GetCameraMultiplier()
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    return 1f / 6f;
                if (Input.GetKey(KeyCode.LeftShift))
                    return 4f;
                return 1f;
            }
        }

        [HarmonyPatch(typeof(BaseCameraControl), "InputMouseProc")]
        public static class BaseCameraControl_InputMouseProc_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._cameraSpeedShortcuts;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (inst.opcode == OpCodes.Ldstr &&
                        instructionsList[i + 1].opcode == OpCodes.Call)
                    {
                        ++i;
                        yield return instructionsList[i]; //Call
                        yield return new CodeInstruction(OpCodes.Call, typeof(CameraControl_InputMouseProc_Patches).GetMethod(nameof(GetCameraMultiplier)));
                        yield return new CodeInstruction(OpCodes.Mul);
                    }

                }
            }

            public static float GetCameraMultiplier()
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    return 1f / 6f;
                if (Input.GetKey(KeyCode.LeftShift))
                    return 4f;
                return 1f;
            }
        }

        [HarmonyPatch(typeof(Studio.CameraControl), "InputKeyProc")]
        public static class CameraControl_InputKeyProc_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._cameraSpeedShortcuts;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                int set = 0;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (set < 2 && (inst.opcode == OpCodes.Call &&
                                    instructionsList[i + 1].opcode == OpCodes.Stloc_1 ||
                                    inst.opcode == OpCodes.Mul &&
                                    instructionsList[i + 1].opcode == OpCodes.Stloc_2))
                    {
                        yield return new CodeInstruction(OpCodes.Call, typeof(CameraControl_InputMouseProc_Patches).GetMethod(nameof(GetCameraMultiplier)));
                        yield return new CodeInstruction(OpCodes.Mul);
                        ++set;
                    }
                }
            }

            public static float GetCameraMultiplier()
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    return 1f / 6f;
                if (Input.GetKey(KeyCode.LeftShift))
                    return 4f;
                return 1f;
            }
        }

        [HarmonyPatch(typeof(ShortcutKeyCtrl), "Update")]
        public static class ShortcutKeyCtrl_Update_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._cameraSpeedShortcuts;
            }

#if HONEYSELECT
            public static void Postfix(Studio.CameraControl ___cameraControl)
            {
#elif KOIKATSU
        private static Studio.CameraControl ___cameraControl;
        public static void Postfix(ShortcutKeyCtrl __instance)
        {
            if (___cameraControl == null)
                ___cameraControl = (Studio.CameraControl)__instance.GetPrivate("cameraControl");
#endif
                if (!Studio.Studio.IsInstance() && Studio.Studio.Instance.isInputNow && !Manager.Scene.IsInstance() && Manager.Scene.Instance.AddSceneName != string.Empty)
                    return;
                if (!Studio.Studio.Instance.isVRMode && Input.GetKeyDown(KeyCode.F) && GuideObjectManager.Instance.selectObject != null)
                    ___cameraControl.targetPos = GuideObjectManager.Instance.selectObject.transformTarget.position;
            }
        }
    }
}
