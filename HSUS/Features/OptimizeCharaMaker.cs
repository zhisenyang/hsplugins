using System.IO;
#if HONEYSELECT
using CustomMenu;
#elif KOIKATSU
using System;
using System.Collections.Generic;
using ChaCustom;
using Harmony;
using UILib;
#endif
using ToolBox;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
#if HONEYSELECT
    public static class OptimizeCharaMaker
    {
        public static void Do(int level)
        {
            if (level != 21)
                return;
            GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu/SubItemTop/Infomation/TabControl/TabItem01/Name/InputField").GetComponent<InputField>().characterLimit = 0;
            GameObject.Find("CustomScene/CustomControl/CustomUI/CustomCheck/checkPng/checkInputName/InputField").GetComponent<InputField>().characterLimit = 0;

            HSUS._self._routines.ExecuteDelayed(() =>
            {

                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    switch (sprite.name)
                    {
                        case "rect_middle":
                            HSUS._self._searchBarBackground = sprite;
                            break;
                        case "btn_01":
                            HSUS._self._buttonBackground = sprite;
                            break;
                    }
                }
                foreach (Mask mask in Resources.FindObjectsOfTypeAll<Mask>()) //Thank you Henk for this tip
                {
                    mask.gameObject.AddComponent<RectMask2D>();
                    Object.DestroyImmediate(mask);
                }
                foreach (SmClothesLoad f in Resources.FindObjectsOfTypeAll<SmClothesLoad>())
                {
                    SmClothesLoad_Data.Init(f);
                    break;
                }
                foreach (SmCharaLoad f in Resources.FindObjectsOfTypeAll<SmCharaLoad>())
                {
                    SmCharaLoad_Data.Init(f);
                    break;
                }
                foreach (SmClothes_F f in Resources.FindObjectsOfTypeAll<SmClothes_F>())
                {
                    SmClothes_F_Data.Init(f);
                    break;
                }
                foreach (SmAccessory f in Resources.FindObjectsOfTypeAll<SmAccessory>())
                {
                    SmAccessory_Data.Init(f);
                    break;
                }
                foreach (SmSwimsuit f in Resources.FindObjectsOfTypeAll<SmSwimsuit>())
                {
                    SmSwimsuit_Data.Init(f);
                    break;
                }
                foreach (SmHair_F f in Resources.FindObjectsOfTypeAll<SmHair_F>())
                {
                    SmHair_F_Data.Init(f);
                    break;
                }
                foreach (SmKindColorD f in Resources.FindObjectsOfTypeAll<SmKindColorD>())
                {
                    SmKindColorD_Data.Init(f);
                    break;
                }
                foreach (SmKindColorDS f in Resources.FindObjectsOfTypeAll<SmKindColorDS>())
                {
                    SmKindColorDS_Data.Init(f);
                    break;
                }
                foreach (SmFaceSkin f in Resources.FindObjectsOfTypeAll<SmFaceSkin>())
                {
                    SmFaceSkin_Data.Init(f);
                    break;
                }
            }, 10);
        }
    }
#elif KOIKATSU
    [HarmonyPatch(typeof(CustomFileWindow), "Start")]
    internal class CustomFileWindow_Start_Patches
    {
        private static bool Prepare()
        {
            return HSUS._self._optimizeCharaMaker;
        }

        private static void Prefix(CustomFileWindow __instance)
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
            clearButton.image.sprite = __instance.transform.FindDescendant("btnLoad").GetComponent<Image>().sprite;

            CustomFileListCtrl listCtrl = __instance.GetComponent<CustomFileListCtrl>();
            List<CustomFileInfo> items = null;
            try
            {
                items = (List<CustomFileInfo>)listCtrl.GetPrivate("lstFileInfo");
            }
            catch
            {
                items = (List<CustomFileInfo>)listCtrl.GetPrivateProperty("lstFileInfo");
            }

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

