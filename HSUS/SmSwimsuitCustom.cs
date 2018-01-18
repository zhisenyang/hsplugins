using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Policy;
using CustomMenu;
using Harmony;
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

        public static readonly Dictionary<int, List<ObjectData>> objects = new Dictionary<int, List<ObjectData>>();
        public static int previousType;
        public static RectTransform container;
        public static InputField searchBar;
        public static List<OneTimeVerticalLayoutGroup> groups = new List<OneTimeVerticalLayoutGroup>();

        private static SmSwimsuit _originalComponent;

        public static void Init(SmSwimsuit originalComponent)
        {
            Reset();

            _originalComponent = originalComponent;

            container = _originalComponent.transform.FindDescendant("ListTop").transform as RectTransform;
            OneTimeVerticalLayoutGroup group = container.gameObject.AddComponent<OneTimeVerticalLayoutGroup>();
            groups.Add(group);
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _originalComponent.rtfPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = _originalComponent.rtfPanel.gameObject.AddComponent<OneTimeVerticalLayoutGroup>();
            groups.Add(group);
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
            groups.Clear();
        }
        public static void UpdateAllGroups()
        {
            foreach (OneTimeVerticalLayoutGroup group in groups)
            {
                group.UpdateLayout();
            }
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            if (objects.ContainsKey(previousType) == false)
                return;
            foreach (ObjectData objectData in objects[previousType])
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

        public static void Prefix(SmSwimsuit __instance)
        {
            SmSwimsuit_Data.searchBar.text = "";
            SmSwimsuit_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharFileInfoClothes clothesInfo = (CharFileInfoClothes)__instance.GetPrivate("clothesInfo");
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)__instance.GetPrivate("clothesInfoF");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            if (null == chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;

            if (SmSwimsuit_Data.previousType != nowSubMenuTypeId && SmSwimsuit_Data.objects.ContainsKey(SmSwimsuit_Data.previousType))
                foreach (SmSwimsuit_Data.ObjectData o in SmSwimsuit_Data.objects[SmSwimsuit_Data.previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (SmSwimsuit_Data.objects.ContainsKey(nowSubMenuTypeId))
            {
                int num2 = 0;
                if (clothesInfo != null)
                {
                    num2 = clothesInfo.clothesId[4];
                }
                count = SmSwimsuit_Data.objects[nowSubMenuTypeId].Count;
                for (int i = 0; i < SmSwimsuit_Data.objects[nowSubMenuTypeId].Count; i++)
                {
                    SmSwimsuit_Data.ObjectData o = SmSwimsuit_Data.objects[nowSubMenuTypeId][i];
                    o.obj.SetActive(true);
                    if (o.key == num2)
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
                int num2 = 0;
                dictionary = chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swim, true);
                if (clothesInfo != null)
                {
                    num2 = clothesInfo.clothesId[4];
                }

                List<SmSwimsuit_Data.ObjectData> cd = new List<SmSwimsuit_Data.ObjectData>();
                SmSwimsuit_Data.objects.Add(nowSubMenuTypeId, cd);

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
                        gameObject.transform.SetParent(__instance.objListTop.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(SmSwimsuit_Data.container.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;
                        __instance.CallPrivateExplicit<SmSwimsuit>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new SmSwimsuit_Data.ObjectData() {key = current.Key, obj = gameObject, toggle = component2, text = component});
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                        });
                        if (current.Key == num2)
                        {
                            component2.isOn = true;
                            selected = count;
                        }
                        ToggleGroup component3 = __instance.objListTop.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        int num5 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform = rectTransform.FindChild("imgNew");
                        if (transform && num5 == 1)
                        {
                            transform.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
            }
            SmSwimsuit_Data.UpdateAllGroups();
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            __instance.rtfPanel.anchoredPosition = new Vector2(0f, y);

            __instance.SetPrivateExplicit<SmSwimsuit>("nowChanging", true);
            if (clothesInfo != null)
            {
                float specularIntensity = clothesInfo.clothesColor[4].specularIntensity;
                float specularSharpness = clothesInfo.clothesColor[4].specularSharpness;
                float specularSharpness2 = clothesInfo.clothesColor2[4].specularSharpness;
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
            __instance.SetPrivateExplicit<SmSwimsuit>("nowChanging", false);
            __instance.OnClickColorSpecular(1);
            __instance.OnClickColorSpecular(0);
            __instance.OnClickColorDiffuse(1);
            __instance.OnClickColorDiffuse(0);
            if (__instance.tglOpt01)
            {
                __instance.tglOpt01.isOn = !clothesInfoF.hideSwimOptTop;
            }
            if (__instance.tglOpt02)
            {
                __instance.tglOpt02.isOn = !clothesInfoF.hideSwimOptBot;
            }
            SmSwimsuit_Data.previousType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

}
