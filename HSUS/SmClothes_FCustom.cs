using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmClothes_F_Data
    {
        public class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static readonly Dictionary<int, List<ObjectData>> objects = new Dictionary<int, List<ObjectData>>();
        public static int previousType = -1;
        public static RectTransform container;
        public static InputField searchBar;

        private static SmClothes_F _originalComponent;

        public static void Init(SmClothes_F originalComponent)
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
        }
        private static void Reset()
        {
            objects.Clear();
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            if (objects.ContainsKey((int)_originalComponent.GetPrivate("nowSubMenuTypeId")) == false)
                return;
            foreach (ObjectData objectData in objects[(int)_originalComponent.GetPrivate("nowSubMenuTypeId")])
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }
    }

    [HarmonyPatch(typeof(SmClothes_F))]
    [HarmonyPatch("SetCharaInfoSub")]
    public class SmClothes_F_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmClothes_F __instance)
        {
            SmClothes_F_Data.searchBar.text = "";
            SmClothes_F_Data.SearchChanged("");
            int nowSubMenuTypeId = (int) __instance.GetPrivate("nowSubMenuTypeId");
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)__instance.GetPrivate("clothesInfoF");
            CharInfo chaInfo = (CharInfo) __instance.GetPrivate("chaInfo");
            if (null == __instance.GetPrivate("chaInfo") || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;
            if (SmClothes_F_Data.previousType != nowSubMenuTypeId && SmClothes_F_Data.objects.ContainsKey(SmClothes_F_Data.previousType))
                foreach (SmClothes_F_Data.ObjectData o in SmClothes_F_Data.objects[SmClothes_F_Data.previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (SmClothes_F_Data.objects.ContainsKey(nowSubMenuTypeId))
            {
                int num = 0;
                if (clothesInfoF != null)
                    switch (nowSubMenuTypeId)
                    {
                        case 57:
                            num = clothesInfoF.clothesId[0];
                            break;
                        case 58:
                            num = clothesInfoF.clothesId[1];
                            break;
                        case 59:
                            num = clothesInfoF.clothesId[2];
                            break;
                        case 60:
                            num = clothesInfoF.clothesId[3];
                            break;
                        case 61:
                            num = clothesInfoF.clothesId[7];
                            break;
                        case 62:
                            num = clothesInfoF.clothesId[8];
                            break;
                        case 63:
                            num = clothesInfoF.clothesId[9];
                            break;
                        case 64:
                            num = clothesInfoF.clothesId[10];
                            break;
                        case 76:
                            num = clothesInfoF.clothesId[5];
                            break;
                        case 77:
                            num = clothesInfoF.clothesId[6];
                            break;
                    }
                count = SmClothes_F_Data.objects[nowSubMenuTypeId].Count;
                for (int i = 0; i < SmClothes_F_Data.objects[nowSubMenuTypeId].Count; i++)
                {
                    SmClothes_F_Data.ObjectData o = SmClothes_F_Data.objects[nowSubMenuTypeId][i];
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
                    case 57:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[0];
                        }
                        break;
                    case 58:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[1];
                        }
                        break;
                    case 59:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bra);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[2];
                        }
                        break;
                    case 60:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shorts);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[3];
                        }
                        break;
                    case 61:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_gloves);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[7];
                        }
                        break;
                    case 62:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_panst);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[8];
                        }
                        break;
                    case 63:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_socks);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[9];
                        }
                        break;
                    case 64:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shoes);
                        if (clothesInfoF != null)
                        {
                            num = clothesInfoF.clothesId[10];
                        }
                        break;
                    default:
                        if (nowSubMenuTypeId != 76)
                        {
                            if (nowSubMenuTypeId == 77)
                            {
                                dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimbot);
                                if (clothesInfoF != null)
                                {
                                    num = clothesInfoF.clothesId[6];
                                }
                            }
                        }
                        else
                        {
                            dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimtop);
                            if (clothesInfoF != null)
                            {
                                num = clothesInfoF.clothesId[5];
                            }
                        }
                        break;
                }
                List<SmClothes_F_Data.ObjectData> cd = new List<SmClothes_F_Data.ObjectData>();
                SmClothes_F_Data.objects.Add(nowSubMenuTypeId, cd);
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    bool flag = false;
                    if (chaInfo.customInfo.isConcierge)
                    {
                        flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                    }
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0 || flag)
                    {
                        GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                        fbxTypeInfo.id = current.Key;
                        fbxTypeInfo.typeName = current.Value.Name;
                        fbxTypeInfo.info = current.Value;
                        gameObject.transform.SetParent(__instance.objListTop.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(SmClothes_F_Data.container.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;
                        __instance.CallPrivateExplicit<SmClothes_F>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new SmClothes_F_Data.ObjectData { key = current.Key, obj = gameObject, toggle = component2, text = component });
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
                        if (!flag)
                        {
                            int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                            Transform transform = rectTransform.FindChild("imgNew");
                            if (transform && num4 == 1)
                            {
                                transform.gameObject.SetActive(true);
                            }
                        }
                        SmClothes_F.IdTglInfo idTglInfo = new SmClothes_F.IdTglInfo();
                        idTglInfo.coorde = int.Parse(current.Value.Etc[1]);
                        idTglInfo.tgl = component2;
                        GameObject gameObject2 = gameObject.transform.FindLoop("Background");
                        if (gameObject2)
                        {
                            idTglInfo.imgBG = gameObject2.GetComponent<Image>();
                        }
                        gameObject2 = gameObject.transform.FindLoop("Checkmark");
                        if (gameObject2)
                        {
                            idTglInfo.imgCheck = gameObject2.GetComponent<Image>();
                        }
                        gameObject2 = gameObject.transform.FindLoop("Label");
                        if (gameObject2)
                        {
                            idTglInfo.text = gameObject2.GetComponent<Text>();
                        }
                        ((List<SmClothes_F.IdTglInfo>)__instance.GetPrivateExplicit<SmClothes_F>("lstIdTgl")).Add(idTglInfo);
                        count++;
                    }
                }
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);
            __instance.SetPrivateExplicit<SmClothes_F>("nowChanging", true);
            if (clothesInfoF != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 57:
                        value = clothesInfoF.clothesColor[0].specularIntensity;
                        value2 = clothesInfoF.clothesColor[0].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[0].specularSharpness;
                        __instance.CallPrivateExplicit<SmClothes_F>("UpdateTopListEnable");
                        break;
                    case 58:
                        value = clothesInfoF.clothesColor[1].specularIntensity;
                        value2 = clothesInfoF.clothesColor[1].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[1].specularSharpness;
                        __instance.CallPrivateExplicit<SmClothes_F>("UpdateTopListEnable");
                        break;
                    case 59:
                        value = clothesInfoF.clothesColor[2].specularIntensity;
                        value2 = clothesInfoF.clothesColor[2].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[2].specularSharpness;
                        break;
                    case 60:
                        value = clothesInfoF.clothesColor[3].specularIntensity;
                        value2 = clothesInfoF.clothesColor[3].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[3].specularSharpness;
                        break;
                    case 61:
                        value = clothesInfoF.clothesColor[7].specularIntensity;
                        value2 = clothesInfoF.clothesColor[7].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[7].specularSharpness;
                        break;
                    case 62:
                        value = clothesInfoF.clothesColor[8].specularIntensity;
                        value2 = clothesInfoF.clothesColor[8].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[8].specularSharpness;
                        break;
                    case 63:
                        value = clothesInfoF.clothesColor[9].specularIntensity;
                        value2 = clothesInfoF.clothesColor[9].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[9].specularSharpness;
                        break;
                    case 64:
                        value = clothesInfoF.clothesColor[10].specularIntensity;
                        value2 = clothesInfoF.clothesColor[10].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[10].specularSharpness;
                        break;
                    default:
                        if (nowSubMenuTypeId != 76)
                        {
                            if (nowSubMenuTypeId == 77)
                            {
                                value = clothesInfoF.clothesColor[6].specularIntensity;
                                value2 = clothesInfoF.clothesColor[6].specularSharpness;
                                value3 = clothesInfoF.clothesColor2[6].specularSharpness;
                            }
                        }
                        else
                        {
                            value = clothesInfoF.clothesColor[5].specularIntensity;
                            value2 = clothesInfoF.clothesColor[5].specularSharpness;
                            value3 = clothesInfoF.clothesColor2[5].specularSharpness;
                        }
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
                if (__instance.sldSharpness[0])
                {
                    __instance.sldSharpness[0].value = value2;
                }
                if (__instance.inputSharpness[0])
                {
                    __instance.inputSharpness[0].text = __instance.ChangeTextFromFloat(value2);
                }
                if (__instance.sldSharpness[1])
                {
                    __instance.sldSharpness[1].value = value3;
                }
                if (__instance.inputSharpness[1])
                {
                    __instance.inputSharpness[1].text = __instance.ChangeTextFromFloat(value3);
                }
            }
            __instance.SetPrivateExplicit<SmClothes_F>("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);
            SmClothes_F_Data.previousType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
