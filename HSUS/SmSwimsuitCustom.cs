using System;
using System.Collections.Generic;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmSwimsuitCustom : SmSwimsuit
    {
        private class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        private readonly Dictionary<int, List<ObjectData>> _objects = new Dictionary<int, List<ObjectData>>();
        private int _previousType;
        private RectTransform _container;
        private InputField _searchBar;

        public void LoadFrom(SmSwimsuit other)
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
            foreach (Text t in this._searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
        }

        private void SearchChanged(string arg0)
        {
            string search = this._searchBar.text.Trim();
            if (this._objects.ContainsKey(this.nowSubMenuTypeId) == false)
                return;
            foreach (ObjectData objectData in this._objects[this.nowSubMenuTypeId])
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
            if (null == this.chaInfo || null == this.objListTop || null == this.objLineBase || null == this.rtfPanel)
                return;
            if (null != this.tglTab)
                this.tglTab.isOn = true;

            if (this._previousType != this.nowSubMenuTypeId && this._objects.ContainsKey(this._previousType))
                foreach (ObjectData o in this._objects[this._previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (this._objects.ContainsKey(this.nowSubMenuTypeId))
            {
                int num2 = 0;
                if (this.clothesInfo != null)
                {
                    num2 = this.clothesInfo.clothesId[4];
                }
                count = this._objects[this.nowSubMenuTypeId].Count;
                for (int i = 0; i < this._objects[this.nowSubMenuTypeId].Count; i++)
                {
                    ObjectData o = this._objects[this.nowSubMenuTypeId][i];
                    o.obj.SetActive(true);
                    if (o.key == num2)
                    {
                        selected = i;
                        o.toggle.isOn = true;
                        o.toggle.onValueChanged.Invoke(true);
                    }
                    else if (o.toggle.isOn)
                        o.toggle.isOn = false;
                }
            }
            else
            {
                Dictionary<int, ListTypeFbx> dictionary = null;
                int num2 = 0;
                dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swim, true);
                if (this.clothesInfo != null)
                {
                    num2 = this.clothesInfo.clothesId[4];
                }

                List<ObjectData> cd = new List<ObjectData>();
                this._objects.Add(this.nowSubMenuTypeId, cd);

                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
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
                        this.CallPrivateExplicit<SmSwimsuit>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new ObjectData() {key = current.Key, obj = gameObject, toggle = component2, text = component});
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                        });
                        if (current.Key == num2)
                        {
                            component2.isOn = true;
                            selected = count;
                        }
                        ToggleGroup component3 = this.objListTop.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        int num5 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform = rectTransform.FindChild("imgNew");
                        if (transform && num5 == 1)
                        {
                            transform.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            this.rtfPanel.anchoredPosition = new Vector2(0f, y);

            this.SetPrivateExplicit<SmSwimsuit>("nowChanging", true);
            if (this.clothesInfo != null)
            {
                float specularIntensity = this.clothesInfo.clothesColor[4].specularIntensity;
                float specularSharpness = this.clothesInfo.clothesColor[4].specularSharpness;
                float specularSharpness2 = this.clothesInfo.clothesColor2[4].specularSharpness;
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
            this.SetPrivateExplicit<SmSwimsuit>("nowChanging", false);
            this.OnClickColorSpecular(1);
            this.OnClickColorSpecular(0);
            this.OnClickColorDiffuse(1);
            this.OnClickColorDiffuse(0);
            if (this.tglOpt01)
            {
                this.tglOpt01.isOn = !this.clothesInfoF.hideSwimOptTop;
            }
            if (this.tglOpt02)
            {
                this.tglOpt02.isOn = !this.clothesInfoF.hideSwimOptBot;
            }
            this._previousType = this.nowSubMenuTypeId;
        }
    }
}
