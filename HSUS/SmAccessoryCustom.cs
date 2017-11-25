using System;
using System.Collections;
using System.Collections.Generic;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmAccessoryCustom : SmAccessory
    {
        private class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        private readonly Dictionary<int, List<ObjectData>> _objects = new Dictionary<int, List<ObjectData>>();
        private int _lastType;
        private int _lastId;
        private RectTransform _container;
        private InputField _searchBar;

        public void LoadFrom(SmAccessory other)
        {
            this.LoadWith(other);
            this.ReplaceEventsOf(other);

            this._container = this.transform.FindDescendant("ListTop").transform as RectTransform;
            VerticalLayoutGroup group = this._container.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = this._container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.rtfPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            group = this.rtfPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            RectTransform rt = this.transform.FindChild("TabControl/TabItem01/ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);
            float newY = rt.offsetMax.y;
            rt = this.transform.FindChild("TabControl/TabItem01/Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);

            this._searchBar = UIUtility.CreateInputField("Search Bar", this.transform.FindChild("TabControl/TabItem01"));
            rt = this._searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            this._searchBar.placeholder.GetComponent<Text>().text = "Search...";
            this._searchBar.onValueChanged.AddListener(this.SearchChanged);
        }

        private void SearchChanged(string arg0)
        {
            string search = this._searchBar.text.Trim();
            if (this._objects.ContainsKey(this._lastType) == false)
                return;
            foreach (ObjectData objectData in this._objects[this._lastType])
            {
                bool active = objectData.obj.activeSelf;
                ToggleGroup group = objectData.toggle.group;
                objectData.obj.SetActive(objectData.text.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                if (active && objectData.obj.activeSelf == false)
                    group.RegisterToggle(objectData.toggle);
            }
        }

        public new virtual void SetCharaInfo(int smTypeId, bool sameSubMenu)
        {
            if (null == this.customControl)
                return;
            this.colorMenu = this.customControl.colorMenu;
            this.chaInfo = this.customControl.chainfo;
            if (null == this.chaInfo)
                return;
            this.chaBody = this.chaInfo.chaBody;
            this.chaCustom = this.chaInfo.chaCustom;
            this.chaClothes = this.chaInfo.chaClothes;
            this.customInfo = this.chaInfo.customInfo;
            this.clothesInfo = this.chaInfo.clothesInfo;
            this.coordinateInfo = this.chaInfo.chaFile.coordinateInfo;
            this.statusInfo = this.chaInfo.statusInfo;
            this.parameterInfo = this.chaInfo.parameterInfo;
            if (this.chaInfo.Sex == 0)
            {
                this.chaM = (this.chaInfo as CharMale);
                this.chaBodyM = (this.chaInfo.chaBody as CharMaleBody);
                this.chaCustomM = (this.chaCustom as CharMaleCustom);
                this.chaClothesM = (this.chaClothes as CharMaleClothes);
                this.customInfoM = (this.customInfo as CharFileInfoCustomMale);
                this.clothesInfoM = (this.clothesInfo as CharFileInfoClothesMale);
                this.coordinateInfoM = (this.coordinateInfo as CharFileInfoCoordinateMale);
                this.statusInfoM = (this.statusInfo as CharFileInfoStatusMale);
                this.parameterInfoM = (this.parameterInfo as CharFileInfoParameterMale);
            }
            else
            {
                this.chaF = (this.chaInfo as CharFemale);
                this.chaBodyF = (this.chaInfo.chaBody as CharFemaleBody);
                this.chaCustomF = (this.chaCustom as CharFemaleCustom);
                this.chaClothesF = (this.chaClothes as CharFemaleClothes);
                this.customInfoF = (this.customInfo as CharFileInfoCustomFemale);
                this.clothesInfoF = (this.clothesInfo as CharFileInfoClothesFemale);
                this.coordinateInfoF = (this.coordinateInfo as CharFileInfoCoordinateFemale);
                this.statusInfoF = (this.statusInfo as CharFileInfoStatusFemale);
                this.parameterInfoF = (this.parameterInfo as CharFileInfoParameterFemale);
            }
            this.defMale = this.customControl.defMaleSetting;
            if (null != this.defMale)
            {
                this.defCustomM = this.defMale.maleCustom;
                this.defClothesM = this.defMale.maleClothes;
                this.defCustomInfoM = this.defMale.maleCustomInfo;
                this.defClothesInfoM = this.defMale.maleClothesInfo;
            }
            this.defFemale = this.customControl.defFemaleSetting;
            if (null != this.defFemale)
            {
                this.defCustomF = this.defFemale.femaleCustom;
                this.defClothesF = this.defFemale.femaleClothes;
                this.defCustomInfoF = this.defFemale.femaleCustomInfo;
                this.defClothesInfoF = this.defFemale.femaleClothesInfo;
            }
            if (!sameSubMenu)
            {
                if (this.customControl.smClothesColorCtrlM)
                {
                    this.customControl.smClothesColorCtrlM.ReflectColorOff();
                }
                if (this.customControl.smClothesColorCtrlF)
                {
                    this.customControl.smClothesColorCtrlF.ReflectColorOff();
                }
            }
            this._searchBar.text = "";
            this.SearchChanged("");
            this.nowSubMenuTypeId = smTypeId;
            this.SetCharaInfoSub();
        }

        public override void SetCharaInfoSub()
        {
            if (!(bool)this.GetPrivateExplicit<SmAccessory>("initEnd"))
            {
                this.CallPrivateExplicit<SmAccessory>("Init");
            }
            if (null == this.chaInfo)
            {
                return;
            }
            this.SetPrivateExplicit<SmAccessory>("initFlags", true);
            if (null != this.tglTab)
            {
                this.tglTab.isOn = true;
            }
            int slotNoFromSubMenuSelect = (int)this.CallPrivateExplicit<SmAccessory>("GetSlotNoFromSubMenuSelect");
            int num = this.clothesInfo.accessory[slotNoFromSubMenuSelect].type + 1;
            this.SetPrivateExplicit<SmAccessory>("nowTglAllSet", true);
            foreach (Toggle t in (Toggle[])this.GetPrivateExplicit<SmAccessory>("tglType"))
            {
                t.isOn = false;
            }
            this.SetPrivateExplicit<SmAccessory>("nowTglAllSet", false);
            ((Toggle[])this.GetPrivateExplicit<SmAccessory>("tglType"))[num].isOn = true;
            this.ChangeAccessoryTypeList(this.clothesInfo.accessory[slotNoFromSubMenuSelect].type, this.clothesInfo.accessory[slotNoFromSubMenuSelect].id);
            this.CallPrivateExplicit<SmAccessory>("UpdateShowTab");
            this.MoveInfoAllSet();
            this.SetPrivateExplicit<SmAccessory>("initFlags", false);
        }

        public new virtual void OnChangeAccessoryType(int newType)
        {
            this._searchBar.text = "";
            this.SearchChanged("");
            int num = newType + 1;
            if ((bool)this.GetPrivateExplicit<SmAccessory>("initFlags"))
                return;
            if (null == this.chaInfo)
                return;
            if (null == this.grpType)
                return;
            Toggle toggle = ((Toggle[])this.GetPrivateExplicit<SmAccessory>("tglType"))[num];
            if (null == toggle)
                return;
            if (!toggle.isOn)
                return;
            int slotNoFromSubMenuSelect = (int)this.CallPrivateExplicit<SmAccessory>("GetSlotNoFromSubMenuSelect");
            if (this.clothesInfo.accessory[slotNoFromSubMenuSelect].type == newType)
                return;
            this.SetPrivateExplicit<SmAccessory>("nowLoading", true);
            this.ChangeAccessoryTypeList(newType, -1);
            this.SetPrivateExplicit<SmAccessory>("nowLoading", false);
            this.chaBody.ChangeAccessory(slotNoFromSubMenuSelect, newType, (int)this.GetPrivateExplicit<SmAccessory>("firstIndex"), string.Empty, false);
            CharFileInfoClothes info = this.coordinateInfo.GetInfo(this.statusInfo.coordinateType);
            info.accessory[slotNoFromSubMenuSelect].Copy(this.clothesInfo.accessory[slotNoFromSubMenuSelect]);
            this.CallPrivateExplicit<SmAccessory>("UpdateShowTab");
            this.chaClothes.ResetAccessoryMove(slotNoFromSubMenuSelect, 7);
            this.customControl.UpdateAcsName();
        }

        public override void UpdateCharaInfoSub()
        {
            if (null == this.chaInfo)
                return;
            this.SetPrivateExplicit<SmAccessory>("initFlags", true);
            if (null != this.tab05)
                this.tab05.isOn = true;
            int slotNoFromSubMenuSelect = (int)this.CallPrivateExplicit<SmAccessory>("GetSlotNoFromSubMenuSelect");
            int num = this.clothesInfo.accessory[slotNoFromSubMenuSelect].type + 1;
            this.SetPrivateExplicit<SmAccessory>("nowTglAllSet", true);
            foreach (Toggle t in (Toggle[])this.GetPrivateExplicit<SmAccessory>("tglType"))
                t.isOn = false;
            this.SetPrivateExplicit<SmAccessory>("nowTglAllSet", false);
            ((Toggle[])this.GetPrivateExplicit<SmAccessory>("tglType"))[num].isOn = true;
            this.ChangeAccessoryTypeList(this.clothesInfo.accessory[slotNoFromSubMenuSelect].type, this.clothesInfo.accessory[slotNoFromSubMenuSelect].id);
            this.CallPrivateExplicit<SmAccessory>("UpdateShowTab");
            this.MoveInfoAllSet();
            this.SetPrivateExplicit<SmAccessory>("initFlags", false);
        }

        public new virtual void ChangeAccessoryTypeList(int newType, int newId)
        {
            this.SetPrivateExplicit<SmAccessory>("acsType", newType);
            if (null == this.chaInfo)
                return;
            if (null == this.objListTop)
                return;
            if (null == this.objLineBase)
                return;
            if (null == this.rtfPanel)
                return;
            int slotNoFromSubMenuSelect = (int)this.CallPrivateExplicit<SmAccessory>("GetSlotNoFromSubMenuSelect");
            int count = 0;
            int selectedIndex = 0;
            if (this._lastType != this.nowSubMenuTypeId && this._objects.ContainsKey(this._lastType))
                foreach (ObjectData o in this._objects[this._lastType])
                    o.obj.SetActive(false);
            if (newType != -1)
            {
                if (this._objects.ContainsKey(newType))
                {
                    foreach (ObjectData o in this._objects[newType])
                    {
                        o.obj.SetActive(true);
                        if (count == 0)
                            this.SetPrivateExplicit<SmAccessory>("firstIndex", o.key);
                        if (newId == -1)
                            o.toggle.isOn = count == 0;
                        else if (o.key == newId)
                            o.toggle.isOn = true;
                        else
                            o.toggle.isOn = false;
                        if (o.toggle.isOn)
                        {
                            selectedIndex = count;
                            o.toggle.onValueChanged.Invoke(true);
                        }
                        ++count;
                    }
                }
                else
                {
                    this._objects.Add(newType, new List<ObjectData>());
                    List<ObjectData> objects = this._objects[newType];
                    Dictionary<int, ListTypeFbx> dictionary = null;
                    CharaListInfo.TypeAccessoryFbx type = (CharaListInfo.TypeAccessoryFbx)((int)Enum.ToObject(typeof(CharaListInfo.TypeAccessoryFbx), newType));
                    dictionary = this.chaInfo.ListInfo.GetAccessoryFbxList(type, true);

                    foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                    {
                        bool flag = false;
                        if (this.chaInfo.customInfo.isConcierge)
                        {
                            flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                        }
                        if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0 || flag)
                        {
                            if (this.chaInfo.Sex == 0)
                            {
                                if ("0" == current.Value.PrefabM)
                                    continue;
                            }
                            else if ("0" == current.Value.PrefabF)
                                continue;
                            if (count == 0)
                                this.SetPrivateExplicit<SmAccessory>("firstIndex", current.Key);
                            GameObject gameObject = Instantiate(this.objLineBase);
                            gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                            FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                            fbxTypeInfo.id = current.Key;
                            fbxTypeInfo.typeName = current.Value.Name;
                            fbxTypeInfo.info = current.Value;
                            gameObject.transform.SetParent(this.objListTop.transform, false);
                            RectTransform rectTransform = gameObject.transform as RectTransform;
                            rectTransform.localScale = new Vector3(1f, 1f, 1f);
                            rectTransform.sizeDelta = new Vector2(this._container.rect.width, 24f);
                            Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                            component.text = fbxTypeInfo.typeName;
                            this.CallPrivateExplicit<SmAccessory>("SetButtonClickHandler", gameObject);
                            Toggle component2 = gameObject.GetComponent<Toggle>();
                            objects.Add(new ObjectData { obj = gameObject, key = current.Key, toggle = component2, text = component });
                            component2.onValueChanged.AddListener(v =>
                            {
                                if (component2.isOn)
                                    UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                            });
                            if (newId == -1)
                            {
                                if (count == 0)
                                    component2.isOn = true;
                            }
                            else if (current.Key == newId)
                                component2.isOn = true;
                            if (component2.isOn)
                                selectedIndex = count;
                            ToggleGroup component3 = this.objListTop.GetComponent<ToggleGroup>();
                            component2.group = component3;
                            gameObject.SetActive(true);
                            if (!flag)
                            {
                                int num3 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                                Transform transform = rectTransform.FindChild("imgNew");
                                if (transform && num3 == 1)
                                    transform.gameObject.SetActive(true);
                            }
                            count++;
                        }
                    }
                }
                float b = 24f * count - 168f;
                float y = Mathf.Min(24f * selectedIndex, b);
                this.rtfPanel.anchoredPosition = new Vector2(0f, y);

                if (this.tab02)
                {
                    this.tab02.gameObject.SetActive(true);
                }
                if (this.tab03)
                {
                    this.tab03.gameObject.SetActive(true);
                }
            }
            else
            {
                this.rtfPanel.sizeDelta = new Vector2(this.rtfPanel.sizeDelta.x, 0f);
                this.rtfPanel.anchoredPosition = new Vector2(0f, 0f);
                if (this.tab02)
                    this.tab02.gameObject.SetActive(false);
                if (this.tab03)
                    this.tab03.gameObject.SetActive(false);
                if (this.tab04)
                    this.tab04.gameObject.SetActive(false);
            }
            this.SetPrivateExplicit<SmAccessory>("nowChanging", true);
            if (this.clothesInfo != null)
            {
                float specularIntensity = this.clothesInfo.accessory[slotNoFromSubMenuSelect].color.specularIntensity;
                float specularSharpness = this.clothesInfo.accessory[slotNoFromSubMenuSelect].color.specularSharpness;
                float specularSharpness2 = this.clothesInfo.accessory[slotNoFromSubMenuSelect].color2.specularSharpness;
                if (this.sldIntensity)
                {
                    this.sldIntensity.value = specularIntensity;
                }
                if (this.inputIntensity)
                {
                    this.inputIntensity.text = this.ChangeTextFromFloat(specularIntensity);
                }
                if (this.sldSharpness[0])
                {
                    this.sldSharpness[0].value = specularSharpness;
                }
                if (this.inputSharpness[0])
                {
                    this.inputSharpness[0].text = this.ChangeTextFromFloat(specularSharpness);
                }
                if (this.sldSharpness[1])
                {
                    this.sldSharpness[1].value = specularSharpness2;
                }
                if (this.inputSharpness[1])
                {
                    this.inputSharpness[1].text = this.ChangeTextFromFloat(specularSharpness2);
                }
            }
            this.SetPrivateExplicit<SmAccessory>("nowChanging", false);
            this.OnClickColorSpecular(1);
            this.OnClickColorSpecular(0);
            this.OnClickColorDiffuse(1);
            this.OnClickColorDiffuse(0);
            this._lastType = newType;
            this._lastId = newId;
        }

        public new virtual void OnChangeAccessoryParentDefault()
        {
            if (this.clothesInfo != null)
            {
                this.SetPrivateExplicit<SmAccessory>("nowTglAllSet", true);
                foreach (Toggle t in this.GetPrivateExplicit<SmAccessory>("tglParent") as Toggle[])
                    t.isOn = false;
                this.SetPrivateExplicit<SmAccessory>("nowTglAllSet", false);
                int slotNoFromSubMenuSelect = (int)this.CallPrivateExplicit<SmAccessory>("GetSlotNoFromSubMenuSelect");
                int id = this.clothesInfo.accessory[slotNoFromSubMenuSelect].id;
                string accessoryDefaultParentStr = this.chaClothes.GetAccessoryDefaultParentStr((int)this.GetPrivateExplicit<SmAccessory>("acsType"), id);
                int parentIndexFromParentKey = this.GetParentIndexFromParentKey(accessoryDefaultParentStr);
                (this.GetPrivateExplicit<SmAccessory>("tglParent") as Toggle[])[parentIndexFromParentKey].isOn = true;
            }
        }

    }
}
