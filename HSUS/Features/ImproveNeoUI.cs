﻿using System.Xml;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ToolBox;
using ToolBox.Extensions;
#if IPA
using Harmony;
#elif BEPINEX
using HarmonyLib;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace HSUS.Features
{
    public class ImproveNeoUI : IFeature
    {
        internal static bool _improveNeoUI = true;

        public void Awake()
        {

        }

        public void LoadParams(XmlNode node)
        {
#if HONEYSELECT || KOIKATSU
            node = node.FindChildNode("improveNeoUI");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _improveNeoUI = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
#endif
        }

        public void SaveParams(XmlTextWriter writer)
        {
#if HONEYSELECT || KOIKATSU
            writer.WriteStartElement("improveNeoUI");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_improveNeoUI));
            writer.WriteEndElement();
#endif
        }

        public void LevelLoaded()
        {
#if HONEYSELECT || KOIKATSU
            if (_improveNeoUI && HSUS._self._binary == Binary.Studio &&
#if HONEYSELECT
                HSUS._self._level == 3
#elif KOIKATSU
                   HSUS._self._level == 1
#endif
            )
            {
                RectTransform rt = (RectTransform)GameObject.Find("StudioScene/Canvas Main Menu/01_Add/02_Item/Scroll View Item").transform;
                rt.offsetMax += new Vector2(60f, 0f);
                rt = (RectTransform)rt.Find("Viewport");
                rt.offsetMax += new Vector2(60f, 0f);
                rt = (RectTransform)rt.Find("Content");
                rt.offsetMax += new Vector2(60f, 0f);

                VerticalLayoutGroup group = rt.GetComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
#if HONEYSELECT
                group.padding = new RectOffset(group.padding.left + 4, group.padding.right + 24, group.padding.top, group.padding.bottom);
#elif KOIKATSU
                group.padding = new RectOffset(group.padding.left + 4, group.padding.right, group.padding.top, group.padding.bottom);
#endif
                rt.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                Text t = GameObject.Find("StudioScene/Canvas Main Menu/01_Add/02_Item/Scroll View Item/node/Text").GetComponent<Text>();
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 2;
                t.resizeTextMaxSize = 100;
            }
#endif
        }
#if HONEYSELECT
        
        [HarmonyPatch(typeof(Studio.AnimeControl), "OnEndEditSpeed")]
        public static class AnimeControl_OnEndEditSpeed_Patches
        {
            public static bool Prepare()
            {
                return _improveNeoUI;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo clampMethod = typeof(Mathf).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name.Equals("Clamp") && m.GetParameters()[0].ParameterType == typeof(float));
                List<CodeInstruction> instructionsList = instructions.ToList();
                bool set = false;
                foreach (CodeInstruction inst in instructionsList)
                {
                    if (inst.opcode == OpCodes.Call && inst.operand == clampMethod)
                        yield return new CodeInstruction(OpCodes.Call, typeof(AnimeControl_OnEndEditSpeed_Patches).GetMethod(nameof(FakeClamp), BindingFlags.NonPublic | BindingFlags.Static));
                    else if (set == false && inst.opcode == OpCodes.Ldstr)
                    {
                        yield return new CodeInstruction(OpCodes.Ldstr, "0.00####");
                        set = true;
                    }
                    else
                        yield return inst;
                }
            }

            private static float FakeClamp(float value, float min, float max)
            {
                return value;
            }
        }

        [HarmonyPatch(typeof(Studio.AnimeControl), "UpdateInfo")]
        public static class AnimeControl_UpdateInfo_Patches
        {
            public static bool Prepare()
            {
                return _improveNeoUI;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                bool set = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldstr)
                    {
                        yield return new CodeInstruction(OpCodes.Ldstr, "0.00####");
                        set = true;
                    }
                    else
                        yield return inst;
                }
            }
        }
        [HarmonyPatch(typeof(Studio.AnimeControl), "OnValueChangedSpeed")]
        public static class AnimeControl_OnValueChangedSpeed_Patches
        {
            public static bool Prepare()
            {
                return _improveNeoUI;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();
                bool set = false;
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    if (set == false && inst.opcode == OpCodes.Ldstr)
                    {
                        yield return new CodeInstruction(OpCodes.Ldstr, "0.00####");
                        set = true;
                    }
                    else
                        yield return inst;
                }
            }
        }
#endif
    }
}