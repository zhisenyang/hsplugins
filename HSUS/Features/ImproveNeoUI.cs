using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class ImproveNeoUI
    {
        public static void Do()
        {
            RectTransform rt = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item") as RectTransform;
            rt.offsetMax += new Vector2(60f, 0f);
            rt = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport") as RectTransform;
            rt.offsetMax += new Vector2(60f, 0f);
            rt = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content") as RectTransform;
            rt.offsetMax += new Vector2(60f, 0f);

            VerticalLayoutGroup group = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content").GetComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.padding = new RectOffset(group.padding.left + 4, group.padding.right + 24, group.padding.top, group.padding.bottom);
            GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content").GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            Text t = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/node/Text").GetComponent<Text>();
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 2;
            t.resizeTextMaxSize = 100;
        }
#if HONEYSELECT
        
        [HarmonyPatch(typeof(Studio.AnimeControl), "OnEndEditSpeed")]
        public static class AnimeControl_OnEndEditSpeed_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._improveNeoUI;
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
        [HarmonyPatch(typeof(Studio.AnimeControl), "UpdateInfo")]
        public static class AnimeControl_UpdateInfo_Patches
        {
            public static bool Prepare()
            {
                return HSUS._self._improveNeoUI;
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
                return HSUS._self._improveNeoUI;
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
                        yield return new CodeInstruction(OpCodes.Ldstr, "0.00##");
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
