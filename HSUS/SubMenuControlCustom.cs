using System;
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
                if (item != null && !((UnityEngine.Object)null == (UnityEngine.Object)item.objTop))
                {
                    item.objTop.SetActive(false);
                }
            }
            if (this.smItem[this.nowSubMenuTypeId] != null)
            {
                if ((UnityEngine.Object)null != (UnityEngine.Object)this.textTitle)
                    this.textTitle.text = this.smItem[this.nowSubMenuTypeId].menuName;
                if ((UnityEngine.Object)null != (UnityEngine.Object)this.smItem[this.nowSubMenuTypeId].objTop)
                {
                    this.smItem[this.nowSubMenuTypeId].objTop.SetActive(true);
                    this.SetPrivateExplicit<SubMenuControl>("objActiveSubItem", this.smItem[this.nowSubMenuTypeId].objTop);
                    if ((UnityEngine.Object)null != (UnityEngine.Object)this.rtfBasePanel)
                    {
                        RectTransform rectTransform = this.smItem[this.nowSubMenuTypeId].objTop.transform as RectTransform;
                        Vector2 sizeDelta = rectTransform.sizeDelta;
                        this.SetPrivateExplicit<SubMenuControl>("sizeBasePanelHeight", sizeDelta.y);
                    }
                }
            }

            SmClothes_FCustom compCutsom = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmClothes_FCustom>();
            SmCharaLoadCustom compCustom2;
            SmAccessoryCustom compCustom3;
            SmHair_FCustom compCustom4;
            SmKindColorDCustom compCustom5;
            SmKindColorDSCustom compCustom6;
            SmFaceSkinCustom compCustom7;
            SmSwimsuitCustom compCustom8;
            SmClothesLoadCustom compCustom9;
            if (compCutsom != null)
                compCutsom.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom2 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmCharaLoadCustom>()) != null)
                compCustom2.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
            else if ((compCustom3 = ((GameObject)this.GetPrivateExplicit<SubMenuControl>("objActiveSubItem")).GetComponent<SmAccessoryCustom>()) != null)
                compCustom3.SetCharaInfo(this.nowSubMenuTypeId, sameSubMenu);
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
                if (this.nowSubMenuTypeId == (int)SubMenuType.SM_BodyNip)
                foreach (Toggle t in GameObject.Find("CustomScene").transform.FindChild("CustomControl/CustomUI/CustomSubMenu/W_SubMenu/SubItemTop/BodyNip/TabControl/TabItem01/ScrollView/TypeControlPanel/ListTop").GetComponentsInChildren<Toggle>())
                {
                    Toggle t1 = t;
                    t.onValueChanged.AddListener(b =>
                    {
                        MatTypeInfo info = t1.GetComponent<MatTypeInfo>();
                        if (t1.isOn == true)
                            UnityEngine.Debug.Log(info.info.Id + " " + info.info.ABPath);
                    });
                }
                
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
