using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using System.Threading;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
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
        public static List<OneTimeVerticalLayoutGroup> groups = new List<OneTimeVerticalLayoutGroup>();

        private static SmClothesLoad _originalComponent;
        public static void Init(SmClothesLoad originalComponent)
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

            RectTransform rt = _originalComponent.transform.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);
            float newY = rt.offsetMax.y;
            rt = _originalComponent.transform.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);

            searchBar = UIUtility.CreateInputField("Search Bar", _originalComponent.transform);
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
            created = false;
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

        public static void Prefix(SmClothesLoad __instance)
        {
            if (__instance.imgPrev)
                __instance.imgPrev.enabled = false;
            if (SmClothesLoad_Data.created)
                return;
            List<RectTransform> lstRtfTgl = ((List<RectTransform>)__instance.GetPrivate("lstRtfTgl"));
            if (__instance.objListTop.transform.childCount > 0 && SmClothesLoad_Data.objects.ContainsValue(__instance.objListTop.transform.GetChild(0).gameObject) == false)
            {
                lstRtfTgl.Clear();
                for (int i = 0; i < __instance.objListTop.transform.childCount; i++)
                    GameObject.Destroy(__instance.objListTop.transform.GetChild(i).gameObject);
                
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
                    __instance.SetButtonClickHandler(gameObject);
                    gameObject.SetActive(true);
                    lstRtfTgl.Add(rectTransform);
                    num2++;
                }
            }
            else if (lstFileInfo.Count < SmClothesLoad_Data.objects.Count)
            {
                foreach (KeyValuePair<SmClothesLoad.FileInfo, GameObject> pair in new Dictionary<SmClothesLoad.FileInfo, GameObject>(SmClothesLoad_Data.objects))
                {
                    int i;
                    if ((i = lstFileInfo.IndexOf(pair.Key)) != -1)
                    {
                        lstRtfTgl.Remove(pair.Value.transform as RectTransform);
                        GameObject.Destroy(pair.Value);
                        SmClothesLoad_Data.objects.Remove(pair.Key);
                    }
                }
            }
            SmClothesLoad_Data.UpdateAllGroups();
            if (SmClothesLoad_Data.searchBar != null)
            {
                SmClothesLoad_Data.searchBar.text = "";
                SmClothesLoad_Data.SearchChanged("");
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.rtfPanel);
            SmClothesLoad_Data.created = true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
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

        public static void Prefix(SmClothesLoad __instance, bool ascend)
        {
            __instance.SetPrivateExplicit<SmClothesLoad>("ascendDate", ascend);
            List<SmClothesLoad.FileInfo> lstFileInfo = (List<SmClothesLoad.FileInfo>)__instance.GetPrivate("lstFileInfo");
            if (lstFileInfo.Count == 0)
                return;
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
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }

    [HarmonyPatch(typeof(SmClothesLoad), "SortName", new[] { typeof(bool) })]
    public class SmClothesLoad_SortName_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeCharaMaker;
        }

        public static void Prefix(SmClothesLoad __instance, bool ascend)
        {
            __instance.SetPrivate("ascendName", ascend);
            List<SmClothesLoad.FileInfo> lstFileInfo = (List<SmClothesLoad.FileInfo>)__instance.GetPrivate("lstFileInfo");
            if (lstFileInfo.Count == 0)
                return;
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
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
