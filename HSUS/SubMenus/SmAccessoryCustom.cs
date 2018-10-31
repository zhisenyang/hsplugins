﻿#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using CustomMenu;
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmAccessory_Data
    {
        public class TypeData
        {
            public RectTransform parentObject;
            public List<ObjectData> objects = new List<ObjectData>();
        }

        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static readonly Dictionary<int, TypeData> types = new Dictionary<int, TypeData>();
        public static int lastType;
        public static int lastId;
        public static RectTransform container;
        public static InputField searchBar;
        internal static readonly List<IEnumerator> _methods = new List<IEnumerator>();
        internal static PropertyInfo _translateProperty = null;

        private static SmAccessory _originalComponent;

        public static void Init(SmAccessory originalComponent)
        {
            Reset();

            _originalComponent = originalComponent;
            container = _originalComponent.transform.FindDescendant("ListTop").transform as RectTransform;
            VerticalLayoutGroup group = container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _originalComponent.rtfPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = _originalComponent.rtfPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            RectTransform rt = _originalComponent.transform.FindChild("TabControl/TabItem01/ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);
            float newY = rt.offsetMax.y;
            rt = _originalComponent.transform.FindChild("TabControl/TabItem01/Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);

            searchBar = UIUtility.CreateInputField("Search Bar", _originalComponent.transform.FindChild("TabControl/TabItem01"));
            searchBar.GetComponent<Image>().sprite = HSUS.self.searchBarBackground;
            foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;

            rt = searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            searchBar.placeholder.GetComponent<Text>().text = "Search...";
            searchBar.onValueChanged.AddListener(SearchChanged);

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (HSUS.self._asyncLoading)
            {
                CharInfo chaInfo = originalComponent.customControl.chainfo;
                for (int i = 0; i < 12; i++)
                {
                    IEnumerator method = SmAccessory_ChangeAccessoryTypeList_Patches.ChangeAccessoryTypeList(originalComponent, i, chaInfo);
                    _methods.Add(method);
                    HSUS._self._asyncMethods.Add(method);
                }
            }
            else
            {
                for (int i = 0; i < 12; i++)
                    _methods.Add(null);
            }

        }

        private static void Reset()
        {
            types.Clear();
            _methods.Clear();
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            if (types.ContainsKey(lastType) == false)
                return;
            foreach (ObjectData objectData in types[lastType].objects)
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }
    }

    [HarmonyPatch(typeof(SmAccessory))]
    [HarmonyPatch("ChangeAccessoryTypeList")]
    [HarmonyPatch(new []{typeof(int), typeof(int)})]
    public class SmAccessory_ChangeAccessoryTypeList_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmAccessory __instance, int newType, int newId, CharInfo ___chaInfo, CharFileInfoClothes ___clothesInfo)
        {
            __instance.SetPrivate("acsType", newType);
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            int slotNoFromSubMenuSelect = (int)__instance.CallPrivate("GetSlotNoFromSubMenuSelect");
            int count = 0;
            int selectedIndex = 0;
            SmAccessory_Data.TypeData td;
            if (SmAccessory_Data.lastType != nowSubMenuTypeId && SmAccessory_Data.types.TryGetValue(SmAccessory_Data.lastType, out td))
                td.parentObject.gameObject.SetActive(false);
            if (newType == -1)
            {
                __instance.rtfPanel.sizeDelta = new Vector2(__instance.rtfPanel.sizeDelta.x, 0f);
                __instance.rtfPanel.anchoredPosition = new Vector2(0f, 0f);
                if (__instance.tab02)
                    __instance.tab02.gameObject.SetActive(false);
                if (__instance.tab03)
                    __instance.tab03.gameObject.SetActive(false);
                if (__instance.tab04)
                    __instance.tab04.gameObject.SetActive(false);
            }
            else
            {
                IEnumerator method = SmAccessory_Data._methods[newType];
                if (method == null || method.MoveNext() == false)
                    method = ChangeAccessoryTypeList(__instance, newType, ___chaInfo);

                while (method.MoveNext()) ;

                td = SmAccessory_Data.types[newType];

                td.parentObject.gameObject.SetActive(true);
                foreach (SmAccessory_Data.ObjectData o in td.objects)
                {
                    if (count == 0)
                        __instance.SetPrivate("firstIndex", o.key);
                    if (newId == -1)
                        o.toggle.isOn = count == 0;
                    else if (o.key == newId)
                        o.toggle.isOn = true;
                    else
                        o.toggle.isOn = false;
                    if (o.toggle.isOn)
                    {
                        selectedIndex = count;
                        o.toggle.onValueChanged.Invoke(true);
                    }
                    ++count;
                }

                float b = 24f * count - 168f;
                float y = Mathf.Min(24f * selectedIndex, b);
                __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

                if (__instance.tab02)
                {
                    __instance.tab02.gameObject.SetActive(true);
                }
                if (__instance.tab03)
                {
                    __instance.tab03.gameObject.SetActive(true);
                }
            }
            __instance.SetPrivate("nowChanging", true);
            if (___clothesInfo != null)
            {
                float specularIntensity = ___clothesInfo.accessory[slotNoFromSubMenuSelect].color.specularIntensity;
                float specularSharpness = ___clothesInfo.accessory[slotNoFromSubMenuSelect].color.specularSharpness;
                float specularSharpness2 = ___clothesInfo.accessory[slotNoFromSubMenuSelect].color2.specularSharpness;
                if (__instance.sldIntensity)
                    __instance.sldIntensity.value = specularIntensity;
                if (__instance.inputIntensity)
                    __instance.inputIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", specularIntensity);
                if (__instance.sldSharpness[0])
                    __instance.sldSharpness[0].value = specularSharpness;
                if (__instance.inputSharpness[0])
                    __instance.inputSharpness[0].text = (string)__instance.CallPrivate("ChangeTextFromFloat", specularSharpness);
                if (__instance.sldSharpness[1])
                    __instance.sldSharpness[1].value = specularSharpness2;
                if (__instance.inputSharpness[1])
                    __instance.inputSharpness[1].text = (string)__instance.CallPrivate("ChangeTextFromFloat", specularSharpness2);
            }
            __instance.SetPrivate("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);
            SmAccessory_Data.lastType = newType;
            SmAccessory_Data.lastId = newId;
            return false;
        }

        internal static IEnumerator ChangeAccessoryTypeList(SmAccessory __instance, int newType, CharInfo chaInfo)
        {
            if (SmAccessory_Data.types.ContainsKey(newType))
                yield break;
            SmAccessory_Data.TypeData td = new SmAccessory_Data.TypeData();
            td.parentObject = new GameObject("Type " + newType, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
            td.parentObject.SetParent(__instance.objListTop.transform, false);
            td.parentObject.localScale = Vector3.one;
            td.parentObject.localPosition = Vector3.zero;
            td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SmAccessory_Data.types.Add(newType, td);
            ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
            td.parentObject.gameObject.SetActive(false);

            Dictionary<int, ListTypeFbx> dictionary = null;
            CharaListInfo.TypeAccessoryFbx type = (CharaListInfo.TypeAccessoryFbx)((int)Enum.ToObject(typeof(CharaListInfo.TypeAccessoryFbx), newType));
            dictionary = chaInfo.ListInfo.GetAccessoryFbxList(type, true);
            int count = 0;
            foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
            {
                bool flag = false;
                if (chaInfo.customInfo.isConcierge)
                    flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0 && !flag)
                    continue;
                if (chaInfo.Sex == 0)
                {
                    if ("0" == current.Value.PrefabM)
                        continue;
                }
                else if ("0" == current.Value.PrefabF)
                    continue;
                if (count == 0)
                    __instance.SetPrivate("firstIndex", current.Key);
                GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                fbxTypeInfo.id = current.Key;
                fbxTypeInfo.typeName = current.Value.Name;
                fbxTypeInfo.info = current.Value;
                gameObject.transform.SetParent(td.parentObject, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = Vector3.one;
                Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                if (HSUS._self._asyncLoading && SmAccessory_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmAccessory_Data._translateProperty.SetValue(component, false, null);
                    string t = fbxTypeInfo.typeName;
                    HSUS._self._translationMethod(ref t);
                    component.text = t;
                }
                else
                    component.text = fbxTypeInfo.typeName;

                __instance.CallPrivate("SetButtonClickHandler", gameObject);
                Toggle component2 = gameObject.GetComponent<Toggle>();
                td.objects.Add(new SmAccessory_Data.ObjectData {obj = gameObject, key = current.Key, toggle = component2, text = component});
                component2.onValueChanged.AddListener(v =>
                {
                    if (component2.isOn)
                        UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                });
                component2.group = group;
                gameObject.SetActive(true);
                if (!flag)
                {
                    int num3 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                    Transform transform = rectTransform.FindChild("imgNew");
                    if (transform && num3 == 1)
                        transform.gameObject.SetActive(true);
                }
                count++;
                yield return null;
            }
        }
    }

    [HarmonyPatch(typeof(SmAccessory))]
    [HarmonyPatch("SetCharaInfo")]
    [HarmonyPatch(new[] {typeof(int), typeof(bool)})]
    public class SmAccessory_SetCharaInfo_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(int smTypeId, bool sameSubMenu)
        {
            if (SmAccessory_Data.searchBar != null)
            {
                SmAccessory_Data.searchBar.text = "";
                SmAccessory_Data.SearchChanged("");
            }
        }
    }

    [HarmonyPatch(typeof(SmAccessory))]
    [HarmonyPatch("OnChangeAccessoryType")]
    [HarmonyPatch(new[] { typeof(int) })]
    public class SmAccessory_OnChangeAccessoryType_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(int newType)
        {
            if (SmAccessory_Data.searchBar != null)
            {
                SmAccessory_Data.searchBar.text = "";
                SmAccessory_Data.SearchChanged("");
            }
        }
    }
}

#endif