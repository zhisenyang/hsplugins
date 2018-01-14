using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using HSUS;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace StudioFileCheck
{
    [HarmonyPatch(typeof(ItemList), "Awake")]
    public class ItemList_Awake_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS.self.optimizeNeo;
        }

        public static void Prefix(ItemList __instance)
        {
            ItemList_InitList_Patches.ItemListData data;
            if (ItemList_InitList_Patches._dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new ItemList_InitList_Patches.ItemListData();
                ItemList_InitList_Patches._dataByInstance.Add(__instance, data);
            }

            Transform transformRoot = (Transform)__instance.GetPrivate("transformRoot");

            RectTransform rt = transformRoot.parent as RectTransform;
            rt.offsetMin += new Vector2(0f, 18f);
            float newY = rt.offsetMin.y;

            data.searchBar = UIUtility.CreateInputField("Search Bar", transformRoot.parent.parent, "Search...");
            Image image = data.searchBar.GetComponent<Image>();
            image.color = UIUtility.grayColor;
            //image.sprite = null;
            rt = data.searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(HSUS.HSUS.self.improveNeoUI ? 10f : 7f, 16f), new Vector2(HSUS.HSUS.self.improveNeoUI ? -22f : -14f, newY));
            data.searchBar.onValueChanged.AddListener(s => SearchChanged(__instance));
            foreach (Text t in data.searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
        }

        public static void ResetSearch(ItemList instance)
        {
            ItemList_InitList_Patches.ItemListData data;
            if (ItemList_InitList_Patches._dataByInstance.TryGetValue(instance, out data) == false)
            {
                data = new ItemList_InitList_Patches.ItemListData();
                ItemList_InitList_Patches._dataByInstance.Add(instance, data);
            }
            if (data.searchBar != null)
            {
                data.searchBar.text = "";
                SearchChanged(instance);
            }
        }

        private static void SearchChanged(ItemList instance)
        {
            ItemList_InitList_Patches.ItemListData data;
            if (ItemList_InitList_Patches._dataByInstance.TryGetValue(instance, out data) == false)
            {
                data = new ItemList_InitList_Patches.ItemListData();
                ItemList_InitList_Patches._dataByInstance.Add(instance, data);
            }
            string search = data.searchBar.text.Trim();
            int currentGroup = (int)instance.GetPrivate("group");
            List<StudioNode> list;
            if (data.objects.TryGetValue(currentGroup, out list) == false)
                return;
            foreach (StudioNode objectData in list)
            {
                objectData.active = objectData.textUI.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1;
            }
        }
    }

    [HarmonyPatch(typeof(ItemList), "InitList", new[] { typeof(int) })]
    public class ItemList_InitList_Patches
    {
        public class ItemListData
        {
            public readonly Dictionary<int, List<StudioNode>> objects = new Dictionary<int, List<StudioNode>>();
            public InputField searchBar;
        }

        public static readonly Dictionary<ItemList, ItemListData> _dataByInstance = new Dictionary<ItemList, ItemListData>();

        public static bool Prepare()
        {
            return HSUS.HSUS.self.optimizeNeo;
        }

        public static void Prefix(ItemList __instance, int _group)
        {
            ItemListData data;
            if (_dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new ItemListData();
                _dataByInstance.Add(__instance, data);
            }

            ItemList_Awake_Patches.ResetSearch(__instance);

            int currentGroup = (int)__instance.GetPrivate("group");
            if (currentGroup != _group)
            {
                ((ScrollRect)__instance.GetPrivate("scrollRect")).verticalNormalizedPosition = 1f;

                List<StudioNode> list;
                if (data.objects.TryGetValue(currentGroup, out list))
                    foreach (StudioNode studioNode in list)
                        studioNode.active = false;
                if (data.objects.TryGetValue(_group, out list))
                    foreach (StudioNode studioNode in list)
                        studioNode.active = true;
                else
                {
                    list = new List<StudioNode>();
                    data.objects.Add(_group, list);

                    Transform transformRoot = (Transform)__instance.GetPrivate("transformRoot");
                    GameObject objectNode = (GameObject)__instance.GetPrivate("objectNode");
                    foreach (KeyValuePair<int, Info.ItemLoadInfo> item in Singleton<Info>.Instance.dicItemLoadInfo)
                    {
                        if (item.Value.group != _group)
                            continue;
                        GameObject gameObject = Object.Instantiate(objectNode);
                        gameObject.transform.SetParent(transformRoot, false);
                        StudioNode component = gameObject.GetComponent<StudioNode>();
                        component.active = true;
                        int no = item.Key;
                        component.addOnClick = delegate { __instance.OnSelect(no); };
                        component.text = item.Value.name;
                        component.textColor = ((!(item.Value.isColor & item.Value.isColor2)) ? ((!(item.Value.isColor | item.Value.isColor2)) ? Color.white : Color.red) : Color.cyan);
                        if (item.Value.isColor || item.Value.isColor2)
                        {
                            Shadow shadow = (component.textUI).gameObject.AddComponent<Shadow>();
                            shadow.effectColor = Color.black;
                        }
                        list.Add(component);
                    }
                }
                if (!__instance.gameObject.activeSelf)
                    __instance.gameObject.SetActive(true);
                __instance.SetPrivate("group", _group);
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
