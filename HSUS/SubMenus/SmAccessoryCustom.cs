#if HONEYSELECT
using System;
using System.Collections.Generic;
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
        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static readonly Dictionary<int, List<ObjectData>> objects = new Dictionary<int, List<ObjectData>>();
        public static int lastType;
        public static int lastId;
        public static RectTransform container;
        public static InputField searchBar;

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
        }

        private static void Reset()
        {
            objects.Clear();
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            if (objects.ContainsKey(lastType) == false)
                return;
            foreach (ObjectData objectData in objects[lastType])
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

        public static void Prefix(SmAccessory __instance, int newType, int newId)
        {
            __instance.SetPrivateExplicit<SmAccessory>("acsType", newType);
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharFileInfoClothes clothesInfo = (CharFileInfoClothes)__instance.GetPrivate("clothesInfo");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            if (null == chaInfo)
                return;
            if (null == __instance.objListTop)
                return;
            if (null == __instance.objLineBase)
                return;
            if (null == __instance.rtfPanel)
                return;
            int slotNoFromSubMenuSelect = (int)__instance.CallPrivateExplicit<SmAccessory>("GetSlotNoFromSubMenuSelect");
            int count = 0;
            int selectedIndex = 0;
            if (SmAccessory_Data.lastType != nowSubMenuTypeId && SmAccessory_Data.objects.ContainsKey(SmAccessory_Data.lastType))
                foreach (SmAccessory_Data.ObjectData o in SmAccessory_Data.objects[SmAccessory_Data.lastType])
                    o.obj.SetActive(false);
            if (newType != -1)
            {
                if (SmAccessory_Data.objects.ContainsKey(newType))
                {
                    foreach (SmAccessory_Data.ObjectData o in SmAccessory_Data.objects[newType])
                    {
                        o.obj.SetActive(true);
                        if (count == 0)
                            __instance.SetPrivateExplicit<SmAccessory>("firstIndex", o.key);
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
                }
                else
                {
                    SmAccessory_Data.objects.Add(newType, new List<SmAccessory_Data.ObjectData>());
                    List<SmAccessory_Data.ObjectData> objects = SmAccessory_Data.objects[newType];
                    Dictionary<int, ListTypeFbx> dictionary = null;
                    CharaListInfo.TypeAccessoryFbx type = (CharaListInfo.TypeAccessoryFbx)((int)Enum.ToObject(typeof(CharaListInfo.TypeAccessoryFbx), newType));
                    dictionary = chaInfo.ListInfo.GetAccessoryFbxList(type, true);

                    foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                    {
                        bool flag = false;
                        if (chaInfo.customInfo.isConcierge)
                        {
                            flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                        }
                        if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0 || flag)
                        {
                            if (chaInfo.Sex == 0)
                            {
                                if ("0" == current.Value.PrefabM)
                                    continue;
                            }
                            else if ("0" == current.Value.PrefabF)
                                continue;
                            if (count == 0)
                                __instance.SetPrivateExplicit<SmAccessory>("firstIndex", current.Key);
                            GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                            gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                            FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                            fbxTypeInfo.id = current.Key;
                            fbxTypeInfo.typeName = current.Value.Name;
                            fbxTypeInfo.info = current.Value;
                            gameObject.transform.SetParent(__instance.objListTop.transform, false);
                            RectTransform rectTransform = gameObject.transform as RectTransform;
                            rectTransform.localScale = new Vector3(1f, 1f, 1f);
                            rectTransform.sizeDelta = new Vector2(SmAccessory_Data.container.rect.width, 24f);
                            Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                            component.text = fbxTypeInfo.typeName;
                            __instance.CallPrivateExplicit<SmAccessory>("SetButtonClickHandler", gameObject);
                            Toggle component2 = gameObject.GetComponent<Toggle>();
                            objects.Add(new SmAccessory_Data.ObjectData { obj = gameObject, key = current.Key, toggle = component2, text = component });
                            component2.onValueChanged.AddListener(v =>
                            {
                                if (component2.isOn)
                                    UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                            });
                            if (newId == -1)
                            {
                                if (count == 0)
                                    component2.isOn = true;
                            }
                            else if (current.Key == newId)
                                component2.isOn = true;
                            if (component2.isOn)
                                selectedIndex = count;
                            ToggleGroup component3 = __instance.objListTop.GetComponent<ToggleGroup>();
                            component2.group = component3;
                            gameObject.SetActive(true);
                            if (!flag)
                            {
                                int num3 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                                Transform transform = rectTransform.FindChild("imgNew");
                                if (transform && num3 == 1)
                                    transform.gameObject.SetActive(true);
                            }
                            count++;
                        }
                    }
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
            else
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
            __instance.SetPrivateExplicit<SmAccessory>("nowChanging", true);
            if (clothesInfo != null)
            {
                float specularIntensity = clothesInfo.accessory[slotNoFromSubMenuSelect].color.specularIntensity;
                float specularSharpness = clothesInfo.accessory[slotNoFromSubMenuSelect].color.specularSharpness;
                float specularSharpness2 = clothesInfo.accessory[slotNoFromSubMenuSelect].color2.specularSharpness;
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
            __instance.SetPrivateExplicit<SmAccessory>("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);
            SmAccessory_Data.lastType = newType;
            SmAccessory_Data.lastId = newId;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
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