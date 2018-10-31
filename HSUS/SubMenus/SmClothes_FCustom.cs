#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using ToolBox;
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
            public readonly List<SmClothes_F.IdTglInfo> lstIdTgl = new List<SmClothes_F.IdTglInfo>();
            public readonly Dictionary<int, int> keyToObjectIndex = new Dictionary<int, int>();
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
        internal static readonly Dictionary<int, IEnumerator> _methods = new Dictionary<int, IEnumerator>();
        internal static PropertyInfo _translateProperty = null;

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

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            int[] keys = { 57, 58, 59, 60, 61, 62, 63, 64, 76, 77 };
            if (HSUS.self._asyncLoading)
            {
                CharInfo chaInfo = originalComponent.customControl.chainfo;
                foreach (int key in keys)
                {
                    IEnumerator method = SmClothes_F_SetCharaInfoSub_Patches.SetCharaInfoSub(originalComponent, key, chaInfo);
                    _methods.Add(key, method);
                    HSUS._self._asyncMethods.Add(method);
                }
            }
            else
            {
                foreach (int key in keys)
                    _methods.Add(key, null);
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
    }

    [HarmonyPatch(typeof(SmClothes_F))]
    [HarmonyPatch("SetCharaInfoSub")]
    public class SmClothes_F_SetCharaInfoSub_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmClothes_F __instance, CharInfo ___chaInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            SmClothes_F_Data.searchBar.text = "";
            SmClothes_F_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (null == ___chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return false;
            if (__instance.tglTab != null)
                __instance.tglTab.isOn = true;

            IEnumerator method;
            if (SmClothes_F_Data._methods.TryGetValue(nowSubMenuTypeId, out method) == false || method == null || method.MoveNext() == false)
                method = SetCharaInfoSub(__instance, nowSubMenuTypeId, ___chaInfo);

            foreach (KeyValuePair<int, SmClothes_F_Data.TypeData> pair in SmClothes_F_Data.objects)
            {
                if (pair.Value.parentObject.gameObject.activeSelf)
                    pair.Value.parentObject.gameObject.SetActive(false);
            }

            while (method.MoveNext());

            SmClothes_F_Data.TypeData td = SmClothes_F_Data.objects[nowSubMenuTypeId];

            int num = 0;
            if (___clothesInfoF != null)
                switch (nowSubMenuTypeId)
                {
                    case 57:
                        num = ___clothesInfoF.clothesId[0];
                        break;
                    case 58:
                        num = ___clothesInfoF.clothesId[1];
                        break;
                    case 59:
                        num = ___clothesInfoF.clothesId[2];
                        break;
                    case 60:
                        num = ___clothesInfoF.clothesId[3];
                        break;
                    case 61:
                        num = ___clothesInfoF.clothesId[7];
                        break;
                    case 62:
                        num = ___clothesInfoF.clothesId[8];
                        break;
                    case 63:
                        num = ___clothesInfoF.clothesId[9];
                        break;
                    case 64:
                        num = ___clothesInfoF.clothesId[10];
                        break;
                    case 76:
                        num = ___clothesInfoF.clothesId[5];
                        break;
                    case 77:
                        num = ___clothesInfoF.clothesId[6];
                        break;
                }

            td.parentObject.gameObject.SetActive(true);
            int selected = td.keyToObjectIndex[num];
            SmClothes_F_Data.ObjectData o = td.objects[selected];
            o.toggle.isOn = true;
            o.toggle.onValueChanged.Invoke(true);

            float b = 24f * td.objects.Count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivate("nowChanging", true);
            if (___clothesInfoF != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                switch (nowSubMenuTypeId)
                {
                    case 57:
                        value = ___clothesInfoF.clothesColor[0].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[0].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[0].specularSharpness;
                        SmClothes_F_UpdateTopListEnable_Patches.Prefix(__instance, ___chaInfo, ___clothesInfoF);
                        break;
                    case 58:
                        value = ___clothesInfoF.clothesColor[1].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[1].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[1].specularSharpness;
                        SmClothes_F_UpdateBotListEnable_Patches.Prefix(__instance, ___chaInfo, ___clothesInfoF);
                        break;
                    case 59:
                        value = ___clothesInfoF.clothesColor[2].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[2].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[2].specularSharpness;
                        break;
                    case 60:
                        value = ___clothesInfoF.clothesColor[3].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[3].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[3].specularSharpness;
                        break;
                    case 61:
                        value = ___clothesInfoF.clothesColor[7].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[7].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[7].specularSharpness;
                        break;
                    case 62:
                        value = ___clothesInfoF.clothesColor[8].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[8].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[8].specularSharpness;
                        break;
                    case 63:
                        value = ___clothesInfoF.clothesColor[9].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[9].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[9].specularSharpness;
                        break;
                    case 64:
                        value = ___clothesInfoF.clothesColor[10].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[10].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[10].specularSharpness;
                        break;
                    case 76:
                        value = ___clothesInfoF.clothesColor[5].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[5].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[5].specularSharpness;
                        break;
                    case 77:
                        value = ___clothesInfoF.clothesColor[6].specularIntensity;
                        value2 = ___clothesInfoF.clothesColor[6].specularSharpness;
                        value3 = ___clothesInfoF.clothesColor2[6].specularSharpness;
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

            SmClothes_F_Data._methods[nowSubMenuTypeId] = null;
            SmClothes_F_Data.previousType = nowSubMenuTypeId;

            return false;
        }

        internal static IEnumerator SetCharaInfoSub(SmClothes_F __instance, int nowSubMenuTypeId, CharInfo chaInfo)
        {
            if (SmClothes_F_Data.objects.ContainsKey(nowSubMenuTypeId) != false)
                yield break;
            int count = 0;
            Dictionary<int, ListTypeFbx> dictionary = null;
            switch (nowSubMenuTypeId)
            {
                case 57:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
                    break;
                case 58:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
                    break;
                case 59:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bra);
                    break;
                case 60:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shorts);
                    break;
                case 61:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_gloves);
                    break;
                case 62:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_panst);
                    break;
                case 63:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_socks);
                    break;
                case 64:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shoes);
                    break;
                case 76:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimtop);
                    break;
                case 77:
                    dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimbot);
                    break;
            }
            SmClothes_F_Data.TypeData td = new SmClothes_F_Data.TypeData();
            td.parentObject = new GameObject("Type " + nowSubMenuTypeId, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup)).GetComponent<RectTransform>();
            td.parentObject.SetParent(__instance.objListTop.transform, false);
            td.parentObject.localScale = Vector3.one;
            td.parentObject.localPosition = Vector3.zero;
            td.parentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SmClothes_F_Data.objects.Add(nowSubMenuTypeId, td);
            ToggleGroup group = td.parentObject.GetComponent<ToggleGroup>();
            td.parentObject.gameObject.SetActive(false);
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
                if (HSUS._self._asyncLoading && SmClothes_F_Data._translateProperty != null) // Fuck you translation plugin
                {
                    SmClothes_F_Data._translateProperty.SetValue(component, false, null);
                    string t = fbxTypeInfo.typeName;
                    HSUS._self._translationMethod(ref t);
                    component.text = t;
                }
                else
                    component.text = fbxTypeInfo.typeName;
                __instance.CallPrivate("SetButtonClickHandler", gameObject);
                Toggle component2 = gameObject.GetComponent<Toggle>();
                td.keyToObjectIndex.Add(current.Key, count);
                td.objects.Add(new SmClothes_F_Data.ObjectData {obj = gameObject, toggle = component2, text = component});
                component2.onValueChanged.AddListener(v =>
                {
                    if (component2.isOn)
                        UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                });
                component2.group = group;
                gameObject.SetActive(true);
                if (!flag)
                {
                    int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                    Transform transform = rectTransform.FindChild("imgNew");
                    if (transform && num4 == 1)
                        transform.gameObject.SetActive(true);
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
                yield return null;
            }
        }
    }

    [HarmonyPatch(typeof(SmClothes_F), "UpdateTopListEnable")]
    public class SmClothes_F_UpdateTopListEnable_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmClothes_F __instance, CharInfo ___chaInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            if (null == ___chaInfo)
            {
                return false;
            }
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            List<SmClothes_F.IdTglInfo> lstIdTgl = SmClothes_F_Data.objects[nowSubMenuTypeId].lstIdTgl;
            int key = ___clothesInfoF.clothesId[1];
            Dictionary<int, ListTypeFbx> femaleFbxList = ___chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
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
            return false;
        }
    }


    [HarmonyPatch(typeof(SmClothes_F), "UpdateBotListEnable")]
    public class SmClothes_F_UpdateBotListEnable_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmClothes_F __instance, CharInfo ___chaInfo, CharFileInfoClothesFemale ___clothesInfoF)
        {
            if (null == ___chaInfo)
            {
                return false;
            }
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            List<SmClothes_F.IdTglInfo> lstIdTgl = SmClothes_F_Data.objects[nowSubMenuTypeId].lstIdTgl;
            int key = ___clothesInfoF.clothesId[0];
            Dictionary<int, ListTypeFbx> femaleFbxList = ___chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
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
            return false;
        }
    }
}
#endif