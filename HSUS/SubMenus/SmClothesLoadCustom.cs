#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class SmClothesLoad_Data
    {
        public static bool created;
        public static RectTransform container;
        public static readonly Dictionary<SmClothesLoad.FileInfo, GameObject> objects = new Dictionary<SmClothesLoad.FileInfo, GameObject>();
        public static InputField searchBar;
        internal static IEnumerator _createListObject;
        internal static PropertyInfo _translateProperty = null;

        private static SmClothesLoad _originalComponent;
        public static void Init(SmClothesLoad originalComponent)
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

            searchBar = CharaMakerSearch.SpawnSearchBar(_originalComponent.transform, SearchChanged, -24f);

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (HSUS._self._asyncLoading)
            {
                _createListObject = SmClothesLoad_CreateListObject_Patches.CreateListObject(originalComponent);
                HSUS._self._asyncMethods.Add(_createListObject);
            }
        }

        private static void Reset()
        {
            objects.Clear();
            created = false;
            _createListObject = null;
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            if (objects != null && objects.Count != 0)
                foreach (SmClothesLoad.FileInfo fi in (List<SmClothesLoad.FileInfo>)_originalComponent.GetPrivate("lstFileInfo"))
                {
                    GameObject go;
                    if (objects.TryGetValue(fi, out go))
                        go.SetActive(fi.comment.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                }
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "CreateListObject")]
    public class SmClothesLoad_CreateListObject_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmClothesLoad __instance)
        {
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;
            if (SmClothesLoad_Data.created == false)
            {
                if (SmClothesLoad_Data._createListObject == null || SmClothesLoad_Data._createListObject.MoveNext() == false)
                    SmClothesLoad_Data._createListObject = CreateListObject(__instance);

                while (SmClothesLoad_Data._createListObject.MoveNext());

                SmClothesLoad_Data._createListObject = null;
            }

            if (SmClothesLoad_Data.searchBar != null)
            {
                SmClothesLoad_Data.searchBar.text = "";
                //SmClothesLoad_Data.SearchChanged("");
            }

            return false;
        }

        internal static IEnumerator CreateListObject(SmClothesLoad __instance)
        {
            List<RectTransform> lstRtfTgl = ((List<RectTransform>)__instance.GetPrivate("lstRtfTgl"));
            if (__instance.objListTop.transform.childCount > 0 && SmClothesLoad_Data.objects.ContainsValue(__instance.objListTop.transform.GetChild(0).gameObject) == false)
            {
                lstRtfTgl.Clear();
                for (int i = 0; i < __instance.objListTop.transform.childCount; i++)
                {
                    GameObject go = __instance.objListTop.transform.GetChild(i).gameObject;
                    GameObject.Destroy(go);

                }
                yield return null;
            }
            List<SmClothesLoad.FileInfo> lstFileInfo = (List<SmClothesLoad.FileInfo>)__instance.GetPrivate("lstFileInfo");
            ToggleGroup component = __instance.objListTop.GetComponent<ToggleGroup>();
            int num2 = 0;
            if (lstFileInfo.Count > SmClothesLoad_Data.objects.Count)
            {
                foreach (SmClothesLoad.FileInfo fi in lstFileInfo)
                {
                    if (SmClothesLoad_Data.objects.ContainsKey(fi))
                        continue;
                    GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                    SmClothesLoad_Data.objects.Add(fi, gameObject);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    SmClothesLoad.FileInfoComponent fileInfoComponent = gameObject.AddComponent<SmClothesLoad.FileInfoComponent>();
                    fileInfoComponent.info = fi;
                    GameObject gameObject2 = null;
                    fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                    gameObject2 = gameObject.transform.FindLoop("Background");
                    if (gameObject2)
                        fileInfoComponent.imgBG = gameObject2.GetComponent<Image>();
                    gameObject2 = gameObject.transform.FindLoop("Checkmark");
                    if (gameObject2)
                        fileInfoComponent.imgCheck = gameObject2.GetComponent<Image>();
                    gameObject2 = gameObject.transform.FindLoop("Label");
                    if (gameObject2)
                        fileInfoComponent.text = gameObject2.GetComponent<Text>();
                    fileInfoComponent.tgl.group = component;
                    if (HSUS._self._asyncLoading && SmClothesLoad_Data._translateProperty != null) // Fuck you translation plugin
                    {
                        SmClothesLoad_Data._translateProperty.SetValue(fileInfoComponent.text, false, null);
                        string t = fileInfoComponent.info.comment;
                        HSUS._self._translationMethod(ref t);
                        fileInfoComponent.text.text = t;
                    }
                    else
                        fileInfoComponent.text.text = fileInfoComponent.info.comment;
                    __instance.CallPrivate("SetButtonClickHandler", gameObject);
                    gameObject.SetActive(true);
                    gameObject.transform.SetParent(__instance.objListTop.transform, false);
                    RectTransform rectTransform = (RectTransform)gameObject.transform;
                    rectTransform.localScale = Vector3.one;
                    lstRtfTgl.Add(rectTransform);
                    num2++;
                    yield return null;
                } 
            }
            else if (lstFileInfo.Count < SmClothesLoad_Data.objects.Count)
            {
                foreach (KeyValuePair<SmClothesLoad.FileInfo, GameObject> pair in new Dictionary<SmClothesLoad.FileInfo, GameObject>(SmClothesLoad_Data.objects))
                {
                    if (lstFileInfo.IndexOf(pair.Key) != -1)
                        continue;
                    lstRtfTgl.Remove(pair.Value.transform as RectTransform);
                    GameObject.Destroy(pair.Value);
                    SmClothesLoad_Data.objects.Remove(pair.Key);
                    yield return null;
                }
            }
            SmClothesLoad_Data.created = true;
            LayoutRebuilder.MarkLayoutForRebuild(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "ExecuteSaveNew")]
    public class SmClothesLoad_ExecuteSaveNew_Patches
    {
        public static void Prefix()
        {
            SmClothesLoad_Data.created = false;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "ExecuteDelete")]
    public class SmClothesLoad_ExecuteDelete_Patches
    {
        public static void Prefix()
        {
            SmClothesLoad_Data.created = false;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnSortDate")]
    public class SmClothesLoad_OnSortDate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Postfix(SmClothesLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "OnSortName")]
    public class SmClothesLoad_OnSortName_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Postfix(SmClothesLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortDate", new []{typeof(bool)})]
    public class SmClothesLoad_SortDate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmClothesLoad __instance, bool ascend)
        {
            __instance.SetPrivateExplicit<SmClothesLoad>("ascendDate", ascend);
            List<SmClothesLoad.FileInfo> lstFileInfo = (List<SmClothesLoad.FileInfo>)__instance.GetPrivate("lstFileInfo");
            if (lstFileInfo.Count == 0)
                return false;
            __instance.SetPrivateExplicit<SmClothesLoad>("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                lstFileInfo.Sort((a, b) => a.time.CompareTo(b.time));
            else
                lstFileInfo.Sort((a, b) => b.time.CompareTo(a.time));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (SmClothesLoad.FileInfo fi in lstFileInfo)
            {
                fi.no = i;
                GameObject go;
                if (SmClothesLoad_Data.objects.TryGetValue(fi, out go))
                    go.transform.SetAsLastSibling();
                ++i;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortName", new[] { typeof(bool) })]
    public class SmClothesLoad_SortName_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmClothesLoad __instance, bool ascend)
        {
            __instance.SetPrivate("ascendName", ascend);
            List<SmClothesLoad.FileInfo> lstFileInfo = (List<SmClothesLoad.FileInfo>)__instance.GetPrivate("lstFileInfo");
            if (lstFileInfo.Count == 0)
                return false;
            __instance.SetPrivate("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                lstFileInfo.Sort((a, b) => a.comment.CompareTo(b.comment));
            else
                lstFileInfo.Sort((a, b) => b.comment.CompareTo(a.comment));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (SmClothesLoad.FileInfo fi in lstFileInfo)
            {
                fi.no = i;
                GameObject go;
                if (SmClothesLoad_Data.objects.TryGetValue(fi, out go))
                    go.transform.SetAsLastSibling();
                ++i;
            }
            return false;
        }
    }
}

#endif