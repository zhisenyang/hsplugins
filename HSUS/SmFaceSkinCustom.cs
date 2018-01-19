using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using CustomMenu;
using Harmony;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmFaceSkin_Data
    {
        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static bool created;
        public static readonly List<ObjectData> listHead = new List<ObjectData>();
        public static readonly List<ObjectData> listFace = new List<ObjectData>();
        public static readonly List<ObjectData> listDetail = new List<ObjectData>();
        public static RectTransform containerHead;
        public static InputField searchBarHead;
        public static RectTransform containerSkin;
        public static InputField searchBarSkin;
        public static RectTransform containerDetail;
        public static InputField searchBarDetail;

        private static SmFaceSkin _originalComponent;

        public static void Init(SmFaceSkin originalComponent)
        {
            Reset();

            _originalComponent = originalComponent;
            {
                containerHead = _originalComponent.transform.FindChild("TabControl/TabItem01").FindDescendant("ListTop").transform as RectTransform;
                VerticalLayoutGroup group = containerHead.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                ContentSizeFitter fitter = containerHead.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _originalComponent.rtfPanelHead.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                group = _originalComponent.rtfPanelHead.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                RectTransform rt = _originalComponent.transform.FindChild("TabControl/TabItem01/ScrollView") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);
                float newY = rt.offsetMax.y;
                rt = _originalComponent.transform.FindChild("TabControl/TabItem01/Scrollbar") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);

                searchBarHead = UIUtility.CreateInputField("Search Bar", _originalComponent.transform.FindChild("TabControl/TabItem01"));
                searchBarHead.GetComponent<Image>().sprite = HSUS.self.searchBarBackground;
                rt = searchBarHead.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
                searchBarHead.placeholder.GetComponent<Text>().text = "Search...";
                searchBarHead.onValueChanged.AddListener(SearchChangedHead);
                foreach (Text t in searchBarHead.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }

            {
                containerSkin = _originalComponent.transform.FindChild("TabControl/TabItem02").FindDescendant("ListTop").transform as RectTransform;
                VerticalLayoutGroup group = containerSkin.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                ContentSizeFitter fitter = containerSkin.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _originalComponent.rtfPanelSkin.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                group = _originalComponent.rtfPanelSkin.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                RectTransform rt = _originalComponent.transform.FindChild("TabControl/TabItem02/ScrollView") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);
                float newY = rt.offsetMax.y;
                rt = _originalComponent.transform.FindChild("TabControl/TabItem02/Scrollbar") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);

                searchBarSkin = UIUtility.CreateInputField("Search Bar", _originalComponent.transform.FindChild("TabControl/TabItem02"));
                searchBarSkin.GetComponent<Image>().sprite = HSUS.self.searchBarBackground;
                rt = searchBarSkin.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
                searchBarSkin.placeholder.GetComponent<Text>().text = "Search...";
                searchBarSkin.onValueChanged.AddListener(SearchChangedSkin);
                foreach (Text t in searchBarSkin.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }

            {
                containerDetail = _originalComponent.transform.FindChild("TabControl/TabItem03").FindDescendant("ListTop").transform as RectTransform;
                VerticalLayoutGroup group = containerDetail.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                ContentSizeFitter fitter = containerDetail.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _originalComponent.rtfPanelDetail.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                group = _originalComponent.rtfPanelDetail.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                RectTransform rt = _originalComponent.transform.FindChild("TabControl/TabItem03/ScrollView") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);
                float newY = rt.offsetMax.y;
                rt = _originalComponent.transform.FindChild("TabControl/TabItem03/Scrollbar") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);

                searchBarDetail = UIUtility.CreateInputField("Search Bar", _originalComponent.transform.FindChild("TabControl/TabItem03"));
                searchBarDetail.GetComponent<Image>().sprite = HSUS.self.searchBarBackground;
                rt = searchBarDetail.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
                searchBarDetail.placeholder.GetComponent<Text>().text = "Search...";
                searchBarDetail.onValueChanged.AddListener(SearchChangedDetail);
                foreach (Text t in searchBarDetail.GetComponentsInChildren<Text>())
                    t.color = Color.white;
            }
        }

        private static void Reset()
        {
            listHead.Clear();
            listFace.Clear();
            listDetail.Clear();
        }

        public static void SearchChangedHead(string arg0)
        {
            SearchChanged(searchBarHead.text.Trim(), listHead);
        }

        public static void SearchChangedSkin(string arg0)
        {
            SearchChanged(searchBarSkin.text.Trim(), listFace);
        }

        public static void SearchChangedDetail(string arg0)
        {
            SearchChanged(searchBarDetail.text.Trim(), listDetail);
        }

        public static void SearchChanged(string search, List<ObjectData> list)
        {
            if (list != null)
                foreach (ObjectData objectData in list)
                {
                    bool active = objectData.obj.activeSelf;
                    ToggleGroup group = objectData.toggle.group;
                    objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                    if (active && objectData.obj.activeSelf == false)
                        group.RegisterToggle(objectData.toggle);
                }
        }
    }

    [HarmonyPatch(typeof(SmFaceSkin), "SetCharaInfoSub")]
    public class SmFaceSkin_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmFaceSkin __instance)
        {
            SmFaceSkin_Data.searchBarHead.text = "";
            SmFaceSkin_Data.SearchChangedHead("");
            SmFaceSkin_Data.searchBarSkin.text = "";
            SmFaceSkin_Data.SearchChangedSkin("");
            SmFaceSkin_Data.searchBarDetail.text = "";
            SmFaceSkin_Data.SearchChangedDetail("");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            CharFileInfoCustom customInfo = (CharFileInfoCustom)__instance.GetPrivate("customInfo");
            if (null == chaInfo)
                return;
            if (null == __instance.objListTopHead)
                return;
            if (null == __instance.objListTopSkin)
                return;
            if (null == __instance.objListTopDetail)
                return;
            if (null == __instance.objLineBase)
                return;
            if (null == __instance.rtfPanelHead)
                return;
            if (null == __instance.rtfPanelSkin)
                return;
            if (null == __instance.rtfPanelDetail)
                return;
            if (SmFaceSkin_Data.created == false)
            {
                Dictionary<int, ListTypeFbx> dictionary = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleFbxList(CharaListInfo.TypeMaleFbx.cm_f_head, true) : chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_head, true);
                int num = 0;
                if (customInfo != null)
                {
                    num = customInfo.headId;
                }
                int count = 0;
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                        fbxTypeInfo.id = current.Key;
                        fbxTypeInfo.typeName = current.Value.Name;
                        fbxTypeInfo.info = current.Value;
                        gameObject.transform.SetParent(__instance.objListTopHead.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(SmFaceSkin_Data.containerHead.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;
                        __instance.CallPrivateExplicit<SmFaceSkin>("SetHeadButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        SmFaceSkin_Data.listHead.Add(new SmFaceSkin_Data.ObjectData(){key = current.Key, obj = gameObject, text = component, toggle = component2});
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                        });
                        if (current.Key == num)
                        {
                            component2.isOn = true;
                        }
                        ToggleGroup component3 = __instance.objListTopHead.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        int num3 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform = rectTransform.FindChild("imgNew");
                        if (transform && num3 == 1)
                        {
                            transform.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
                Dictionary<int, ListTypeTexture> dictionary2 = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_face, true) : chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_face, true);
                num = 0;
                if (customInfo != null)
                {
                    num = customInfo.texFaceId;
                }
                count = 0;
                foreach (KeyValuePair<int, ListTypeTexture> current in dictionary2)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject2 = GameObject.Instantiate(__instance.objLineBase);
                        gameObject2.AddComponent<LayoutElement>().preferredHeight = 24f;
                        TexTypeInfo texTypeInfo = gameObject2.AddComponent<TexTypeInfo>();
                        texTypeInfo.id = current.Key;
                        texTypeInfo.typeName = current.Value.Name;
                        texTypeInfo.info = current.Value;
                        gameObject2.transform.SetParent(__instance.objListTopSkin.transform, false);
                        RectTransform rectTransform2 = gameObject2.transform as RectTransform;
                        rectTransform2.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform2.sizeDelta = new Vector2(SmFaceSkin_Data.containerSkin.rect.width, 24f);
                        Text component4 = rectTransform2.FindChild("Label").GetComponent<Text>();
                        component4.text = texTypeInfo.typeName;
                        __instance.CallPrivateExplicit<SmFaceSkin>("SetSkinButtonClickHandler", gameObject2);
                        Toggle component5 = gameObject2.GetComponent<Toggle>();
                        SmFaceSkin_Data.listFace.Add(new SmFaceSkin_Data.ObjectData() { key = current.Key, obj = gameObject2, text = component4, toggle = component5 });
                        if (current.Key == num)
                        {
                            component5.isOn = true;
                        }
                        ToggleGroup component6 = __instance.objListTopSkin.GetComponent<ToggleGroup>();
                        component5.group = component6;
                        gameObject2.SetActive(true);
                        int num5 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform2 = rectTransform2.FindChild("imgNew");
                        if (transform2 && num5 == 1)
                        {
                            transform2.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
                dictionary2 = chaInfo.Sex == 0 ? chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_detail_f, true) : chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_detail_f, true);
                num = 0;
                if (customInfo != null)
                {
                    num = customInfo.texFaceDetailId;
                }
                count = 0;
                foreach (KeyValuePair<int, ListTypeTexture> current in dictionary2)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject3 = GameObject.Instantiate(__instance.objLineBase);
                        gameObject3.AddComponent<LayoutElement>().preferredHeight = 24f;
                        TexTypeInfo texTypeInfo2 = gameObject3.AddComponent<TexTypeInfo>();
                        texTypeInfo2.id = current.Key;
                        texTypeInfo2.typeName = current.Value.Name;
                        texTypeInfo2.info = current.Value;
                        gameObject3.transform.SetParent(__instance.objListTopDetail.transform, false);
                        RectTransform rectTransform3 = gameObject3.transform as RectTransform;
                        rectTransform3.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform3.sizeDelta = new Vector2(SmFaceSkin_Data.containerDetail.rect.width, 24f);
                        Text component7 = rectTransform3.FindChild("Label").GetComponent<Text>();
                        component7.text = texTypeInfo2.typeName;
                        __instance.CallPrivateExplicit<SmFaceSkin>("SetDetailButtonClickHandler", gameObject3);
                        Toggle component8 = gameObject3.GetComponent<Toggle>();
                        SmFaceSkin_Data.listDetail.Add(new SmFaceSkin_Data.ObjectData() { key = current.Key, obj = gameObject3, text = component7, toggle = component8 });
                        if (current.Key == num)
                        {
                            component8.isOn = true;
                        }
                        ToggleGroup component9 = __instance.objListTopDetail.GetComponent<ToggleGroup>();
                        component8.group = component9;
                        gameObject3.SetActive(true);
                        int num7 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform3 = rectTransform3.FindChild("imgNew");
                        if (transform3 && num7 == 1)
                        {
                            transform3.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
            }
            else
            {
                int num = 0;
                if (customInfo != null)
                    num = customInfo.headId;
                foreach (SmFaceSkin_Data.ObjectData objectData in SmFaceSkin_Data.listHead)
                {
                    if (objectData.key == num)
                        objectData.toggle.isOn = true;
                    else if (objectData.toggle.isOn)
                        objectData.toggle.isOn = false;
                }
                num = 0;
                if (customInfo != null)
                    num = customInfo.texFaceId;
                foreach (SmFaceSkin_Data.ObjectData objectData in SmFaceSkin_Data.listFace)
                {
                    if (objectData.key == num)
                        objectData.toggle.isOn = true;
                    else if (objectData.toggle.isOn)
                        objectData.toggle.isOn = false;
                }
                num = 0;
                if (customInfo != null)
                    num = customInfo.texFaceDetailId;
                foreach (SmFaceSkin_Data.ObjectData objectData in SmFaceSkin_Data.listDetail)
                {
                    if (objectData.key == num)
                        objectData.toggle.isOn = true;
                    else if (objectData.toggle.isOn)
                        objectData.toggle.isOn = false;
                }
            }
            __instance.SetPrivateExplicit<SmFaceSkin>("nowChanging", true);
            if (customInfo != null)
            {
                if (__instance.sldDetail)
                {
                    __instance.sldDetail.value = customInfo.faceDetailWeight;
                }
                if (__instance.inputDetail)
                {
                    __instance.inputDetail.text = (string)__instance.CallPrivate("ChangeTextFromFloat", customInfo.faceDetailWeight);
                }
            }
            __instance.SetPrivateExplicit<SmFaceSkin>("nowChanging", false);
            SmFaceSkin_Data.created = true;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
