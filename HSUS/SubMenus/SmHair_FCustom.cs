#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CustomMenu;
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmHair_F_Data
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

        public static readonly Dictionary<int, TypeData> objects = new Dictionary<int, TypeData>();
        public static int previousType;
        public static RectTransform container;
        public static InputField searchBar;
        internal static readonly Dictionary<int, IEnumerator> _methods = new Dictionary<int, IEnumerator>();
        internal static PropertyInfo _translateProperty = null;
        private static SmHair_F _originalComponent;

        public static void Init(SmHair_F originalComponent)
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

            if (HSUS.self._asyncLoading)
            {
                CharInfo chaInfo = originalComponent.customControl.chainfo;
                for (int i = 47; i < 51; i++)
                {
                    IEnumerator method = SmHair_F_SetCharaInfoSub_Patches.SetCharaInfoSub(originalComponent, i, chaInfo);
                    _methods.Add(i, method);
                    HSUS._self._asyncMethods.Add(method);
                }
            }
            else
            {
                for (int i = 47; i < 51; i++)
                    _methods.Add(i, null);
            }
        }
        private static void Reset()
        {
            objects.Clear();
            _methods.Clear();
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            if (objects.ContainsKey(previousType) == false)
                return;
            foreach (ObjectData objectData in objects[previousType].objects)
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }
    }

    [HarmonyPatch(typeof(SmHair_F), "SetCharaInfoSub")]
    public class SmHair_F_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmHair_F __instance, CharInfo ___chaInfo, CharFileInfoCustom ___customInfo)
        {
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            SmHair_F_Data.searchBar.text = "";
            SmHair_F_Data.SearchChanged("");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            if (null != __instance.tglTab)
            {
                __instance.tglTab.isOn = true;
            }
            SmHair_F_Data.TypeData td;
            if (SmHair_F_Data.previousType != nowSubMenuTypeId && SmHair_F_Data.objects.TryGetValue(SmHair_F_Data.previousType, out td))
                td.parentObject.gameObject.SetActive(false);
            int count = 0;
            int selected = 0;

            IEnumerator method;
            if (SmHair_F_Data._methods.TryGetValue(nowSubMenuTypeId, out method) == false || method == null || method.MoveNext() == false)
                method = SetCharaInfoSub(__instance, nowSubMenuTypeId, ___chaInfo);

            while (method.MoveNext())
                ;

            td = SmHair_F_Data.objects[nowSubMenuTypeId];
            int num = 0;
            switch (nowSubMenuTypeId)
            {
                case 47:
                    num = ___customInfo.hairId[1];
                    break;
                case 48:
                    num = ___customInfo.hairId[0];
                    break;
                case 49:
                    num = ___customInfo.hairId[2];
                    break;
                case 50:
                    num = ___customInfo.hairId[0];
                    break;
            }
            count = td.objects.Count;
            td.parentObject.gameObject.SetActive(true);
            for (int i = 0; i < td.objects.Count; i++)
            {
                SmHair_F_Data.ObjectData o = td.objects[i];
                o.toggle.isOn = false;
                if (o.key == num)
                {
                    selected = i;
                    o.toggle.isOn = true;
                    o.toggle.onValueChanged.Invoke(true);
                }
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivate("nowChanging", true);
            if (___customInfo != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                float value4 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 47:
                        value = ___customInfo.hairColor[1].specularIntensity;
                        value2 = ___customInfo.hairColor[1].specularSharpness;
                        value3 = ___customInfo.hairAcsColor[1].specularIntensity;
                        value4 = ___customInfo.hairAcsColor[1].specularSharpness;
                        break;
                    case 48:
                        if (___chaInfo.Sex == 0)
                        {
                            value = ___customInfo.hairColor[0].specularIntensity;
                            value2 = ___customInfo.hairColor[0].specularSharpness;
                            value3 = ___customInfo.hairAcsColor[0].specularIntensity;
                            value4 = ___customInfo.hairAcsColor[0].specularSharpness;
                        }
                        else
                        {
                            value = ___customInfo.hairColor[0].specularIntensity;
                            value2 = ___customInfo.hairColor[0].specularSharpness;
                            value3 = ___customInfo.hairAcsColor[0].specularIntensity;
                            value4 = ___customInfo.hairAcsColor[0].specularSharpness;
                        }
                        break;
                    case 49:
                        value = ___customInfo.hairColor[2].specularIntensity;
                        value2 = ___customInfo.hairColor[2].specularSharpness;
                        value3 = ___customInfo.hairAcsColor[2].specularIntensity;
                        value4 = ___customInfo.hairAcsColor[2].specularSharpness;
                        break;
                    case 50:
                        value = ___customInfo.hairColor[0].specularIntensity;
                        value2 = ___customInfo.hairColor[0].specularSharpness;
                        value3 = ___customInfo.hairAcsColor[0].specularIntensity;
                        value4 = ___customInfo.hairAcsColor[0].specularSharpness;
                        break;
                }
                if (__instance.sldIntensity)
                    __instance.sldIntensity.value = value;
                if (__instance.inputIntensity)
                    __instance.inputIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value);
                if (__instance.sldSharpness)
                    __instance.sldSharpness.value = value2;
                if (__instance.inputSharpness)
                    __instance.inputSharpness.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value2);
                if (__instance.sldAcsIntensity)
                    __instance.sldAcsIntensity.value = value3;
                if (__instance.inputAcsIntensity)
                    __instance.inputAcsIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value3);
                if (__instance.sldAcsSharpness)
                    __instance.sldAcsSharpness.value = value4;
                if (__instance.inputAcsSharpness)
                    __instance.inputAcsSharpness.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value4);
            }
            __instance.SetPrivate("nowChanging", false);
            __instance.OnClickAcsColorSpecular();
            __instance.OnClickAcsColorDiffuse();
            __instance.OnClickColorSpecular();
            __instance.OnClickColorDiffuse();
            SmHair_F_Data.previousType = nowSubMenuTypeId;
            SmClothes_F_Data._methods[nowSubMenuTypeId] = null;
            return false;
        }

        internal static IEnumerator SetCharaInfoSub(SmHair_F __instance, int nowSubMenuTypeId, CharInfo chaInfo)
        {
            if (SmHair_F_Data.objects.ContainsKey(nowSubMenuTypeId) != false)
                yield break;
            Dictionary<int, ListTypeFbx> dictionary = null;
            switch (nowSubMenuTypeId)
            {
                case 47:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairF, true);
                    if (__instance.btnIntegrate01)
                    {
                        Text[] componentsInChildren = __instance.btnIntegrate01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren.Length != 0)
                        {
                            componentsInChildren[0].text = "後ろ髪と横髪も同じ色に合わせる";
                        }
                    }
                    if (__instance.btnIntegrate02)
                    {
                        __instance.btnIntegrate02.gameObject.SetActive(true);
                    }
                    if (__instance.btnReference01)
                    {
                        __instance.btnReference01.gameObject.SetActive(true);
                        Text[] componentsInChildren2 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren2.Length != 0)
                        {
                            componentsInChildren2[0].text = "眉毛の色に合わせる";
                        }
                    }
                    if (__instance.btnReference02)
                    {
                        __instance.btnReference02.gameObject.SetActive(true);
                        Text[] componentsInChildren3 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren3.Length != 0)
                        {
                            componentsInChildren3[0].text = "アンダーヘアの色に合わせる";
                        }
                    }
                    break;
                case 48:
                    if (chaInfo.Sex == 0)
                    {
                        dictionary = chaInfo.ListInfo.GetMaleFbxList(CharaListInfo.TypeMaleFbx.cm_f_hair, true);
                        if (__instance.btnIntegrate01)
                        {
                            Text[] componentsInChildren4 = __instance.btnIntegrate01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren4.Length != 0)
                            {
                                componentsInChildren4[0].text = "眉毛とヒゲも同じ色に合わせる";
                            }
                        }
                        if (__instance.btnIntegrate02)
                        {
                            __instance.btnIntegrate02.gameObject.SetActive(false);
                        }
                        if (__instance.btnReference01)
                        {
                            __instance.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren5 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren5.Length != 0)
                            {
                                componentsInChildren5[0].text = "眉毛の色に合わせる";
                            }
                        }
                        if (__instance.btnReference02)
                        {
                            __instance.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren6 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren6.Length != 0)
                            {
                                componentsInChildren6[0].text = "ヒゲの色に合わせる";
                            }
                        }
                    }
                    else
                    {
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairB, true);
                        if (__instance.btnIntegrate01)
                        {
                            Text[] componentsInChildren7 = __instance.btnIntegrate01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren7.Length != 0)
                            {
                                componentsInChildren7[0].text = "前髪と横髪も同じ色に合わせる";
                            }
                        }
                        if (__instance.btnIntegrate02)
                        {
                            __instance.btnIntegrate02.gameObject.SetActive(true);
                        }
                        if (__instance.btnReference01)
                        {
                            __instance.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren8 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren8.Length != 0)
                            {
                                componentsInChildren8[0].text = "眉毛の色に合わせる";
                            }
                        }
                        if (__instance.btnReference02)
                        {
                            __instance.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren9 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren9.Length != 0)
                            {
                                componentsInChildren9[0].text = "アンダーヘアの色に合わせる";
                            }
                        }
                    }
                    break;
                case 49:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairS, true);
                    if (__instance.btnIntegrate01)
                    {
                        Text[] componentsInChildren10 = __instance.btnIntegrate01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren10.Length != 0)
                        {
                            componentsInChildren10[0].text = "前髪と後ろ髪も同じ色に合わせる";
                        }
                    }
                    if (__instance.btnIntegrate02)
                    {
                        __instance.btnIntegrate02.gameObject.SetActive(true);
                    }
                    if (__instance.btnReference01)
                    {
                        __instance.btnReference01.gameObject.SetActive(true);
                        Text[] componentsInChildren11 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren11.Length != 0)
                        {
                            componentsInChildren11[0].text = "眉毛の色に合わせる";
                        }
                    }
                    if (__instance.btnReference02)
                    {
                        __instance.btnReference02.gameObject.SetActive(true);
                        Text[] componentsInChildren12 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren12.Length != 0)
                        {
                            componentsInChildren12[0].text = "アンダーヘアの色に合わせる";
                        }
                    }
                    break;
                case 50:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairB, true);
                    if (__instance.btnIntegrate01)
                    {
                        Text[] componentsInChildren13 = __instance.btnIntegrate01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren13.Length != 0)
                        {
                            componentsInChildren13[0].text = "眉毛とアンダーヘアも同じ色に合わせる";
                        }
                    }
                    if (__instance.btnIntegrate02)
                    {
                        __instance.btnIntegrate02.gameObject.SetActive(false);
                    }
                    if (__instance.btnReference01)
                    {
                        __instance.btnReference01.gameObject.SetActive(true);
                        Text[] componentsInChildren14 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren14.Length != 0)
                        {
                            componentsInChildren14[0].text = "眉毛の色に合わせる";
                        }
                    }
                    if (__instance.btnReference02)
                    {
                        __instance.btnReference02.gameObject.SetActive(true);
                        Text[] componentsInChildren15 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                        if (componentsInChildren15.Length != 0)
                        {
                            componentsInChildren15[0].text = "アンダーヘアの色に合わせる";
                        }
                    }
                    break;
            }
            if (__instance.btnIntegrate02)
            {
                Text[] componentsInChildren16 = __instance.btnIntegrate02.GetComponentsInChildren<Text>(true);
                if (componentsInChildren16.Length != 0)
                {
                    componentsInChildren16[0].text = "眉毛とアンダーヘアも同じ色に合わせる";
                }
            }


            SmHair_F_Data.TypeData td = new SmHair_F_Data.TypeData();
            td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
            td.parentObject.SetParent(__instance.objListTop.transform, false);
            td.parentObject.localScale = Vector3.one;
            td.parentObject.localPosition = Vector3.zero;
            td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SmHair_F_Data.objects.Add(nowSubMenuTypeId, td);
            ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
            group.allowSwitchOff = true;
            td.parentObject.gameObject.SetActive(false);

            foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
            {
                if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0)
                    continue;
                if (chaInfo.Sex != 0)
                {
                    if (nowSubMenuTypeId == 48)
                    {
                        if ("1" == current.Value.Etc[0])
                        {
                            continue;
                        }
                    }
                    else if (nowSubMenuTypeId == 50 && "1" != current.Value.Etc[0])
                    {
                        continue;
                    }
                }
                GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                fbxTypeInfo.id = current.Key;
                fbxTypeInfo.typeName = current.Value.Name;
                fbxTypeInfo.info = current.Value;
                gameObject.transform.SetParent(td.parentObject, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = Vector3.one;
                rectTransform.sizeDelta = new Vector2(SmHair_F_Data.container.rect.width, 24f);
                Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                if (HSUS._self._asyncLoading && SmHair_F_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmHair_F_Data._translateProperty.SetValue(component, false, null);
                    string t = fbxTypeInfo.typeName;
                    HSUS._self._translationMethod(ref t);
                    component.text = t;
                }
                else
                    component.text = fbxTypeInfo.typeName;
                __instance.CallPrivate("SetButtonClickHandler", gameObject);
                Toggle component2 = gameObject.GetComponent<Toggle>();
                td.objects.Add(new SmHair_F_Data.ObjectData { key = current.Key, obj = gameObject, toggle = component2, text = component });
                component2.onValueChanged.AddListener(v =>
                {
                    if (component2.isOn)
                        UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                });
                component2.@group = @group;
                gameObject.SetActive(true);
                int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                Transform transform = rectTransform.FindChild("imgNew");
                if (transform && num4 == 1)
                {
                    transform.gameObject.SetActive(true);
                }
                yield return null;
            }
        }
    }
}

#endif