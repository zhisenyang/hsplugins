using System;
using HSUS;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SubMenuControlCustom : SubMenuControl
    {
        public void LoadFrom(SubMenuControl other)
        {
            this.LoadWith(other);
            this.ReplaceEventsOf(other);

            foreach (CustomControl customControl in Resources.FindObjectsOfTypeAll<CustomControl>())
                if (customControl.subMenuCtrl == other)
                    customControl.subMenuCtrl = this;

            foreach (CustomCtrlPanel customControl in Resources.FindObjectsOfTypeAll<CustomCtrlPanel>())
                if (customControl.subMenuCtrl == other)
                    customControl.subMenuCtrl = this;

            foreach (PhotoCtrlPanel customControl in Resources.FindObjectsOfTypeAll<PhotoCtrlPanel>())
                if (customControl.subMenuCtrl == other)
                    customControl.subMenuCtrl = this;
            GameObject.Find("CustomScene").transform.FindChild("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_System/SaveDelete").GetComponent<Button>().onClick.AddListener(() => this.ChangeSubMenu(SubMenuType.SM_ClothesSave.ToString()));
            GameObject.Find("CustomScene").transform.FindChild("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootCustom/TT_Preset/SamplePreset").GetComponent<Button>().onClick.AddListener(() => this.ChangeSubMenu(SubMenuType.SM_SamplePresets.ToString()));
        }

        public new virtual void ChangeSubMenu(string subMenuStr)
        {
            bool sameSubMenu = this.nowSubMenuTypeStr == subMenuStr;
            this.nowSubMenuTypeStr = subMenuStr;
            this.nowSubMenuTypeId = (int)Enum.Parse(typeof(SubMenuType), subMenuStr);
            foreach (SubMenuItem item in this.smItem)
            {
                if (item != null && !(null == item.objTop))
                {
                    item.objTop.SetActive(false);
                }
            }
            if (this.smItem[this.nowSubMenuTypeId] != null)
            {
                if (null != this.textTitle)
                    this.textTitle.text = this.smItem[this.nowSubMenuTypeId].menuName;
                if (null != this.smItem[this.nowSubMenuTypeId].objTop)
                {
                    this.smItem[this.nowSubMenuTypeId].objTop.SetActive(true);
                    this.SetPrivateExplicit<SubMenuControl>("objActiveSubItem", this.smItem[this.nowSubMenuTypeId].objTop);
                    if (null != this.rtfBasePanel)
                    {
                        RectTransform rectTransform = this.smItem[this.nowSubMenuTypeId].objTop.transform as RectTransform;
                        Vector2 sizeDelta = rectTransform.sizeDelta;
                        this.SetPrivateExplicit<SubMenuControl>("sizeBasePanelHeight", sizeDelta.y);
                    }
                }
            }

            SmCharaLoadCustom compCustom2;
            SmHair_FCustom compCustom4;
            SmKindColorDCustom compCustom5;
            SmKindColorDSCustom compCustom6;
            SmFaceSkinCustom compCustom7;
            SmSwimsuitCustom compCustom8;
            SmClothesLoadCustom compCustom9;
            if ((compCustom2 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmCharaLoadCustom>()) != null)
                compCustom2.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom4 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmHair_FCustom>()) != null)
                compCustom4.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom5 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmKindColorDCustom>()) != null)
                compCustom5.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom6 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmKindColorDSCustom>()) != null)
                compCustom6.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom7 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmFaceSkinCustom>()) != null)
                compCustom7.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom8 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmSwimsuitCustom>()) != null)
                compCustom8.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom9 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmClothesLoadCustom>()) != null)
                compCustom9.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else
            {
                SubMenuBase component = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SubMenuBase>();
                if (null != component)
                    component.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            }
            int cosStateFromSelect = this.GetCosStateFromSelect();
            if (cosStateFromSelect != -1 && this.customCtrlPanel && this.customCtrlPanel.autoClothesState)
            {
                this.customCtrlPanel.ChangeCosStateSub(cosStateFromSelect);
            }
        }

        public new virtual void UpdateLimitMainMenu()
        {
            SubMenuBase component = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SubMenuBase>();
            if (null != component)
            {
                component.UpdateLimitMainMenu();
            }
        }

        public new virtual void UpdateSubMenu()
        {
            SubMenuBase component = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SubMenuBase>();
            if (null != component)
            {
                component.UpdateCharaInfo();
            }
        }
    }
}
