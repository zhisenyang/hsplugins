using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
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

        public static readonly Dictionary<int, List<ObjectData>> _objects = new Dictionary<int, List<ObjectData>>();
        public static int _lastType;
        public static int _lastId;
        public static RectTransform _container;
        public static InputField _searchBar;

        private static SmAccessory _originalComponent;

        public static void Init(SmAccessory originalComponent)
        {
            _originalComponent = originalComponent;
            _container = _originalComponent.transform.FindDescendant("ListTop").transform as RectTransform;
            VerticalLayoutGroup group = _container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = _container.gameObject.AddComponent<ContentSizeFitter>();
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

            _searchBar = UIUtility.CreateInputField("Search Bar", _originalComponent.transform.FindChild("TabControl/TabItem01"));
            foreach (Text t in _searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;

            rt = _searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            _searchBar.placeholder.GetComponent<Text>().text = "Search...";
            _searchBar.onValueChanged.AddListener(SearchChanged);
        }

        public static void SearchChanged(string arg0)
        {
            string search = _searchBar.text.Trim();
            if (_objects.ContainsKey(_lastType) == false)
                return;
            foreach (ObjectData objectData in _objects[_lastType])
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
            if (SmAccessory_Data._lastType != nowSubMenuTypeId && SmAccessory_Data._objects.ContainsKey(SmAccessory_Data._lastType))
                foreach (SmAccessory_Data.ObjectData o in SmAccessory_Data._objects[SmAccessory_Data._lastType])
                    o.obj.SetActive(false);
            if (newType != -1)
            {
                if (SmAccessory_Data._objects.ContainsKey(newType))
                {
                    foreach (SmAccessory_Data.ObjectData o in SmAccessory_Data._objects[newType])
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
                    SmAccessory_Data._objects.Add(newType, new List<SmAccessory_Data.ObjectData>());
                    List<SmAccessory_Data.ObjectData> objects = SmAccessory_Data._objects[newType];
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
                            rectTransform.sizeDelta = new Vector2(SmAccessory_Data._container.rect.width, 24f);
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
                    __instance.inputIntensity.text = __instance.ChangeTextFromFloat(specularIntensity);
                if (__instance.sldSharpness[0])
                    __instance.sldSharpness[0].value = specularSharpness;
                if (__instance.inputSharpness[0])
                    __instance.inputSharpness[0].text = __instance.ChangeTextFromFloat(specularSharpness);
                if (__instance.sldSharpness[1])
                    __instance.sldSharpness[1].value = specularSharpness2;
                if (__instance.inputSharpness[1])
                    __instance.inputSharpness[1].text = __instance.ChangeTextFromFloat(specularSharpness2);
            }
            __instance.SetPrivateExplicit<SmAccessory>("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);
            SmAccessory_Data._lastType = newType;
            SmAccessory_Data._lastId = newId;
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
        public static void Prefix(int smTypeId, bool sameSubMenu)
        {
            if (SmAccessory_Data._searchBar != null)
            {
                SmAccessory_Data._searchBar.text = "";
                SmAccessory_Data.SearchChanged("");
            }
        }
    }

    [HarmonyPatch(typeof(SmAccessory))]
    [HarmonyPatch("OnChangeAccessoryType")]
    [HarmonyPatch(new[] { typeof(int) })]
    public class SmAccessory_OnChangeAccessoryType_Patches
    {
        public static void Prefix(int newType)
        {
            if (SmAccessory_Data._searchBar != null)
            {
                SmAccessory_Data._searchBar.text = "";
                SmAccessory_Data.SearchChanged("");
            }
        }
    }
}
