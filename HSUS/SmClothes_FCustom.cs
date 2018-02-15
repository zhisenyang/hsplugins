using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        public class TypeData
        {
            public RectTransform parentObject;
            public List<SmClothes_F.IdTglInfo> lstIdTgl = new List<SmClothes_F.IdTglInfo>();
            public Dictionary<int, int> keyToObjectIndex = new Dictionary<int, int>();
            public List<ObjectData> objects = new List<ObjectData>();
        }

        public class ObjectData
        {
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        public static readonly Dictionary<int, TypeData> objects = new Dictionary<int, TypeData>();
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

            //HSUS.self.routines.StartCoroutine(CreateCachedLists());
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
            foreach (ObjectData objectData in objects[(int)_originalComponent.GetPrivate("nowSubMenuTypeId")].objects)
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }

        private static IEnumerator CreateCachedLists()
        {
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)_originalComponent.GetPrivate("clothesInfoF");
            CharInfo chaInfo = (CharInfo)_originalComponent.GetPrivate("chaInfo");
            if (null == chaInfo || null == _originalComponent.objListTop || null == _originalComponent.objLineBase || null == _originalComponent.rtfPanel)
                yield break;

            int count = 0;

            SubMenuControl control = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu").GetComponent<SubMenuControl>();
            for (int nowSubMenuTypeId = 0; nowSubMenuTypeId < control.smItem.Length; nowSubMenuTypeId++)
            {
                if (control.smItem[nowSubMenuTypeId].objTop != _originalComponent.gameObject)
                    continue;
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
                SmClothes_F_Data.TypeData td = new SmClothes_F_Data.TypeData();
                td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
                td.parentObject.transform.SetParent(_originalComponent.objListTop.transform, false);
                td.parentObject.transform.localScale = Vector3.one;
                td.parentObject.transform.localPosition = Vector3.zero;
                td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                SmClothes_F_Data.objects.Add(nowSubMenuTypeId, td);
                ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    bool flag = false;
                    if (chaInfo.customInfo.isConcierge)
                    {
                        flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                    }
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0 && !flag)
                        continue;
                    GameObject gameObject = GameObject.Instantiate(_originalComponent.objLineBase);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                    fbxTypeInfo.id = current.Key;
                    fbxTypeInfo.typeName = current.Value.Name;
                    fbxTypeInfo.info = current.Value;
                    gameObject.transform.SetParent(td.parentObject.transform, false);
                    RectTransform rectTransform = gameObject.transform as RectTransform;
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform.sizeDelta = new Vector2(SmClothes_F_Data.container.rect.width, 24f);
                    Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                    component.text = fbxTypeInfo.typeName;
                    _originalComponent.CallPrivate("SetButtonClickHandler", gameObject);
                    Toggle component2 = gameObject.GetComponent<Toggle>();
                    td.keyToObjectIndex.Add(current.Key, count);
                    td.objects.Add(new SmClothes_F_Data.ObjectData { obj = gameObject, toggle = component2, text = component });
                    component2.onValueChanged.AddListener(v =>
                    {
                        if (component2.isOn)
                            UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                    });
                    if (current.Key == num)
                    {
                        component2.isOn = true;
                    }
                    component2.group = group;
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
                        idTglInfo.imgBG = gameObject2.GetComponent<Image>();
                    gameObject2 = gameObject.transform.FindLoop("Checkmark");
                    if (gameObject2)
                        idTglInfo.imgCheck = gameObject2.GetComponent<Image>();
                    gameObject2 = gameObject.transform.FindLoop("Label");
                    if (gameObject2)
                        idTglInfo.text = gameObject2.GetComponent<Text>();
                    ((List<SmClothes_F.IdTglInfo>)_originalComponent.GetPrivate("lstIdTgl")).Add(idTglInfo);
                    count++;
                    yield return null;
                }
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
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)__instance.GetPrivate("clothesInfoF");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            if (null == chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return;
            if (__instance.tglTab != null)
                __instance.tglTab.isOn = true;
            SmClothes_F_Data.TypeData td;
            if (SmClothes_F_Data.previousType != nowSubMenuTypeId && SmClothes_F_Data.objects.TryGetValue(SmClothes_F_Data.previousType, out td))
                td.parentObject.gameObject.SetActive(false);
            int count = 0;
            int selected = 0;
            if (SmClothes_F_Data.objects.TryGetValue(nowSubMenuTypeId, out td))
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
               td.parentObject.gameObject.SetActive(true);
                selected = td.keyToObjectIndex[num];
                SmClothes_F_Data.ObjectData o = td.objects[selected];
                o.toggle.isOn = true;
                o.toggle.onValueChanged.Invoke(true);
                count = td.objects.Count;
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
                            num = clothesInfoF.clothesId[0];
                        break;
                    case 58:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[1];
                        break;
                    case 59:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bra);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[2];
                        break;
                    case 60:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shorts);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[3];
                        break;
                    case 61:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_gloves);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[7];
                        break;
                    case 62:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_panst);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[8];
                        break;
                    case 63:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_socks);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[9];
                        break;
                    case 64:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shoes);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[10];
                        break;
                    case 76:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimtop);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[5];
                        break;
                    case 77:
                        dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimbot);
                        if (clothesInfoF != null)
                            num = clothesInfoF.clothesId[6];
                        break;
                }
                td = new SmClothes_F_Data.TypeData();
                td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
                td.parentObject.SetParent(__instance.objListTop.transform, false);
                td.parentObject.localScale = Vector3.one;
                td.parentObject.localPosition = Vector3.zero;
                td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                SmClothes_F_Data.objects.Add(nowSubMenuTypeId, td);
                ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    bool flag = false;
                    if (chaInfo.customInfo.isConcierge)
                    {
                        flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                    }
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) == 0 && !flag)
                        continue;
                    GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                    fbxTypeInfo.id = current.Key;
                    fbxTypeInfo.typeName = current.Value.Name;
                    fbxTypeInfo.info = current.Value;
                    gameObject.transform.SetParent(td.parentObject.transform, false);
                    RectTransform rectTransform = gameObject.transform as RectTransform;
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform.sizeDelta = new Vector2(SmClothes_F_Data.container.rect.width, 24f);
                    Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                    component.text = fbxTypeInfo.typeName;
                    __instance.CallPrivate("SetButtonClickHandler", gameObject);
                    Toggle component2 = gameObject.GetComponent<Toggle>();
                    td.keyToObjectIndex.Add(current.Key, count);
                    td.objects.Add(new SmClothes_F_Data.ObjectData { obj = gameObject, toggle = component2, text = component });
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
                    component2.group = group;
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
                        idTglInfo.imgBG = gameObject2.GetComponent<Image>();
                    gameObject2 = gameObject.transform.FindLoop("Checkmark");
                    if (gameObject2)
                        idTglInfo.imgCheck = gameObject2.GetComponent<Image>();
                    gameObject2 = gameObject.transform.FindLoop("Label");
                    if (gameObject2)
                        idTglInfo.text = gameObject2.GetComponent<Text>();
                    td.lstIdTgl.Add(idTglInfo);
                    count++;
                }

            }

            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);
            __instance.SetPrivate("nowChanging", true);
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
                        SmClothes_F_UpdateTopListEnable_Patches.Prefix(__instance);
                        break;
                    case 58:
                        value = clothesInfoF.clothesColor[1].specularIntensity;
                        value2 = clothesInfoF.clothesColor[1].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[1].specularSharpness;
                        SmClothes_F_UpdateBotListEnable_Patches.Prefix(__instance);
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
                    case 76:
                        value = clothesInfoF.clothesColor[5].specularIntensity;
                        value2 = clothesInfoF.clothesColor[5].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[5].specularSharpness;
                        break;
                    case 77:
                        value = clothesInfoF.clothesColor[6].specularIntensity;
                        value2 = clothesInfoF.clothesColor[6].specularSharpness;
                        value3 = clothesInfoF.clothesColor2[6].specularSharpness;
                        break;
                }

                if (__instance.sldIntensity)
                    __instance.sldIntensity.value = value;
                if (__instance.inputIntensity)
                    __instance.inputIntensity.text = (string)__instance.CallPrivate("ChangeTextFromFloat", value);
                if (__instance.sldSharpness[0])
                    __instance.sldSharpness[0].value = value2;
                if (__instance.inputSharpness[0])
                    __instance.inputSharpness[0].text = (string)__instance.CallPrivate("ChangeTextFromFloat", value2);
                if (__instance.sldSharpness[1])
                    __instance.sldSharpness[1].value = value3;
                if (__instance.inputSharpness[1])
                    __instance.inputSharpness[1].text = (string)__instance.CallPrivate("ChangeTextFromFloat", value3);
            }

            __instance.SetPrivate("nowChanging", false);
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

    [HarmonyPatch(typeof(SmClothes_F), "UpdateTopListEnable")]
    public class SmClothes_F_UpdateTopListEnable_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmClothes_F __instance)
        {
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            if (null == chaInfo)
            {
                return;
            }
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            List<SmClothes_F.IdTglInfo> lstIdTgl = SmClothes_F_Data.objects[nowSubMenuTypeId].lstIdTgl;
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)__instance.GetPrivate("clothesInfoF");
            int key = clothesInfoF.clothesId[1];
            Dictionary<int, ListTypeFbx> femaleFbxList = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
            ListTypeFbx fbx;
            if (femaleFbxList.TryGetValue(key, out fbx) && fbx.Etc[1].Equals("1"))
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    if (idTglInfo.coorde != 1)
                    {
                        idTglInfo.tgl.interactable = true;
                        idTglInfo.imgBG.color = Color.white;
                        idTglInfo.imgCheck.color = Color.white;
                        idTglInfo.text.color = Color.white;
                    }
                    else
                    {
                        idTglInfo.tgl.interactable = false;
                        idTglInfo.imgBG.color = Color.gray;
                        idTglInfo.imgCheck.color = Color.gray;
                        idTglInfo.text.color = Color.gray;
                    }
                }
            }
            else
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    idTglInfo.tgl.interactable = true;
                    idTglInfo.imgBG.color = Color.white;
                    idTglInfo.imgCheck.color = Color.white;
                    idTglInfo.text.color = Color.white;
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }


    [HarmonyPatch(typeof(SmClothes_F), "UpdateBotListEnable")]
    public class SmClothes_F_UpdateBotListEnable_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmClothes_F __instance)
        {
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            if (null == chaInfo)
            {
                return;
            }
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            List<SmClothes_F.IdTglInfo> lstIdTgl = SmClothes_F_Data.objects[nowSubMenuTypeId].lstIdTgl;
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)__instance.GetPrivate("clothesInfoF");
            int key = clothesInfoF.clothesId[0];
            Dictionary<int, ListTypeFbx> femaleFbxList = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
            ListTypeFbx fbx;
            if (femaleFbxList.TryGetValue(key, out fbx) && fbx.Etc[1].Equals("1"))
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    if (idTglInfo.coorde != 1)
                    {
                        idTglInfo.tgl.interactable = true;
                        idTglInfo.imgBG.color = Color.white;
                        idTglInfo.imgCheck.color = Color.white;
                        idTglInfo.text.color = Color.white;
                    }
                    else
                    {
                        idTglInfo.tgl.interactable = false;
                        idTglInfo.imgBG.color = Color.gray;
                        idTglInfo.imgCheck.color = Color.gray;
                        idTglInfo.text.color = Color.gray;
                    }
                }
            }
            else
            {
                foreach (SmClothes_F.IdTglInfo idTglInfo in lstIdTgl)
                {
                    idTglInfo.tgl.interactable = true;
                    idTglInfo.imgBG.color = Color.white;
                    idTglInfo.imgCheck.color = Color.white;
                    idTglInfo.text.color = Color.white;
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}