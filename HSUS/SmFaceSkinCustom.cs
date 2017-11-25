using System;
using System.Collections.Generic;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmFaceSkinCustom : SmFaceSkin
    {
        private class ObjectData
        {
            public int key;
            public Toggle toggle;
            public Text text;
            public GameObject obj;
        }

        private bool _created;
        private readonly List<ObjectData> _listHead = new List<ObjectData>();
        private readonly List<ObjectData> _listFace = new List<ObjectData>();
        private readonly List<ObjectData> _listDetail = new List<ObjectData>();
        private RectTransform _containerHead;
        private InputField _searchBarHead;
        private RectTransform _containerSkin;
        private InputField _searchBarSkin;
        private RectTransform _containerDetail;
        private InputField _searchBarDetail;

        public void LoadFrom(SmFaceSkin other)
        {
            this.LoadWith(other);
            this.ReplaceEventsOf(other);

            {
                this._containerHead = this.transform.FindChild("TabControl/TabItem01").FindDescendant("ListTop").transform as RectTransform;
                VerticalLayoutGroup group = this._containerHead.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                ContentSizeFitter fitter = this._containerHead.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                this.rtfPanelHead.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                group = this.rtfPanelHead.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                RectTransform rt = this.transform.FindChild("TabControl/TabItem01/ScrollView") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);
                float newY = rt.offsetMax.y;
                rt = this.transform.FindChild("TabControl/TabItem01/Scrollbar") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);

                this._searchBarHead = UIUtility.CreateInputField("Search Bar", this.transform.FindChild("TabControl/TabItem01"));
                rt = this._searchBarHead.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
                this._searchBarHead.placeholder.GetComponent<Text>().text = "Search...";
                this._searchBarHead.onValueChanged.AddListener(this.SearchChangedHead);
            }

            {
                this._containerSkin = this.transform.FindChild("TabControl/TabItem02").FindDescendant("ListTop").transform as RectTransform;
                VerticalLayoutGroup group = this._containerSkin.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                ContentSizeFitter fitter = this._containerSkin.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                this.rtfPanelSkin.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                group = this.rtfPanelSkin.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                RectTransform rt = this.transform.FindChild("TabControl/TabItem02/ScrollView") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);
                float newY = rt.offsetMax.y;
                rt = this.transform.FindChild("TabControl/TabItem02/Scrollbar") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);

                this._searchBarSkin = UIUtility.CreateInputField("Search Bar", this.transform.FindChild("TabControl/TabItem02"));
                rt = this._searchBarSkin.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
                this._searchBarSkin.placeholder.GetComponent<Text>().text = "Search...";
                this._searchBarSkin.onValueChanged.AddListener(this.SearchChangedSkin);
            }

            {
                this._containerDetail = this.transform.FindChild("TabControl/TabItem03").FindDescendant("ListTop").transform as RectTransform;
                VerticalLayoutGroup group = this._containerDetail.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                ContentSizeFitter fitter = this._containerDetail.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                this.rtfPanelDetail.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                group = this.rtfPanelDetail.gameObject.AddComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                RectTransform rt = this.transform.FindChild("TabControl/TabItem03/ScrollView") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);
                float newY = rt.offsetMax.y;
                rt = this.transform.FindChild("TabControl/TabItem03/Scrollbar") as RectTransform;
                rt.offsetMax += new Vector2(0f, -24f);

                this._searchBarDetail = UIUtility.CreateInputField("Search Bar", this.transform.FindChild("TabControl/TabItem03"));
                rt = this._searchBarDetail.transform as RectTransform;
                rt.localPosition = Vector3.zero;
                rt.localScale = Vector3.one;
                rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
                this._searchBarDetail.placeholder.GetComponent<Text>().text = "Search...";
                this._searchBarDetail.onValueChanged.AddListener(this.SearchChangedDetail);
            }
        }

        private void SearchChangedHead(string arg0)
        {
            this.SearchChanged(this._searchBarHead.text.Trim(), this._listHead);
        }

        private void SearchChangedSkin(string arg0)
        {
            this.SearchChanged(this._searchBarSkin.text.Trim(), this._listFace);
        }

        private void SearchChangedDetail(string arg0)
        {
            this.SearchChanged(this._searchBarDetail.text.Trim(), this._listDetail);
        }

        private void SearchChanged(string search, List<ObjectData> list)
        {
            if (list != null)
                foreach (ObjectData objectData in list)
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
            this._searchBarHead.text = "";
            this.SearchChangedHead("");
            this._searchBarSkin.text = "";
            this.SearchChangedSkin("");
            this._searchBarDetail.text = "";
            this.SearchChangedDetail("");
            this.nowSubMenuTypeId = smTypeId;
            this.SetCharaInfoSub();
        }

        public override void SetCharaInfoSub()
        {
            if (null == this.chaInfo)
                return;
            if (null == this.objListTopHead)
                return;
            if (null == this.objListTopSkin)
                return;
            if (null == this.objListTopDetail)
                return;
            if (null == this.objLineBase)
                return;
            if (null == this.rtfPanelHead)
                return;
            if (null == this.rtfPanelSkin)
                return;
            if (null == this.rtfPanelDetail)
                return;
            if (this._created == false)
            {
                Dictionary<int, ListTypeFbx> dictionary = this.chaInfo.Sex == 0 ? this.chaInfo.ListInfo.GetMaleFbxList(CharaListInfo.TypeMaleFbx.cm_f_head, true) : this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_head, true);
                int num = 0;
                if (this.customInfo != null)
                {
                    num = this.customInfo.headId;
                }
                int count = 0;
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
                        gameObject.transform.SetParent(this.objListTopHead.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.sizeDelta = new Vector2(this._containerHead.rect.width, 24f);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;
                        this.CallPrivateExplicit<SmFaceSkin>("SetHeadButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        this._listHead.Add(new ObjectData(){key = current.Key, obj = gameObject, text = component, toggle = component2});
                        if (current.Key == num)
                        {
                            component2.isOn = true;
                        }
                        ToggleGroup component3 = this.objListTopHead.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        int num3 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform = rectTransform.FindChild("imgNew");
                        if (transform && num3 == 1)
                        {
                            transform.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
                Dictionary<int, ListTypeTexture> dictionary2 = this.chaInfo.Sex == 0 ? this.chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_face, true) : this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_face, true);
                num = 0;
                if (this.customInfo != null)
                {
                    num = this.customInfo.texFaceId;
                }
                count = 0;
                foreach (KeyValuePair<int, ListTypeTexture> current in dictionary2)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject2 = Instantiate(this.objLineBase);
                        gameObject2.AddComponent<LayoutElement>().preferredHeight = 24f;
                        TexTypeInfo texTypeInfo = gameObject2.AddComponent<TexTypeInfo>();
                        texTypeInfo.id = current.Key;
                        texTypeInfo.typeName = current.Value.Name;
                        texTypeInfo.info = current.Value;
                        gameObject2.transform.SetParent(this.objListTopSkin.transform, false);
                        RectTransform rectTransform2 = gameObject2.transform as RectTransform;
                        rectTransform2.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform2.sizeDelta = new Vector2(this._containerSkin.rect.width, 24f);
                        Text component4 = rectTransform2.FindChild("Label").GetComponent<Text>();
                        component4.text = texTypeInfo.typeName;
                        this.CallPrivateExplicit<SmFaceSkin>("SetSkinButtonClickHandler", gameObject2);
                        Toggle component5 = gameObject2.GetComponent<Toggle>();
                        this._listFace.Add(new ObjectData() { key = current.Key, obj = gameObject2, text = component4, toggle = component5 });
                        if (current.Key == num)
                        {
                            component5.isOn = true;
                        }
                        ToggleGroup component6 = this.objListTopSkin.GetComponent<ToggleGroup>();
                        component5.group = component6;
                        gameObject2.SetActive(true);
                        int num5 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform2 = rectTransform2.FindChild("imgNew");
                        if (transform2 && num5 == 1)
                        {
                            transform2.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
                dictionary2 = this.chaInfo.Sex == 0 ? this.chaInfo.ListInfo.GetMaleTextureList(CharaListInfo.TypeMaleTexture.cm_t_detail_f, true) : this.chaInfo.ListInfo.GetFemaleTextureList(CharaListInfo.TypeFemaleTexture.cf_t_detail_f, true);
                num = 0;
                if (this.customInfo != null)
                {
                    num = this.customInfo.texFaceDetailId;
                }
                count = 0;
                foreach (KeyValuePair<int, ListTypeTexture> current in dictionary2)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        GameObject gameObject3 = Instantiate(this.objLineBase);
                        gameObject3.AddComponent<LayoutElement>().preferredHeight = 24f;
                        TexTypeInfo texTypeInfo2 = gameObject3.AddComponent<TexTypeInfo>();
                        texTypeInfo2.id = current.Key;
                        texTypeInfo2.typeName = current.Value.Name;
                        texTypeInfo2.info = current.Value;
                        gameObject3.transform.SetParent(this.objListTopDetail.transform, false);
                        RectTransform rectTransform3 = gameObject3.transform as RectTransform;
                        rectTransform3.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform3.sizeDelta = new Vector2(this._containerDetail.rect.width, 24f);
                        Text component7 = rectTransform3.FindChild("Label").GetComponent<Text>();
                        component7.text = texTypeInfo2.typeName;
                        this.CallPrivateExplicit<SmFaceSkin>("SetDetailButtonClickHandler", gameObject3);
                        Toggle component8 = gameObject3.GetComponent<Toggle>();
                        this._listDetail.Add(new ObjectData() { key = current.Key, obj = gameObject3, text = component7, toggle = component8 });
                        if (current.Key == num)
                        {
                            component8.isOn = true;
                        }
                        ToggleGroup component9 = this.objListTopDetail.GetComponent<ToggleGroup>();
                        component8.group = component9;
                        gameObject3.SetActive(true);
                        int num7 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                        Transform transform3 = rectTransform3.FindChild("imgNew");
                        if (transform3 && num7 == 1)
                        {
                            transform3.gameObject.SetActive(true);
                        }
                        count++;
                    }
                }
            }
            else
            {
                int num = 0;
                if (this.customInfo != null)
                    num = this.customInfo.headId;
                foreach (ObjectData objectData in this._listHead)
                {
                    if (objectData.key == num)
                        objectData.toggle.isOn = true;
                    else if (objectData.toggle.isOn)
                        objectData.toggle.isOn = false;
                }
                num = 0;
                if (this.customInfo != null)
                    num = this.customInfo.texFaceId;
                foreach (ObjectData objectData in this._listFace)
                {
                    if (objectData.key == num)
                        objectData.toggle.isOn = true;
                    else if (objectData.toggle.isOn)
                        objectData.toggle.isOn = false;
                }
                num = 0;
                if (this.customInfo != null)
                    num = this.customInfo.texFaceDetailId;
                foreach (ObjectData objectData in this._listDetail)
                {
                    if (objectData.key == num)
                        objectData.toggle.isOn = true;
                    else if (objectData.toggle.isOn)
                        objectData.toggle.isOn = false;
                }
            }
            this.SetPrivateExplicit<SmFaceSkin>("nowChanging", true);
            if (this.customInfo != null)
            {
                if (this.sldDetail)
                {
                    this.sldDetail.value = this.customInfo.faceDetailWeight;
                }
                if (this.inputDetail)
                {
                    this.inputDetail.text = this.ChangeTextFromFloat(this.customInfo.faceDetailWeight);
                }
            }
            this.SetPrivateExplicit<SmFaceSkin>("nowChanging", false);
            this._created = true;
        }
    }
}
