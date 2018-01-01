using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public static class SmHair_F_Data
    {
        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static readonly Dictionary<int, List<ObjectData>> _objects = new Dictionary<int, List<ObjectData>>();
        public static int _previousType;
        public static RectTransform _container;
        public static InputField _searchBar;

        private static SmHair_F _originalComponent;

        public static void Init(SmHair_F originalComponent)
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
            rt = _searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            _searchBar.placeholder.GetComponent<Text>().text = "Search...";
            _searchBar.onValueChanged.AddListener(SearchChanged);
            foreach (Text t in _searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;

        }

        public static void SearchChanged(string arg0)
        {
            string search = _searchBar.text.Trim();
            if (_objects.ContainsKey(_previousType) == false)
                return;
            foreach (ObjectData objectData in _objects[_previousType])
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

        public static void Prefix(SmHair_F __instance)
        {
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            CharFileInfoCustom customInfo = (CharFileInfoCustom) __instance.GetPrivate("customInfo");
            SmHair_F_Data._searchBar.text = "";
            SmHair_F_Data.SearchChanged("");
            if (null == chaInfo)
            {
                return;
            }
            if (null == __instance.objListTop)
            {
                return;
            }
            if (null == __instance.objLineBase)
            {
                return;
            }
            if (null == __instance.rtfPanel)
            {
                return;
            }
            if (null != __instance.tglTab)
            {
                __instance.tglTab.isOn = true;
            }

            if (SmHair_F_Data._previousType != nowSubMenuTypeId && SmHair_F_Data._objects.ContainsKey(SmHair_F_Data._previousType))
                foreach (SmHair_F_Data.ObjectData o in SmHair_F_Data._objects[SmHair_F_Data._previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (SmHair_F_Data._objects.ContainsKey(nowSubMenuTypeId))
            {
                int num = 0;
                switch (nowSubMenuTypeId)
                {
                    case 47:
                        num = customInfo.hairId[1];
                        break;
                    case 48:
                        num = customInfo.hairId[0];
                        break;
                    case 49:
                        num = customInfo.hairId[2];
                        break;
                    case 50:
                        num = customInfo.hairId[0];
                        break;
                }
                count = SmHair_F_Data._objects[nowSubMenuTypeId].Count;
                for (int i = 0; i < SmHair_F_Data._objects[nowSubMenuTypeId].Count; i++)
                {
                    SmHair_F_Data.ObjectData o = SmHair_F_Data._objects[nowSubMenuTypeId][i];
                    o.obj.SetActive(true);
                    if (o.key == num)
                    {
                        selected = i;
                        o.toggle.isOn = true;
                        o.toggle.onValueChanged.Invoke(true);
                    }
                    else if (o.toggle.isOn)
                        o.toggle.isOn = false;
                }
            }
            else
            {
                Dictionary<int, ListTypeFbx> dictionary = null;
                int num = 0;
                switch (nowSubMenuTypeId)
                {
                    case 47:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairF, true);
                        num = customInfo.hairId[1];
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
                            num = customInfo.hairId[0];
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
                            num = customInfo.hairId[0];
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
                        num = customInfo.hairId[2];
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
                        num = customInfo.hairId[0];
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
                List<SmHair_F_Data.ObjectData> cd = new List<SmHair_F_Data.ObjectData>();
                SmHair_F_Data._objects.Add(nowSubMenuTypeId, cd);
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
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
                        gameObject.transform.SetParent(__instance.objListTop.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(SmHair_F_Data._container.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;
                        __instance.CallPrivateExplicit<SmHair_F>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new SmHair_F_Data.ObjectData {key = current.Key, obj = gameObject, toggle = component2, text = component });
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                        });
                        if (current.Key == num)
                        {
                            component2.isOn = true;
                            selected = count;
                        }
                        ToggleGroup component3 = __instance.objListTop.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform = rectTransform.FindChild("imgNew");
                        if (transform && num4 == 1)
                        {
                            transform.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivateExplicit<SmHair_F>("nowChanging", true);
            if (customInfo != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                float value4 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 47:
                        value = customInfo.hairColor[1].specularIntensity;
                        value2 = customInfo.hairColor[1].specularSharpness;
                        value3 = customInfo.hairAcsColor[1].specularIntensity;
                        value4 = customInfo.hairAcsColor[1].specularSharpness;
                        break;
                    case 48:
                        if (chaInfo.Sex == 0)
                        {
                            value = customInfo.hairColor[0].specularIntensity;
                            value2 = customInfo.hairColor[0].specularSharpness;
                            value3 = customInfo.hairAcsColor[0].specularIntensity;
                            value4 = customInfo.hairAcsColor[0].specularSharpness;
                        }
                        else
                        {
                            value = customInfo.hairColor[0].specularIntensity;
                            value2 = customInfo.hairColor[0].specularSharpness;
                            value3 = customInfo.hairAcsColor[0].specularIntensity;
                            value4 = customInfo.hairAcsColor[0].specularSharpness;
                        }
                        break;
                    case 49:
                        value = customInfo.hairColor[2].specularIntensity;
                        value2 = customInfo.hairColor[2].specularSharpness;
                        value3 = customInfo.hairAcsColor[2].specularIntensity;
                        value4 = customInfo.hairAcsColor[2].specularSharpness;
                        break;
                    case 50:
                        value = customInfo.hairColor[0].specularIntensity;
                        value2 = customInfo.hairColor[0].specularSharpness;
                        value3 = customInfo.hairAcsColor[0].specularIntensity;
                        value4 = customInfo.hairAcsColor[0].specularSharpness;
                        break;
                }
                if (__instance.sldIntensity)
                {
                    __instance.sldIntensity.value = value;
                }
                if (__instance.inputIntensity)
                {
                    __instance.inputIntensity.text = __instance.ChangeTextFromFloat(value);
                }
                if (__instance.sldSharpness)
                {
                    __instance.sldSharpness.value = value2;
                }
                if (__instance.inputSharpness)
                {
                    __instance.inputSharpness.text = __instance.ChangeTextFromFloat(value2);
                }
                if (__instance.sldAcsIntensity)
                {
                    __instance.sldAcsIntensity.value = value3;
                }
                if (__instance.inputAcsIntensity)
                {
                    __instance.inputAcsIntensity.text = __instance.ChangeTextFromFloat(value3);
                }
                if (__instance.sldAcsSharpness)
                {
                    __instance.sldAcsSharpness.value = value4;
                }
                if (__instance.inputAcsSharpness)
                {
                    __instance.inputAcsSharpness.text = __instance.ChangeTextFromFloat(value4);
                }
            }
            __instance.SetPrivateExplicit<SmHair_F>("nowChanging", false);
            __instance.OnClickAcsColorSpecular();
            __instance.OnClickAcsColorDiffuse();
            __instance.OnClickColorSpecular();
            __instance.OnClickColorDiffuse();
            SmHair_F_Data._previousType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
