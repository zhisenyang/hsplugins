#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CustomMenu;
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmSwimsuit_Data
    {
        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static readonly List<ObjectData> objects = new List<ObjectData>();
        public static int previousType;
        public static RectTransform container;
        public static InputField searchBar;
        internal static bool _created = false;
        internal static IEnumerator _setCharaInfoSub;
        internal static PropertyInfo _translateProperty = null;

        private static SmSwimsuit _originalComponent;

        public static void Init(SmSwimsuit originalComponent)
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
            rt = searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            searchBar.placeholder.GetComponent<Text>().text = "Search...";
            searchBar.onValueChanged.AddListener(SearchChanged);
            foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (HSUS._self._asyncLoading)
            {
                _setCharaInfoSub = SmSwimsuit_SetCharaInfoSub_Patches.SetCharaInfoSub(originalComponent, originalComponent.customControl.chainfo);
                HSUS._self._asyncMethods.Add(_setCharaInfoSub);
            }

        }
        private static void Reset()
        {
            objects.Clear();
            _created = false;
            _setCharaInfoSub = null;
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            foreach (ObjectData objectData in objects)
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }
    }

    [HarmonyPatch(typeof(SmSwimsuit), "SetCharaInfoSub")]
    public class SmSwimsuit_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmSwimsuit __instance, CharInfo ___chaInfo, CharFileInfoClothes ___clothesInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            SmSwimsuit_Data.searchBar.text = "";
            SmSwimsuit_Data.SearchChanged("");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;

            int count = 0;
            int selected = 0;

            if (SmSwimsuit_Data._setCharaInfoSub == null || SmSwimsuit_Data._setCharaInfoSub.MoveNext() == false)
                SmSwimsuit_Data._setCharaInfoSub = SetCharaInfoSub(__instance, ___chaInfo);

            while (SmSwimsuit_Data._setCharaInfoSub.MoveNext())
                ;

            SmSwimsuit_Data._setCharaInfoSub = null;
            int num2 = 0;
            if (___clothesInfo != null)
            {
                num2 = ___clothesInfo.clothesId[4];
            }
            count = SmSwimsuit_Data.objects.Count;
            for (int i = 0; i < SmSwimsuit_Data.objects.Count; i++)
            {
                SmSwimsuit_Data.ObjectData o = SmSwimsuit_Data.objects[i];
                if (o.key == num2)
                {
                    selected = i;
                    o.toggle.isOn = true;
                    o.toggle.onValueChanged.Invoke(true);
                }
                else if (o.toggle.isOn)
                    o.toggle.isOn = false;
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivate("nowChanging", true);
            if (___clothesInfo != null)
            {
                float specularIntensity = ___clothesInfo.clothesColor[4].specularIntensity;
                float specularSharpness = ___clothesInfo.clothesColor[4].specularSharpness;
                float specularSharpness2 = ___clothesInfo.clothesColor2[4].specularSharpness;
                if (__instance.sldIntensity)
                {
                    __instance.sldIntensity.value = specularIntensity;
                }
                if (__instance.inputIntensity)
                {
                    __instance.inputIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", specularIntensity);
                }
                if (__instance.sldSharpness[0])
                {
                    __instance.sldSharpness[0].value = specularSharpness;
                }
                if (__instance.inputSharpness[0])
                {
                    __instance.inputSharpness[0].text = (string)__instance.CallPrivate("ChangeTextFromFloat", specularSharpness);
                }
                if (__instance.sldSharpness[1])
                {
                    __instance.sldSharpness[1].value = specularSharpness2;
                }
                if (__instance.inputSharpness[1])
                {
                    __instance.inputSharpness[1].text = (string)__instance.CallPrivate("ChangeTextFromFloat", specularSharpness2);
                }
            }
            __instance.SetPrivate("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);
            if (__instance.tglOpt01)
            {
                __instance.tglOpt01.isOn = !___clothesInfoF.hideSwimOptTop;
            }
            if (__instance.tglOpt02)
            {
                __instance.tglOpt02.isOn = !___clothesInfoF.hideSwimOptBot;
            }
            return false;
        }

        internal static IEnumerator SetCharaInfoSub(SmSwimsuit __instance, CharInfo chaInfo)
        {
            if (SmSwimsuit_Data._created)
                yield break;
            Dictionary<int, ListTypeFbx> dictionary = null;
            dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swim, true);
            ToggleGroup component3 = __instance.objListTop.GetComponent<ToggleGroup>();

            foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
            {
                if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0)
                    continue;
                GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                fbxTypeInfo.id = current.Key;
                fbxTypeInfo.typeName = current.Value.Name;
                fbxTypeInfo.info = current.Value;
                gameObject.transform.SetParent(__instance.objListTop.transform, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = new Vector3(1f, 1f, 1f);
                rectTransform.sizeDelta = new Vector2(SmSwimsuit_Data.container.rect.width, 24f);
                Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                if (HSUS._self._asyncLoading && SmSwimsuit_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmSwimsuit_Data._translateProperty.SetValue(component, false, null);
                    string t = fbxTypeInfo.typeName;
                    HSUS._self._translationMethod(ref t);
                    component.text = t;
                }
                else
                    component.text = fbxTypeInfo.typeName;
                __instance.CallPrivate("SetButtonClickHandler", gameObject);
                Toggle component2 = gameObject.GetComponent<Toggle>();
                SmSwimsuit_Data.objects.Add(new SmSwimsuit_Data.ObjectData() { key = current.Key, obj = gameObject, toggle = component2, text = component });
                component2.onValueChanged.AddListener(v =>
                {
                    if (component2.isOn)
                        UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                });
                component2.group = component3;
                gameObject.SetActive(true);
                int num5 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                Transform transform = rectTransform.FindChild("imgNew");
                if (transform && num5 == 1)
                {
                    transform.gameObject.SetActive(true);
                }
                yield return null;
            }

            SmSwimsuit_Data._created = true;
        }
    }

}

#endif