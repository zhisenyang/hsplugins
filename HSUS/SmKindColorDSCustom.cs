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
    public static class SmKindColorDS_Data
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

        private static SmKindColorDS _originalComponent;
        public static void Init(SmKindColorDS originalComponent)
        {
            _originalComponent = originalComponent;

            SmKindColorDS_Data._container = _originalComponent.transform.FindDescendant("ListTop").transform as RectTransform;
            VerticalLayoutGroup group = SmKindColorDS_Data._container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = SmKindColorDS_Data._container.gameObject.AddComponent<ContentSizeFitter>();
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

            SmKindColorDS_Data._searchBar = UIUtility.CreateInputField("Search Bar", _originalComponent.transform.FindChild("TabControl/TabItem01"));
            rt = SmKindColorDS_Data._searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            SmKindColorDS_Data._searchBar.placeholder.GetComponent<Text>().text = "Search...";
            SmKindColorDS_Data._searchBar.onValueChanged.AddListener(SearchChanged);
            foreach (Text t in SmKindColorDS_Data._searchBar.GetComponentsInChildren<Text>())
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

    [HarmonyPatch(typeof(SmKindColorDS), "SetCharaInfoSub")]
    public class SmKindColorDS_SetCharaInfoSub_Patches
    {
        public static void Prefix(SmKindColorDS __instance)
        {
            SmKindColorDS_Data._searchBar.text = "";
            SmKindColorDS_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            CharFileInfoCustom customInfo = (CharFileInfoCustom)__instance.GetPrivate("customInfo");
            CharFileInfoCustomFemale customInfoF = (CharFileInfoCustomFemale)__instance.GetPrivate("customInfoF");
            CharFileInfoCustomMale customInfoM = (CharFileInfoCustomMale)__instance.GetPrivate("customInfoM");
            if (null == chaInfo)
                return;
            if (null == __instance.objListTop)
                return;
            if (null == __instance.objLineBase)
                return;
            if (null == __instance.rtfPanel)
                return;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;

            if (SmKindColorDS_Data._previousType != nowSubMenuTypeId && SmKindColorDS_Data._objects.ContainsKey(SmKindColorDS_Data._previousType))
                foreach (SmKindColorDS_Data.ObjectData o in SmKindColorDS_Data._objects[SmKindColorDS_Data._previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (SmKindColorDS_Data._objects.ContainsKey(nowSubMenuTypeId))
            {
                int num = 0;
                switch (nowSubMenuTypeId)
                {
                    case 33:
                        if (customInfo != null)
                            num = customInfo.matEyebrowId;
                        break;
                    case 34:
                        if (customInfoF != null)
                            num = customInfoF.matEyelashesId;
                        break;
                    case 35:
                    case 39:
                    case 40:
                    case 41:
                    case 42:
                    case 43:
                        if (nowSubMenuTypeId != 11)
                            break;
                        if (customInfo != null)
                            num = customInfoF.matUnderhairId;
                        break;
                    case 36:
                        if (customInfo != null)
                            num = customInfo.matEyeLId;
                        break;
                    case 37:
                        if (customInfo != null)
                            num = customInfo.matEyeRId;
                        break;

                    case 38:
                        if (customInfoF != null)
                            num = customInfoF.matEyeHiId;
                        break;
                    case 44:
                        if (customInfo != null)
                            num = customInfoM.matBeardId;
                        break;
                    default:
                        goto case 43;
                }
                count = SmKindColorDS_Data._objects[nowSubMenuTypeId].Count;
                for (int i = 0; i < SmKindColorDS_Data._objects[nowSubMenuTypeId].Count; i++)
                {
                    SmKindColorDS_Data.ObjectData o = SmKindColorDS_Data._objects[nowSubMenuTypeId][i];
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
                int num = 0;
                Dictionary<int, ListTypeMaterial> dictionary = null;
                switch (nowSubMenuTypeId)
                {
                    case 33:
                        dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_eyebrow, true) : chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyebrow, true);
                        if (customInfo != null)
                            num = customInfo.matEyebrowId;
                        if (__instance.btnIntegrate)
                        {
                            __instance.btnIntegrate.gameObject.SetActive(true);
                            Text[] componentsInChildren = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren.Length != 0)
                                componentsInChildren[0].text = chaInfo.Sex == 0 ? "髪の毛とヒゲも同じ色に合わせる" : "髪の毛とアンダーヘアも同じ色に合わせる";
                        }
                        if (__instance.btnReference01)
                        {
                            __instance.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren2 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren2.Length != 0)
                                componentsInChildren2[0].text = "髪の毛の色に合わせる";
                        }
                        if (__instance.btnReference02)
                        {
                            __instance.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren3 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren3.Length != 0)
                                componentsInChildren3[0].text = chaInfo.Sex == 0 ? "ヒゲの色に合わせる" : "アンダーヘアの色に合わせる";
                        }
                        break;

                    case 34:
                        dictionary = chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyelashes, true);
                        if (customInfoF != null)
                            num = customInfoF.matEyelashesId;
                        if (__instance.btnIntegrate)
                            __instance.btnIntegrate.gameObject.SetActive(false);
                        if (__instance.btnReference01)
                            __instance.btnReference01.gameObject.SetActive(false);
                        if (__instance.btnReference02)
                            __instance.btnReference02.gameObject.SetActive(false);
                        break;

                    case 35:
                    case 39:
                    case 40:
                    case 41:
                    case 42:
                    case 43:
                        if (nowSubMenuTypeId != 11)
                            break;
                        dictionary = chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_underhair, true);
                        if (customInfo != null)
                            num = customInfoF.matUnderhairId;
                        if (__instance.btnIntegrate)
                        {
                            __instance.btnIntegrate.gameObject.SetActive(true);
                            Text[] componentsInChildren4 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren4.Length != 0)
                                componentsInChildren4[0].text = "髪の毛と眉毛も同じ色に合わせる";
                        }
                        if (__instance.btnReference01)
                        {
                            __instance.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren5 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren5.Length != 0)
                                componentsInChildren5[0].text = "髪の毛の色に合わせる";
                        }
                        if (__instance.btnReference02)
                        {
                            __instance.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren6 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren6.Length != 0)
                                componentsInChildren6[0].text = "眉毛の色に合わせる";
                        }
                        break;

                    case 36:
                        dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_eyeball, true) : chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyeball, true);
                        if (customInfo != null)
                            num = customInfo.matEyeLId;
                        if (__instance.btnIntegrate)
                        {
                            __instance.btnIntegrate.gameObject.SetActive(true);
                            Text[] componentsInChildren7 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren7.Length != 0)
                                componentsInChildren7[0].text = "右目も同じ色に合わせる";
                        }
                        if (__instance.btnReference01)
                            __instance.btnReference01.gameObject.SetActive(false);
                        if (__instance.btnReference02)
                            __instance.btnReference02.gameObject.SetActive(false);
                        break;

                    case 37:
                        dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_eyeball, true) : chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyeball, true);
                        if (customInfo != null)
                        {
                            num = customInfo.matEyeRId;
                        }
                        if (__instance.btnIntegrate)
                        {
                            __instance.btnIntegrate.gameObject.SetActive(true);
                            Text[] componentsInChildren8 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren8.Length != 0)
                                componentsInChildren8[0].text = "左目も同じ色に合わせる";
                        }
                        if (__instance.btnReference01)
                            __instance.btnReference01.gameObject.SetActive(false);
                        if (__instance.btnReference02)
                            __instance.btnReference02.gameObject.SetActive(false);
                        break;

                    case 38:
                        dictionary = chaInfo.ListInfo.GetFemaleMaterialList(CharaListInfo.TypeFemaleMaterial.cf_m_eyehi, true);
                        if (customInfoF != null)
                            num = customInfoF.matEyeHiId;
                        if (__instance.btnIntegrate)
                            __instance.btnIntegrate.gameObject.SetActive(false);
                        if (__instance.btnReference01)
                            __instance.btnReference01.gameObject.SetActive(false);
                        if (__instance.btnReference02)
                            __instance.btnReference02.gameObject.SetActive(false);
                        break;
                    case 44:
                        dictionary = chaInfo.ListInfo.GetMaleMaterialList(CharaListInfo.TypeMaleMaterial.cm_m_beard, true);
                        if (customInfo != null)
                            num = customInfoM.matBeardId;
                        if (__instance.btnIntegrate)
                        {
                            __instance.btnIntegrate.gameObject.SetActive(true);
                            Text[] componentsInChildren9 = __instance.btnIntegrate.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren9.Length != 0)
                                componentsInChildren9[0].text = "髪の毛と眉毛も同じ色に合わせる";
                        }
                        if (__instance.btnReference01)
                        {
                            __instance.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren10 = __instance.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren10.Length != 0)
                                componentsInChildren10[0].text = "髪の毛の色に合わせる";
                        }
                        if (__instance.btnReference02)
                        {
                            __instance.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren11 = __instance.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren11.Length != 0)
                                componentsInChildren11[0].text = "眉毛の色に合わせる";
                        }
                        break;
                    default:
                        goto case 43;
                }

                if (dictionary != null)
                {
                    List<SmKindColorDS_Data.ObjectData> cd = new List<SmKindColorDS_Data.ObjectData>();
                    SmKindColorDS_Data._objects.Add(nowSubMenuTypeId, cd);
                    foreach (KeyValuePair<int, ListTypeMaterial> current in dictionary)
                    {
                        if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                        {
                            GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                            gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                            MatTypeInfo matTypeInfo = gameObject.AddComponent<MatTypeInfo>();
                            matTypeInfo.id = current.Key;
                            matTypeInfo.typeName = current.Value.Name;
                            matTypeInfo.info = current.Value;
                            gameObject.transform.SetParent(__instance.objListTop.transform, false);
                            RectTransform rectTransform = gameObject.transform as RectTransform;
                            rectTransform.localScale = new Vector3(1f, 1f, 1f);
//                            rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
                            rectTransform.sizeDelta = new Vector2(SmKindColorDS_Data._container.rect.width, 24f);
                            Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                            component.text = matTypeInfo.typeName;
                            __instance.CallPrivateExplicit<SmKindColorDS>("SetButtonClickHandler", gameObject);
                            Toggle component2 = gameObject.GetComponent<Toggle>();
                            cd.Add(new SmKindColorDS_Data.ObjectData {key = current.Key, obj = gameObject, text = component, toggle = component2});
                            component2.onValueChanged.AddListener(v =>
                            {
                                if (component2.isOn)
                                    UnityEngine.Debug.Log(matTypeInfo.info.Id + " " + matTypeInfo.info.ABPath);
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
                                transform.gameObject.SetActive(true);
                            count++;
                        }
                    }
                }
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivateExplicit<SmKindColorDS>("nowChanging", true);
            if (customInfo != null)
            {
                float value = 1f;
                float value2 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 33:
                        value = customInfo.eyebrowColor.specularIntensity;
                        value2 = customInfo.eyebrowColor.specularSharpness;
                        break;
                    case 34:
                        value = customInfoF.eyelashesColor.specularIntensity;
                        value2 = customInfoF.eyelashesColor.specularSharpness;
                        break;
                    case 35:
                    case 39:
                    case 40:
                    case 41:
                    case 42:
                    case 43:
                        if (nowSubMenuTypeId != 11)
                            break;
                        value = customInfoF.underhairColor.specularIntensity;
                        value2 = customInfoF.underhairColor.specularSharpness;
                        break;
                    case 36:
                        value = customInfo.eyeLColor.specularIntensity;
                        value2 = customInfo.eyeLColor.specularSharpness;
                        break;
                    case 37:
                        value = customInfo.eyeRColor.specularIntensity;
                        value2 = customInfo.eyeRColor.specularSharpness;
                        break;
                    case 38:
                        value = customInfoF.eyeHiColor.specularIntensity;
                        value2 = customInfoF.eyeHiColor.specularSharpness;
                        break;
                    case 44:
                        value = customInfoM.beardColor.specularIntensity;
                        value2 = customInfoM.beardColor.specularSharpness;
                        break;
                    default:
                        goto case 43;
                }
                if (__instance.sldIntensity)
                    __instance.sldIntensity.value = value;
                if (__instance.inputIntensity)
                    __instance.inputIntensity.text = __instance.ChangeTextFromFloat(value);
                if (__instance.sldSharpness)
                    __instance.sldSharpness.value = value2;
                if (__instance.inputSharpness)
                    __instance.inputSharpness.text = __instance.ChangeTextFromFloat(value2);
            }
            __instance.SetPrivateExplicit<SmKindColorDS>("nowChanging", false);
            __instance.OnClickColorSpecular();
            __instance.OnClickColorDiffuse();
            SmKindColorDS_Data._previousType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
