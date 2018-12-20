using System;
using System.Collections.Generic;
#if KOIKATSU
using ChaCustom;
#endif
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HSUS
{
#if HONEYSELECT

    internal static class CharaMakerSearch
    {
        internal static InputField SpawnSearchBar(Transform parent, UnityAction<string> listener, float parentShift = -16f)
        {
            RectTransform rt = parent.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, parentShift);
            float newY = rt.offsetMax.y;
            rt = parent.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, parentShift);

            InputField searchBar = UIUtility.CreateInputField("Search Bar", parent);
            searchBar.GetComponent<Image>().sprite = HSUS.self.searchBarBackground;
            rt = searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            searchBar.placeholder.GetComponent<Text>().text = "Search...";
            searchBar.onValueChanged.AddListener(listener);
            foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
            return searchBar;
        }
    }

    internal static class CharaMakerSort
    {
        internal static void SpawnSortButtons(Transform parent, UnityAction sortByNameListener, UnityAction sortByCreationDateListener, UnityAction resetListener)
        {
            RectTransform rt = parent.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -20f);
            float newY = rt.offsetMax.y;
            rt = parent.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -20f);

            RectTransform container = UIUtility.CreateNewUIObject(parent, "SortContainer");
            container.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(4f, newY), new Vector2(-4f, newY + 20f));

            Text label = UIUtility.CreateText("Label", container, "Sort");
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.25f, 1f));

            Button sortName = UIUtility.CreateButton("Name", container, "名前");
            sortName.transform.SetRect(new Vector2(0.25f, 0f), new Vector2(0.5f, 1f));
            sortName.GetComponentInChildren<Text>().rectTransform.SetRect();
            ((Image)sortName.targetGraphic).sprite = HSUS._self._buttonBackground;
            sortName.onClick.AddListener(sortByNameListener);

            Button sortCreationDate = UIUtility.CreateButton("CreationDate", container, "日付");
            sortCreationDate.transform.SetRect(new Vector2(0.5f, 0f), new Vector2(0.75f, 1f));
            sortCreationDate.GetComponentInChildren<Text>().rectTransform.SetRect();
            ((Image)sortCreationDate.targetGraphic).sprite = HSUS._self._buttonBackground;
            sortCreationDate.onClick.AddListener(sortByCreationDateListener);

            Button sortOriginal = UIUtility.CreateButton("Original", container, "リセット");
            sortOriginal.transform.SetRect(new Vector2(0.75f, 0f), Vector2.one);
            sortOriginal.GetComponentInChildren<Text>().rectTransform.SetRect();
            ((Image)sortOriginal.targetGraphic).sprite = HSUS._self._buttonBackground;
            sortOriginal.onClick.AddListener(resetListener);
        }

        internal static void GenericIntSort<T>(List<T> list, Func<T, int> getIntFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? getIntFunc(y).CompareTo(getIntFunc(x)) : getIntFunc(x).CompareTo(getIntFunc(y)));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }

        internal static void GenericStringSort<T>(List<T> list, Func<T, string> getStringFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? string.Compare(getStringFunc(y), getStringFunc(x), StringComparison.CurrentCultureIgnoreCase) : string.Compare(getStringFunc(x), getStringFunc(y), StringComparison.CurrentCultureIgnoreCase));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }

        internal static void GenericDateSort<T>(List<T> list, Func<T, DateTime> getDateFunc, Func<T, GameObject> getGameObjectFunc, bool reverse = false)
        {
            list.Sort((x, y) => reverse ? getDateFunc(y).CompareTo(getDateFunc(x)) : getDateFunc(x).CompareTo(getDateFunc(y)));
            foreach (T elem in list)
                getGameObjectFunc(elem).transform.SetAsLastSibling();
        }
    }

#elif KOIKATSU
    [HarmonyPatch(typeof(CustomFileWindow), "Awake")]
    internal class CustomFileWindow_Awake_Patches
    {
        private static bool Prepare()
        {
            return HSUS._self._optimizeCharaMaker;
        }

        private static void Postfix(CustomFileWindow __instance)
        {
            RectTransform searchGroup = UIUtility.CreateNewUIObject(__instance.transform.Find("WinRect"), "Search Group");
            searchGroup.transform.SetSiblingIndex(1);
            searchGroup.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
            __instance.transform.Find("WinRect/ListArea").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 32f);

            InputField searchBar = UIUtility.CreateInputField("Search Bar", searchGroup, "Search...");
            searchBar.transform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f));
            ((Image)searchBar.targetGraphic).sprite = __instance.transform.Find("WinRect/imgBack/imgName").GetComponent<Image>().sprite;
            foreach (Text text in searchBar.GetComponentsInChildren<Text>())
            {
                text.color = Color.white;
            }

            Button clearButton = UIUtility.CreateButton("Clear", searchGroup, "X");
            clearButton.transform.SetRect(new Vector2(1f, 0f), Vector2.one, new Vector2(-28f, 0f), Vector2.zero);
            clearButton.GetComponentInChildren<Text>().color = Color.black;
            clearButton.image.sprite = __instance.transform.Find("WinRect/imgBack/btnSortName").GetComponent<Image>().sprite;

            CustomFileListCtrl listCtrl = __instance.GetComponent<CustomFileListCtrl>();
            List<CustomFileInfo> items = (List<CustomFileInfo>)listCtrl.GetPrivate("lstFileInfo");

            searchBar.onValueChanged.AddListener(s =>
            {
                UpdateSearch(searchBar.text, items);
            });

            clearButton.onClick.AddListener(() =>
            {
                searchBar.text = "";
                UpdateSearch("", items);
            });
        }

        private static void UpdateSearch(string text, List<CustomFileInfo> items)
        {
            foreach (CustomFileInfo info in items)
                info.fic.Disvisible(info.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) == -1);
        }
    }
#endif
}

