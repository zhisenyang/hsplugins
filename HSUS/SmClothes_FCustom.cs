using System;
using System.Collections.Generic;
using HSUS;
using IllusionUtility.GetUtility;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmClothes_FCustom : SmClothes_F
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

        public void LoadFrom(SmClothes_F other)
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
            {
                return;
            }
            this.colorMenu = this.customControl.colorMenu;
            this.chaInfo = this.customControl.chainfo;
            if (null == this.chaInfo)
            {
                return;
            }
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
            this._previousType = this.nowSubMenuTypeId;
            this.nowSubMenuTypeId = smTypeId;
            this.SetCharaInfoSub();
        }

        public new virtual void SetCharaInfoSub()
        {
            if (null == this.chaInfo)
            {
                return;
            }
            if (null == this.objListTop)
            {
                return;
            }
            if (null == this.objLineBase)
            {
                return;
            }
            if (null == this.rtfPanel)
            {
                return;
            }
            if (null != this.tglTab)
            {
                this.tglTab.isOn = true;
            }
            if (this._previousType != this.nowSubMenuTypeId && this._objects.ContainsKey(this._previousType))
                foreach (ObjectData o in this._objects[this._previousType])
                    o.obj.SetActive(false);
            int count = 0;
            int selected = 0;

            if (this._objects.ContainsKey(this.nowSubMenuTypeId))
            {
                int num = 0;
                if (this.clothesInfoF != null)
                    switch (this.nowSubMenuTypeId)
                    {
                        case 57:
                            num = this.clothesInfoF.clothesId[0];
                            break;
                        case 58:
                            num = this.clothesInfoF.clothesId[1];
                            break;
                        case 59:
                            num = this.clothesInfoF.clothesId[2];
                            break;
                        case 60:
                            num = this.clothesInfoF.clothesId[3];
                            break;
                        case 61:
                            num = this.clothesInfoF.clothesId[7];
                            break;
                        case 62:
                            num = this.clothesInfoF.clothesId[8];
                            break;
                        case 63:
                            num = this.clothesInfoF.clothesId[9];
                            break;
                        case 64:
                            num = this.clothesInfoF.clothesId[10];
                            break;
                        case 76:
                            num = this.clothesInfoF.clothesId[5];
                            break;
                        case 77:
                            num = this.clothesInfoF.clothesId[6];
                            break;
                    }
                count = this._objects[this.nowSubMenuTypeId].Count;
                for (int i = 0; i < this._objects[this.nowSubMenuTypeId].Count; i++)
                {
                    ObjectData o = this._objects[this.nowSubMenuTypeId][i];
                    o.obj.SetActive(true);
                    if (o.key == num)
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
                int num = 0;
                switch (this.nowSubMenuTypeId)
                {
                    case 57:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_top);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[0];
                        }
                        break;
                    case 58:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bot);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[1];
                        }
                        break;
                    case 59:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_bra);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[2];
                        }
                        break;
                    case 60:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shorts);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[3];
                        }
                        break;
                    case 61:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_gloves);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[7];
                        }
                        break;
                    case 62:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_panst);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[8];
                        }
                        break;
                    case 63:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_socks);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[9];
                        }
                        break;
                    case 64:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_shoes);
                        if (this.clothesInfoF != null)
                        {
                            num = this.clothesInfoF.clothesId[10];
                        }
                        break;
                    default:
                        if (this.nowSubMenuTypeId != 76)
                        {
                            if (this.nowSubMenuTypeId == 77)
                            {
                                dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimbot);
                                if (this.clothesInfoF != null)
                                {
                                    num = this.clothesInfoF.clothesId[6];
                                }
                            }
                        }
                        else
                        {
                            dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_swimtop);
                            if (this.clothesInfoF != null)
                            {
                                num = this.clothesInfoF.clothesId[5];
                            }
                        }
                        break;
                }
                List<ObjectData> cd = new List<ObjectData>();
                this._objects.Add(this.nowSubMenuTypeId, cd);
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    bool flag = false;
                    if (this.chaInfo.customInfo.isConcierge)
                    {
                        flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                    }
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0 || flag)
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
                        this.CallPrivateExplicit<SmClothes_F>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new ObjectData { key = current.Key, obj = gameObject, toggle = component2, text = component });
                        component2.onValueChanged.AddListener(v =>
                        {
                            if (component2.isOn)
                                UnityEngine.Debug.Log(fbxTypeInfo.info.Id + " " + fbxTypeInfo.info.ABPath);
                        });
                        if (current.Key == num)
                        {
                            component2.isOn = true;
                            selected = count;
                        }
                        ToggleGroup component3 = this.objListTop.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        if (!flag)
                        {
                            int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                            Transform transform = rectTransform.FindChild("imgNew");
                            if (transform && num4 == 1)
                            {
                                transform.gameObject.SetActive(true);
                            }
                        }
                        IdTglInfo idTglInfo = new IdTglInfo();
                        idTglInfo.coorde = int.Parse(current.Value.Etc[1]);
                        idTglInfo.tgl = component2;
                        GameObject gameObject2 = gameObject.transform.FindLoop("Background");
                        if (gameObject2)
                        {
                            idTglInfo.imgBG = gameObject2.GetComponent<Image>();
                        }
                        gameObject2 = gameObject.transform.FindLoop("Checkmark");
                        if (gameObject2)
                        {
                            idTglInfo.imgCheck = gameObject2.GetComponent<Image>();
                        }
                        gameObject2 = gameObject.transform.FindLoop("Label");
                        if (gameObject2)
                        {
                            idTglInfo.text = gameObject2.GetComponent<Text>();
                        }
                        ((List<IdTglInfo>)this.GetPrivateExplicit<SmClothes_F>("lstIdTgl")).Add(idTglInfo);
                        count++;
                    }
                }
            }
            float b = 24f * count - 232f;
            float y = Mathf.Min(24f * selected, b);
            this.rtfPanel.anchoredPosition = new Vector2(0f, y);
            this.SetPrivateExplicit<SmClothes_F>("nowChanging", true);
            if (this.clothesInfoF != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                switch (this.nowSubMenuTypeId)
                {
                    case 57:
                        value = this.clothesInfoF.clothesColor[0].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[0].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[0].specularSharpness;
                        this.CallPrivateExplicit<SmClothes_F>("UpdateTopListEnable");
                        break;
                    case 58:
                        value = this.clothesInfoF.clothesColor[1].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[1].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[1].specularSharpness;
                        this.CallPrivateExplicit<SmClothes_F>("UpdateTopListEnable");
                        break;
                    case 59:
                        value = this.clothesInfoF.clothesColor[2].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[2].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[2].specularSharpness;
                        break;
                    case 60:
                        value = this.clothesInfoF.clothesColor[3].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[3].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[3].specularSharpness;
                        break;
                    case 61:
                        value = this.clothesInfoF.clothesColor[7].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[7].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[7].specularSharpness;
                        break;
                    case 62:
                        value = this.clothesInfoF.clothesColor[8].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[8].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[8].specularSharpness;
                        break;
                    case 63:
                        value = this.clothesInfoF.clothesColor[9].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[9].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[9].specularSharpness;
                        break;
                    case 64:
                        value = this.clothesInfoF.clothesColor[10].specularIntensity;
                        value2 = this.clothesInfoF.clothesColor[10].specularSharpness;
                        value3 = this.clothesInfoF.clothesColor2[10].specularSharpness;
                        break;
                    default:
                        if (this.nowSubMenuTypeId != 76)
                        {
                            if (this.nowSubMenuTypeId == 77)
                            {
                                value = this.clothesInfoF.clothesColor[6].specularIntensity;
                                value2 = this.clothesInfoF.clothesColor[6].specularSharpness;
                                value3 = this.clothesInfoF.clothesColor2[6].specularSharpness;
                            }
                        }
                        else
                        {
                            value = this.clothesInfoF.clothesColor[5].specularIntensity;
                            value2 = this.clothesInfoF.clothesColor[5].specularSharpness;
                            value3 = this.clothesInfoF.clothesColor2[5].specularSharpness;
                        }
                        break;
                }
                if (this.sldIntensity)
                {
                    this.sldIntensity.value = value;
                }
                if (this.inputIntensity)
                {
                    this.inputIntensity.text = this.ChangeTextFromFloat(value);
                }
                if (this.sldSharpness[0])
                {
                    this.sldSharpness[0].value = value2;
                }
                if (this.inputSharpness[0])
                {
                    this.inputSharpness[0].text = this.ChangeTextFromFloat(value2);
                }
                if (this.sldSharpness[1])
                {
                    this.sldSharpness[1].value = value3;
                }
                if (this.inputSharpness[1])
                {
                    this.inputSharpness[1].text = this.ChangeTextFromFloat(value3);
                }
            }
            this.SetPrivateExplicit<SmClothes_F>("nowChanging", false);
            this.OnClickColorSpecular(1);
            this.OnClickColorSpecular(0);
            this.OnClickColorDiffuse(1);
            this.OnClickColorDiffuse(0);
        }
    }
}
