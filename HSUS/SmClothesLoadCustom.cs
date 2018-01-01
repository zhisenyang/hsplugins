using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using System.Threading;
using Harmony;
using HSUS;
using IllusionUtility.GetUtility;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public static class SmClothesLoad_Data
    {
        public static bool _created;
        public static RectTransform _container;
        public static readonly Dictionary<SmClothesLoad.FileInfo, GameObject> _objects = new Dictionary<SmClothesLoad.FileInfo, GameObject>();
        public static List<SmClothesLoad.FileInfo> _fileInfos;
        public static List<RectTransform> _rectTransforms;
        public static InputField _searchBar;

        private static SmClothesLoad _originalComponent;
        public static void Init(SmClothesLoad originalComponent)
        {
            _originalComponent = originalComponent;

            _fileInfos = (List<SmClothesLoad.FileInfo>)_originalComponent.GetPrivateExplicit<SmClothesLoad>("lstFileInfo");
            _rectTransforms = ((List<RectTransform>)_originalComponent.GetPrivateExplicit<SmClothesLoad>("lstRtfTgl"));

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

            RectTransform rt = _originalComponent.transform.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);
            float newY = rt.offsetMax.y;
            rt = _originalComponent.transform.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);

            _searchBar = UIUtility.CreateInputField("Search Bar", _originalComponent.transform);
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
            if (_objects != null && _objects.Count != 0)
                foreach (SmClothesLoad.FileInfo fi in _fileInfos)
                    _objects[fi].SetActive(fi.comment.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "CreateListObject")]
    public class SmClothesLoad_CreateListObject_Patches
    {
        public static void Prefix(SmClothesLoad __instance)
        {
            SmClothesLoad_Data._searchBar.text = "";
            SmClothesLoad_Data.SearchChanged("");
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;
            if (SmClothesLoad_Data._created)
                return;
            while (__instance.objListTop.transform.childCount != 0)
            {
                GameObject obj = __instance.objListTop.transform.GetChild(0).gameObject;
                obj.transform.SetParent(null);
                GameObject.Destroy(obj);
            }
            SmClothesLoad_Data._rectTransforms.Clear();
            ToggleGroup component = __instance.objListTop.GetComponent<ToggleGroup>();
            int num2 = 0;
            foreach (SmClothesLoad.FileInfo fi in SmClothesLoad_Data._fileInfos)
            {
                GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                SmClothesLoad_Data._objects.Add(fi, gameObject);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                SmClothesLoad.FileInfoComponent fileInfoComponent = gameObject.AddComponent<SmClothesLoad.FileInfoComponent>();
                fileInfoComponent.info = fi;
                GameObject gameObject2 = null;
                fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                gameObject2 = gameObject.transform.FindLoop("Background");
                if (gameObject2)
                {
                    fileInfoComponent.imgBG = gameObject2.GetComponent<Image>();
                }
                gameObject2 = gameObject.transform.FindLoop("Checkmark");
                if (gameObject2)
                {
                    fileInfoComponent.imgCheck = gameObject2.GetComponent<Image>();
                }
                gameObject2 = gameObject.transform.FindLoop("Label");
                if (gameObject2)
                {
                    fileInfoComponent.text = gameObject2.GetComponent<Text>();
                }
                fileInfoComponent.tgl.group = component;
                gameObject.transform.SetParent(__instance.objListTop.transform, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = new Vector3(1f, 1f, 1f);
                rectTransform.anchoredPosition = new Vector3(0f, (float)(-24.0 * num2), 0f);
                Text component2 = rectTransform.FindChild("Label").GetComponent<Text>();
                component2.text = fileInfoComponent.info.comment;
                __instance.CallPrivateExplicit<SmClothesLoad>("SetButtonClickHandler", gameObject);
                gameObject.SetActive(true);
                SmClothesLoad_Data._rectTransforms.Add(rectTransform);
                num2++;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
            SmClothesLoad_Data._created = true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnSortDate")]
    public class SmClothesLoad_OnSortDate_Patches
    {
        public static void Postfix(SmClothesLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnSortName")]
    public class SmClothesLoad_OnSortName_Patches
    {
        public static void Postfix(SmClothesLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortDate", new []{typeof(bool)})]
    public class SmClothesLoad_SortDate_Patches
    {
        public static void Prefix(SmClothesLoad __instance, bool ascend)
        {
            __instance.SetPrivateExplicit<SmClothesLoad>("ascendDate", ascend);
            if (SmClothesLoad_Data._fileInfos.Count == 0)
                return;
            __instance.SetPrivateExplicit<SmClothesLoad>("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                SmClothesLoad_Data._fileInfos.Sort((a, b) => a.time.CompareTo(b.time));
            else
                SmClothesLoad_Data._fileInfos.Sort((a, b) => b.time.CompareTo(a.time));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (SmClothesLoad.FileInfo fi in SmClothesLoad_Data._fileInfos)
            {
                fi.no = i;
                ++i;
            }
            foreach (SmClothesLoad.FileInfo fi in SmClothesLoad_Data._fileInfos)
            {
                foreach (RectTransform rt in SmClothesLoad_Data._rectTransforms)
                {
                    SmClothesLoad.FileInfoComponent component = rt.GetComponent<SmClothesLoad.FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        break;
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortName", new[] { typeof(bool) })]
    public class SmClothesLoad_SortName_Patches
    {
        public static void Prefix(SmClothesLoad __instance, bool ascend)
        {
            __instance.SetPrivateExplicit<SmClothesLoad>("ascendName", ascend);
            if (SmClothesLoad_Data._fileInfos.Count == 0)
                return;
            __instance.SetPrivateExplicit<SmClothesLoad>("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                SmClothesLoad_Data._fileInfos.Sort((a, b) => a.comment.CompareTo(b.comment));
            else
                SmClothesLoad_Data._fileInfos.Sort((a, b) => b.comment.CompareTo(a.comment));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (SmClothesLoad.FileInfo fi in SmClothesLoad_Data._fileInfos)
            {
                fi.no = i;
                ++i;
            }
            foreach (SmClothesLoad.FileInfo fi in SmClothesLoad_Data._fileInfos)
            {
                foreach (RectTransform rt in SmClothesLoad_Data._rectTransforms)
                {
                    SmClothesLoad.FileInfoComponent component = rt.GetComponent<SmClothesLoad.FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        break;
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

}
