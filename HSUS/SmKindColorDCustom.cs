using System;
using System.Collections.Generic;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmKindColorDCustom : SmKindColorD
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
        private InputField _searchBar;
        private RectTransform _container;

        public void LoadFrom(SmKindColorD other)
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
            this.nowSubMenuTypeId = smTypeId;
            this.SetCharaInfoSub();
        }

        public override void SetCharaInfoSub()
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
                if (this.customInfo != null)
                    switch (this.nowSubMenuTypeId)
                    {
                        case 39:
                            num = this.customInfoF.texEyeshadowId;
                            break;
                        case 40:
                            num = this.customInfoF.texCheekId;
                            break;
                        case 41:
                            num = this.customInfoF.texLipId;
                            break;
                        case 42:
                            num = this.customInfo.texTattoo_fId;
                            break;
                        case 43:
                            num = this.customInfoF.texMoleId;
                            break;
                        case 9:
                            num = this.customInfo.texTattoo_bId;
                            break;
                        case 8:
                            num = this.customInfoF.texSunburnId;
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
                Dictionary<int, ListTypeTexture> dictionary = null;
                int num = 0;
                int nowSubMenuTypeId = this.nowSubMenuTypeId;
                switch (nowSubMenuTypeId)
                {
                    case 39:
                        dictionary = this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_eyeshadow, true);
                        if (this.customInfo != null)
                        {
                            num = this.customInfoF.texEyeshadowId;
                        }
                        break;
                    case 40:
                        dictionary = this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_cheek, true);
                        if (this.customInfo != null)
                        {
                            num = this.customInfoF.texCheekId;
                        }
                        break;
                    case 41:
                        dictionary = this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_lip, true);
                        if (this.customInfo != null)
                        {
                            num = this.customInfoF.texLipId;
                        }
                        break;
                    case 42:
                        dictionary = this.chaInfo.Sex == 0 ? this.chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_tattoo_f, true) : this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_tattoo_f, true);
                        if (this.customInfo != null)
                        {
                            num = this.customInfo.texTattoo_fId;
                        }
                        break;
                    case 43:
                        dictionary = this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_mole, true);
                        if (this.customInfo != null)
                        {
                            num = this.customInfoF.texMoleId;
                        }
                        break;
                    default:
                        if (nowSubMenuTypeId != 8)
                        {
                            if (nowSubMenuTypeId == 9)
                            {
                                dictionary = this.chaInfo.Sex == 0 ? this.chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_tattoo_b, true) : this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_tattoo_b, true);
                                if (this.customInfo != null)
                                {
                                    num = this.customInfo.texTattoo_bId;
                                }
                            }
                        }
                        else
                        {
                            dictionary = this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_sunburn, true);
                            if (this.customInfo != null)
                            {
                                num = this.customInfoF.texSunburnId;
                            }
                        }
                        break;
                }
                List<ObjectData> cd = new List<ObjectData>();
                this._objects.Add(this.nowSubMenuTypeId, cd);

                foreach (KeyValuePair<int, ListTypeTexture> current in dictionary)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject = Instantiate(this.objLineBase);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        TexTypeInfo texTypeInfo = gameObject.AddComponent<TexTypeInfo>();
                        texTypeInfo.id = current.Key;
                        texTypeInfo.typeName = current.Value.Name;
                        texTypeInfo.info = current.Value;
                        gameObject.transform.SetParent(this.objListTop.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(this._container.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = texTypeInfo.typeName;
                        this.CallPrivateExplicit<SmKindColorD>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new ObjectData {key = current.Key, obj = gameObject, toggle = component2, text = component});
                        if (current.Key == num)
                        {
                            component2.isOn = true;
                            selected = count;
                        }
                        ToggleGroup component3 = this.objListTop.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform = rectTransform.FindChild("imgNew");
                        if (transform && num4 == 1)
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

            this.OnClickColorDiffuse();
            this._previousType = this.nowSubMenuTypeId;
        }
    }
}
