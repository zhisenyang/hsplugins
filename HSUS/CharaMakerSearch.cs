#if KOIKATSU
using System;
using System.Collections.Generic;
using ChaCustom;
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
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
}
#endif
