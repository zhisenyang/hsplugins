#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
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
    public static class SmCharaLoad_Data
    {
        public static readonly Dictionary<SmCharaLoad.FileInfo, GameObject> objects = new Dictionary<SmCharaLoad.FileInfo, GameObject>();
        public static int lastMenuType;
        public static bool created;
        public static RectTransform container;
        public static List<SmCharaLoad.FileInfo> fileInfos;
        public static List<RectTransform> rectTransforms;
        public static InputField searchBar;
        internal static IEnumerator _createListObject;
        internal static PropertyInfo _translateProperty = null;

        private static SmCharaLoad _originalComponent;

        public static void Init(SmCharaLoad originalComponent)
        {
            Reset();

            _originalComponent = originalComponent;
            lastMenuType = (int)_originalComponent.GetPrivate("nowSubMenuTypeId");
            fileInfos = (List<SmCharaLoad.FileInfo>)_originalComponent.GetPrivate("lstFileInfo");
            rectTransforms = ((List<RectTransform>)_originalComponent.GetPrivate("lstRtfTgl"));

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

            //Type t = Type.GetType("AdditionalBoneModifier.BoneControllerMgr,AdditionalBoneModifier");
            //if (t != null)
            //    t.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null).SetPrivate("loadSubMenu", this);

            searchBar = CharaMakerSearch.SpawnSearchBar(_originalComponent.transform, SearchChanged, -24f);

            _translateProperty = typeof(Text).GetProperty("Translate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (HSUS._self._asyncLoading)
            {
                originalComponent.SetPrivate("chaInfo", originalComponent.customControl.chainfo);
                originalComponent.Init();
                _createListObject = SmCharaLoad_CreateListObject_Patches.CreateListObject(originalComponent);
                HSUS._self._asyncMethods.Add(_createListObject);
            }
        }
        private static void Reset()
        {
            objects.Clear();
            created = false;
            fileInfos = null;
            rectTransforms = null;
            _createListObject = null;
        }

        public static void SearchChanged(string arg0)
        {
            string search = searchBar.text.Trim();
            foreach (SmCharaLoad.FileInfo fi in fileInfos)
            {
                GameObject obj;
                if (objects.TryGetValue(fi, out obj) == false)
                    continue;
                if (fi.noAccess == false)
                    obj.SetActive(fi.CharaName.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                else
                    obj.SetActive(false);
            }
        }

    }

    [HarmonyPatch(typeof(SmCharaLoad), "CreateListObject")]
    public class SmCharaLoad_CreateListObject_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static bool Prefix(SmCharaLoad __instance)
        {
            SmCharaLoad_Data.searchBar.text = "";
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (SmCharaLoad_Data.lastMenuType == nowSubMenuTypeId && SmCharaLoad_Data.created)
                return false;
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;

            if (SmCharaLoad_Data._createListObject == null || SmCharaLoad_Data._createListObject.MoveNext() == false)
                SmCharaLoad_Data._createListObject = CreateListObject(__instance);

            while (SmCharaLoad_Data._createListObject.MoveNext()) ;

            SmCharaLoad_Data._createListObject = null;

            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
                SmCharaLoad_Data.objects[fi].SetActive(!fi.noAccess);
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
            SmCharaLoad_Data.lastMenuType = nowSubMenuTypeId;
            return false;
        }

        internal static IEnumerator CreateListObject(SmCharaLoad __instance)
        {
            if (SmCharaLoad_Data.created == false)
            {
                ToggleGroup component = __instance.objListTop.GetComponent<ToggleGroup>();

                if (SmCharaLoad_Data.fileInfos.Count > SmCharaLoad_Data.objects.Count)
                {
                    foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
                    {
                        if (SmCharaLoad_Data.objects.ContainsKey(fi))
                            continue;
                        GameObject gameObject = GameObject.Instantiate(__instance.objLineBase);
                        SmCharaLoad_Data.objects.Add(fi, gameObject);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        SmCharaLoad.FileInfoComponent fileInfoComponent = gameObject.AddComponent<SmCharaLoad.FileInfoComponent>();
                        fileInfoComponent.info = fi;
                        fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                        GameObject gameObject2 = gameObject.transform.FindLoop("Background");
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
                        gameObject.transform.localScale = Vector3.one;
                        (gameObject.transform as RectTransform).sizeDelta = new Vector2(SmCharaLoad_Data.container.rect.width, 24f);
                        if (HSUS._self._asyncLoading && SmCharaLoad_Data._translateProperty != null) // Fuck you translation plugin
                        {
                            SmCharaLoad_Data._translateProperty.SetValue(fileInfoComponent.text, false, null);
                            string t = fileInfoComponent.info.CharaName;
                            HSUS._self._translationMethod(ref t);
                            fileInfoComponent.text.text = t;
                        }
                        else
                            fileInfoComponent.text.text = fileInfoComponent.info.CharaName;
                        __instance.CallPrivate("SetButtonClickHandler", gameObject);
                        gameObject.SetActive(true);
                        SmCharaLoad_Data.rectTransforms.Add(gameObject.transform as RectTransform);
                        yield return null;
                    }
                }
                else if (SmCharaLoad_Data.fileInfos.Count < SmCharaLoad_Data.objects.Count)
                {
                    foreach (KeyValuePair<SmCharaLoad.FileInfo, GameObject> pair in new Dictionary<SmCharaLoad.FileInfo, GameObject>(SmCharaLoad_Data.objects))
                    {
                        if (SmCharaLoad_Data.fileInfos.Contains(pair.Key) == false)
                        {
                            SmCharaLoad_Data.rectTransforms.Remove(pair.Value.transform as RectTransform);
                            GameObject.Destroy(pair.Value);
                            SmCharaLoad_Data.objects.Remove(pair.Key);
                        }
                    }
                }
                SmCharaLoad_Data.created = true;
            }

        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "ExecuteSaveNew")]
    public class SmCharaLoad_ExecuteSaveNew_Patches
    {
        public static void Prefix()
        {
            SmCharaLoad_Data.created = false;
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "ExecuteDelete")]
    public class SmCharaLoad_ExecuteDelete_Patches
    {
        public static void Prefix()
        {
            SmCharaLoad_Data.created = false;
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnSortName")]
    public class SmCharaLoad_OnSortName_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Postfix(SmCharaLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnSortDate")]
    public class SmCharaLoad_OnSortDate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Postfix(SmCharaLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }


    [HarmonyPatch(typeof(SmCharaLoad), "SortName", new []{typeof(bool)})]
    public class SmCharaLoad_SortName_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmCharaLoad __instance, bool ascend)
        {
            __instance.SetPrivateExplicit<SmCharaLoad>("ascendName", ascend);
            if (SmCharaLoad_Data.fileInfos.Count == 0)
                return;
            __instance.SetPrivateExplicit<SmCharaLoad>("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                SmCharaLoad_Data.fileInfos.Sort((a, b) => a.CharaName.CompareTo(b.CharaName));
            else
                SmCharaLoad_Data.fileInfos.Sort((a, b) => b.CharaName.CompareTo(a.CharaName));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
            {
                fi.no = i;
                ++i;
                GameObject go;
                if (SmCharaLoad_Data.objects.TryGetValue(fi, out go))
                    go.transform.SetAsLastSibling();
            }
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "SortDate", new[] { typeof(bool) })]
    public class SmCharaLoad_SortDate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmCharaLoad __instance, bool ascend)
        {
            __instance.SetPrivateExplicit<SmCharaLoad>("ascendDate", ascend);
            if (SmCharaLoad_Data.fileInfos.Count == 0)
                return;
            __instance.SetPrivateExplicit<SmCharaLoad>("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                SmCharaLoad_Data.fileInfos.Sort((a, b) => a.time.CompareTo(b.time));
            else
                SmCharaLoad_Data.fileInfos.Sort((a, b) => b.time.CompareTo(a.time));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
            {
                fi.no = i;
                ++i;
                GameObject go;
                if (SmCharaLoad_Data.objects.TryGetValue(fi, out go))
                    go.transform.SetAsLastSibling();
            }
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
#endif