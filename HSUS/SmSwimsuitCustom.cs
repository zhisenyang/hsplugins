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
    public static class SmSwimsuit_Data
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

        private static SmSwimsuit _originalComponent;

        public static void Init(SmSwimsuit originalComponent)
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

    [HarmonyPatch(typeof(SmSwimsuit), "SetCharaInfoSub")]
    public class SmSwimsuit_SetCharaInfoSub_Patches
    {
        public static void Prefix(SmSwimsuit __instance)
        {
            SmSwimsuit_Data._searchBar.text = "";
            SmSwimsuit_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            CharFileInfoClothes clothesInfo = (CharFileInfoClothes)__instance.GetPrivate("clothesInfo");
            CharFileInfoClothesFemale clothesInfoF = (CharFileInfoClothesFemale)__instance.GetPrivate("clothesInfoF");
            CharInfo chaInfo = (CharInfo)__instance.GetPrivate("chaInfo");
            if (null == chaInfo || null == __instance.objListTop || null == __instance.objLineBase || null == __instance.rtfPanel)
                return;
            if (null != __instance.tglTab)
                __instance.tglTab.isOn = true;

            if (SmSwimsuit_Data._previousType != nowSubMenuTypeId && SmSwimsuit_Data._objects.ContainsKey(SmSwimsuit_Data._previousType))
                foreach (SmSwimsuit_Data.ObjectData o in SmSwimsuit_Data._objects[SmSwimsuit_Data._previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (SmSwimsuit_Data._objects.ContainsKey(nowSubMenuTypeId))
            {
                int num2 = 0;
                if (clothesInfo != null)
                {
                    num2 = clothesInfo.clothesId[4];
                }
                count = SmSwimsuit_Data._objects[nowSubMenuTypeId].Count;
                for (int i = 0; i < SmSwimsuit_Data._objects[nowSubMenuTypeId].Count; i++)
                {
                    SmSwimsuit_Data.ObjectData o = SmSwimsuit_Data._objects[nowSubMenuTypeId][i];
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
                SmSwimsuit_Data._objects.Add(nowSubMenuTypeId, cd);

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
                        rectTransform.sizeDelta = new Vector2(SmSwimsuit_Data._container.rect.width, 24f);
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
                    __instance.inputIntensity.text = __instance.ChangeTextFromFloat(specularIntensity);
                }
                if (__instance.sldSharpness[0])
                {
                    __instance.sldSharpness[0].value = specularSharpness;
                }
                if (__instance.inputSharpness[0])
                {
                    __instance.inputSharpness[0].text = __instance.ChangeTextFromFloat(specularSharpness);
                }
                if (__instance.sldSharpness[1])
                {
                    __instance.sldSharpness[1].value = specularSharpness2;
                }
                if (__instance.inputSharpness[1])
                {
                    __instance.inputSharpness[1].text = __instance.ChangeTextFromFloat(specularSharpness2);
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
            SmSwimsuit_Data._previousType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
