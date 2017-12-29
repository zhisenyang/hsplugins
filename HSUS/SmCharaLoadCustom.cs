using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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
    public static class SmCharaLoad_Data
    {
        public static readonly Dictionary<SmCharaLoad.FileInfo, GameObject> objects = new Dictionary<SmCharaLoad.FileInfo, GameObject>();
        public static int lastMenuType;
        public static bool created;
        public static RectTransform container;
        public static List<SmCharaLoad.FileInfo> fileInfos;
        public static List<RectTransform> rectTransforms;
        public static InputField searchBar;

        private static SmCharaLoad _originalComponent;

        public static void Init(SmCharaLoad originalComponent)
        {
            _originalComponent = originalComponent;
            lastMenuType = (int)_originalComponent.GetPrivate("nowSubMenuTypeId");
            fileInfos = (List<SmCharaLoad.FileInfo>)_originalComponent.GetPrivateExplicit<SmCharaLoad>("lstFileInfo");
            rectTransforms = ((List<RectTransform>)_originalComponent.GetPrivateExplicit<SmCharaLoad>("lstRtfTgl"));

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

            RectTransform rt = _originalComponent.transform.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);
            float newY = rt.offsetMax.y;
            rt = _originalComponent.transform.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);

            searchBar = UIUtility.CreateInputField("Search Bar", _originalComponent.transform);
            rt = searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            searchBar.placeholder.GetComponent<Text>().text = "Search...";
            searchBar.onValueChanged.AddListener(SearchChanged);
            foreach (Text te in searchBar.GetComponentsInChildren<Text>())
                te.color = Color.white;
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

        public static void Prefix(SmCharaLoad __instance)
        {
            SmCharaLoad_Data.searchBar.text = "";
            SmCharaLoad_Data.SearchChanged("");
            int nowSubMenuTypeId = (int)__instance.GetPrivate("nowSubMenuTypeId");
            if (SmCharaLoad_Data.lastMenuType == nowSubMenuTypeId)
                return;
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;
            ToggleGroup component = __instance.objListTop.GetComponent<ToggleGroup>();
            if (SmCharaLoad_Data.created == false)
            {
                foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
                {
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
                    Text component2 = gameObject.transform.FindChild("Label").GetComponent<Text>();
                    component2.text = fileInfoComponent.info.CharaName;
                    __instance.CallPrivateExplicit<SmCharaLoad>("SetButtonClickHandler", gameObject);
                    gameObject.SetActive(true);
                    SmCharaLoad_Data.rectTransforms.Add(gameObject.transform as RectTransform);
                }
                SmCharaLoad_Data.created = true;
            }
            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
                SmCharaLoad_Data.objects[fi].SetActive(!fi.noAccess);
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
            SmCharaLoad_Data.lastMenuType = nowSubMenuTypeId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnSortName")]
    public class SmCharaLoad_OnSortName_Patches
    {
        public static void Postfix(SmCharaLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }

    [HarmonyPatch(typeof(SmCharaLoad), "OnSortDate")]
    public class SmCharaLoad_OnSortDate_Patches
    {
        public static void Postfix(SmCharaLoad __instance)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
        }
    }


    [HarmonyPatch(typeof(SmCharaLoad), "SortName", new []{typeof(bool)})]
    public class SmCharaLoad_SortName_Patches
    {
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
            }
            int num = 0;
            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
            {
                foreach (RectTransform rt in SmCharaLoad_Data.rectTransforms)
                {
                    if (rt.gameObject.activeSelf == false)
                        continue;
                    SmCharaLoad.FileInfoComponent component = rt.GetComponent<SmCharaLoad.FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        num++;
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

    [HarmonyPatch(typeof(SmCharaLoad), "SortDate", new[] { typeof(bool) })]
    public class SmCharaLoad_SortDate_Patches
    {
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
            }
            int num = 0;
            foreach (SmCharaLoad.FileInfo fi in SmCharaLoad_Data.fileInfos)
            {
                foreach (RectTransform rt in SmCharaLoad_Data.rectTransforms)
                {
                    if (rt.gameObject.activeSelf == false)
                        continue;
                    SmCharaLoad.FileInfoComponent component = rt.GetComponent<SmCharaLoad.FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        num++;
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