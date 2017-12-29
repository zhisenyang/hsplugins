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
    public static class SmKindColorD_Data
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
        public static InputField _searchBar;
        public static RectTransform _container;

        private static SmKindColorD _originalComponent;

        public static void Init(SmKindColorD originalComponent)
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

    [HarmonyPatch(typeof(SmKindColorD), "SetCharaInfoSub")]
    public class SmKindColorD_SetCharaInfoSub_Patches
    {
        public static void Prefix(SmKindColorD __instance)
        {
            SmKindColorD_Data._searchBar.text = "";
            SmKindColorD_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            CharFileInfoCustom customInfo = (CharFileInfoCustom)__instance.GetPrivate("customInfo");
            CharFileInfoCustomFemale customInfoF = (CharFileInfoCustomFemale)__instance.GetPrivate("customInfoF");
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

            if (SmKindColorD_Data._previousType != nowSubMenuTypeId && SmKindColorD_Data._objects.ContainsKey(SmKindColorD_Data._previousType))
                foreach (SmKindColorD_Data.ObjectData o in SmKindColorD_Data._objects[SmKindColorD_Data._previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (SmKindColorD_Data._objects.ContainsKey(nowSubMenuTypeId))
            {
                int num = 0;
                if (customInfo != null)
                    switch (nowSubMenuTypeId)
                    {
                        case 39:
                            num = customInfoF.texEyeshadowId;
                            break;
                        case 40:
                            num = customInfoF.texCheekId;
                            break;
                        case 41:
                            num = customInfoF.texLipId;
                            break;
                        case 42:
                            num = customInfo.texTattoo_fId;
                            break;
                        case 43:
                            num = customInfoF.texMoleId;
                            break;
                        case 9:
                            num = customInfo.texTattoo_bId;
                            break;
                        case 8:
                            num = customInfoF.texSunburnId;
                            break;
                    }
                count = SmKindColorD_Data._objects[nowSubMenuTypeId].Count;
                for (int i = 0; i < SmKindColorD_Data._objects[nowSubMenuTypeId].Count; i++)
                {
                    SmKindColorD_Data.ObjectData o = SmKindColorD_Data._objects[nowSubMenuTypeId][i];
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
                Dictionary<int, ListTypeTexture> dictionary = null;
                int num = 0;
                switch (nowSubMenuTypeId)
                {
                    case 39:
                        dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_eyeshadow, true);
                        if (customInfo != null)
                        {
                            num = customInfoF.texEyeshadowId;
                        }
                        break;
                    case 40:
                        dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_cheek, true);
                        if (customInfo != null)
                        {
                            num = customInfoF.texCheekId;
                        }
                        break;
                    case 41:
                        dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_lip, true);
                        if (customInfo != null)
                        {
                            num = customInfoF.texLipId;
                        }
                        break;
                    case 42:
                        dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_tattoo_f, true) : chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_tattoo_f, true);
                        if (customInfo != null)
                        {
                            num = customInfo.texTattoo_fId;
                        }
                        break;
                    case 43:
                        dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_mole, true);
                        if (customInfo != null)
                        {
                            num = customInfoF.texMoleId;
                        }
                        break;
                    default:
                        if (nowSubMenuTypeId != 8)
                        {
                            if (nowSubMenuTypeId == 9)
                            {
                                dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_tattoo_b, true) : chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_tattoo_b, true);
                                if (customInfo != null)
                                {
                                    num = customInfo.texTattoo_bId;
                                }
                            }
                        }
                        else
                        {
                            dictionary = chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_sunburn, true);
                            if (customInfo != null)
                            {
                                num = customInfoF.texSunburnId;
                            }
                        }
                        break;
                }
                List<SmKindColorD_Data.ObjectData> cd = new List<SmKindColorD_Data.ObjectData>();
                SmKindColorD_Data._objects.Add(nowSubMenuTypeId, cd);

                foreach (KeyValuePair<int, ListTypeTexture> current in dictionary)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        TexTypeInfo texTypeInfo = gameObject.AddComponent<TexTypeInfo>();
                        texTypeInfo.id = current.Key;
                        texTypeInfo.typeName = current.Value.Name;
                        texTypeInfo.info = current.Value;
                        gameObject.transform.SetParent(__instance.objListTop.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(SmKindColorD_Data._container.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = texTypeInfo.typeName;
                        __instance.CallPrivateExplicit<SmKindColorD>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new SmKindColorD_Data.ObjectData {key = current.Key, obj = gameObject, toggle = component2, text = component});
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(texTypeInfo.info.Id + " " + texTypeInfo.info.ABPath);
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

            __instance.OnClickColorDiffuse();
            SmKindColorD_Data._previousType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
