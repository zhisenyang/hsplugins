using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
#if HONEYSELECT || PLAYHOME
using Studio;
using Harmony;
#elif KOIKATSU
using HarmonyLib;
using Studio;
#elif AISHOUJO
using HarmonyLib;
#endif
using Manager;
using ToolBox.Extensions;
using UnityEngine;
using Input = UnityEngine.Input;

namespace HSUS.Features
{
    public class CameraShortcuts : IFeature
    {
        private static bool _cameraSpeedShortcuts = true;

        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
            node = node.FindChildNode("cameraSpeedShortcuts");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _cameraSpeedShortcuts = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
        }

        public void SaveParams(XmlTextWriter writer)
        {
            writer.WriteStartElement("cameraSpeedShortcuts");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_cameraSpeedShortcuts));
            writer.WriteEndElement();
        }

        public void LevelLoaded()
        {
        }

        [HarmonyPatch(typeof(Studio.CameraControl), "InputMouseProc")]
        public static class CameraControl_InputMouseProc_Patches
        {
            public static bool Prepare()
            {
                return _cameraSpeedShortcuts;
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
                return _cameraSpeedShortcuts;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (inst.opcode == OpCodes.Ldstr && instructionsList[i + 1].opcode == OpCodes.Call)
                    {
                        ++i;
                        yield return instructionsList[i]; //Call
                        yield return new CodeInstruction(OpCodes.Call, typeof(BaseCameraControl_InputMouseProc_Patches).GetMethod(nameof(GetCameraMultiplier)));
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
                return _cameraSpeedShortcuts;
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
    }
}
