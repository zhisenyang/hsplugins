using System;
using System.Collections.Generic;
using HSUS;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmHair_FCustom : SmHair_F
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

        public void LoadFrom(SmHair_F other)
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
                switch (this.nowSubMenuTypeId)
                {
                    case 47:
                        num = this.customInfo.hairId[1];
                        break;
                    case 48:
                        num = this.customInfo.hairId[0];
                        break;
                    case 49:
                        num = this.customInfo.hairId[2];
                        break;
                    case 50:
                        num = this.customInfo.hairId[0];
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
                    case 47:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairF, true);
                        num = this.customInfo.hairId[1];
                        if (this.btnIntegrate01)
                        {
                            Text[] componentsInChildren = this.btnIntegrate01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren.Length != 0)
                            {
                                componentsInChildren[0].text = "後ろ髪と横髪も同じ色に合わせる";
                            }
                        }
                        if (this.btnIntegrate02)
                        {
                            this.btnIntegrate02.gameObject.SetActive(true);
                        }
                        if (this.btnReference01)
                        {
                            this.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren2 = this.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren2.Length != 0)
                            {
                                componentsInChildren2[0].text = "眉毛の色に合わせる";
                            }
                        }
                        if (this.btnReference02)
                        {
                            this.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren3 = this.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren3.Length != 0)
                            {
                                componentsInChildren3[0].text = "アンダーヘアの色に合わせる";
                            }
                        }
                        break;
                    case 48:
                        if (this.chaInfo.Sex == 0)
                        {
                            dictionary = this.chaInfo.ListInfo.GetMaleFbxList(CharaListInfo.TypeMaleFbx.cm_f_hair, true);
                            num = this.customInfo.hairId[0];
                            if (this.btnIntegrate01)
                            {
                                Text[] componentsInChildren4 = this.btnIntegrate01.GetComponentsInChildren<Text>(true);
                                if (componentsInChildren4.Length != 0)
                                {
                                    componentsInChildren4[0].text = "眉毛とヒゲも同じ色に合わせる";
                                }
                            }
                            if (this.btnIntegrate02)
                            {
                                this.btnIntegrate02.gameObject.SetActive(false);
                            }
                            if (this.btnReference01)
                            {
                                this.btnReference01.gameObject.SetActive(true);
                                Text[] componentsInChildren5 = this.btnReference01.GetComponentsInChildren<Text>(true);
                                if (componentsInChildren5.Length != 0)
                                {
                                    componentsInChildren5[0].text = "眉毛の色に合わせる";
                                }
                            }
                            if (this.btnReference02)
                            {
                                this.btnReference02.gameObject.SetActive(true);
                                Text[] componentsInChildren6 = this.btnReference02.GetComponentsInChildren<Text>(true);
                                if (componentsInChildren6.Length != 0)
                                {
                                    componentsInChildren6[0].text = "ヒゲの色に合わせる";
                                }
                            }
                        }
                        else
                        {
                            dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairB, true);
                            num = this.customInfo.hairId[0];
                            if (this.btnIntegrate01)
                            {
                                Text[] componentsInChildren7 = this.btnIntegrate01.GetComponentsInChildren<Text>(true);
                                if (componentsInChildren7.Length != 0)
                                {
                                    componentsInChildren7[0].text = "前髪と横髪も同じ色に合わせる";
                                }
                            }
                            if (this.btnIntegrate02)
                            {
                                this.btnIntegrate02.gameObject.SetActive(true);
                            }
                            if (this.btnReference01)
                            {
                                this.btnReference01.gameObject.SetActive(true);
                                Text[] componentsInChildren8 = this.btnReference01.GetComponentsInChildren<Text>(true);
                                if (componentsInChildren8.Length != 0)
                                {
                                    componentsInChildren8[0].text = "眉毛の色に合わせる";
                                }
                            }
                            if (this.btnReference02)
                            {
                                this.btnReference02.gameObject.SetActive(true);
                                Text[] componentsInChildren9 = this.btnReference02.GetComponentsInChildren<Text>(true);
                                if (componentsInChildren9.Length != 0)
                                {
                                    componentsInChildren9[0].text = "アンダーヘアの色に合わせる";
                                }
                            }
                        }
                        break;
                    case 49:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairS, true);
                        num = this.customInfo.hairId[2];
                        if (this.btnIntegrate01)
                        {
                            Text[] componentsInChildren10 = this.btnIntegrate01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren10.Length != 0)
                            {
                                componentsInChildren10[0].text = "前髪と後ろ髪も同じ色に合わせる";
                            }
                        }
                        if (this.btnIntegrate02)
                        {
                            this.btnIntegrate02.gameObject.SetActive(true);
                        }
                        if (this.btnReference01)
                        {
                            this.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren11 = this.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren11.Length != 0)
                            {
                                componentsInChildren11[0].text = "眉毛の色に合わせる";
                            }
                        }
                        if (this.btnReference02)
                        {
                            this.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren12 = this.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren12.Length != 0)
                            {
                                componentsInChildren12[0].text = "アンダーヘアの色に合わせる";
                            }
                        }
                        break;
                    case 50:
                        dictionary = this.chaInfo.ListInfo.GetFemaleFbxList(CharaListInfo.TypeFemaleFbx.cf_f_hairB, true);
                        num = this.customInfo.hairId[0];
                        if (this.btnIntegrate01)
                        {
                            Text[] componentsInChildren13 = this.btnIntegrate01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren13.Length != 0)
                            {
                                componentsInChildren13[0].text = "眉毛とアンダーヘアも同じ色に合わせる";
                            }
                        }
                        if (this.btnIntegrate02)
                        {
                            this.btnIntegrate02.gameObject.SetActive(false);
                        }
                        if (this.btnReference01)
                        {
                            this.btnReference01.gameObject.SetActive(true);
                            Text[] componentsInChildren14 = this.btnReference01.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren14.Length != 0)
                            {
                                componentsInChildren14[0].text = "眉毛の色に合わせる";
                            }
                        }
                        if (this.btnReference02)
                        {
                            this.btnReference02.gameObject.SetActive(true);
                            Text[] componentsInChildren15 = this.btnReference02.GetComponentsInChildren<Text>(true);
                            if (componentsInChildren15.Length != 0)
                            {
                                componentsInChildren15[0].text = "アンダーヘアの色に合わせる";
                            }
                        }
                        break;
                }
                if (this.btnIntegrate02)
                {
                    Text[] componentsInChildren16 = this.btnIntegrate02.GetComponentsInChildren<Text>(true);
                    if (componentsInChildren16.Length != 0)
                    {
                        componentsInChildren16[0].text = "眉毛とアンダーヘアも同じ色に合わせる";
                    }
                }
                List<ObjectData> cd = new List<ObjectData>();
                this._objects.Add(this.nowSubMenuTypeId, cd);
                foreach (KeyValuePair<int, ListTypeFbx> current in dictionary)
                {
                    if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0)
                    {
                        if (this.chaInfo.Sex != 0)
                        {
                            if (this.nowSubMenuTypeId == 48)
                            {
                                if ("1" == current.Value.Etc[0])
                                {
                                    continue;
                                }
                            }
                            else if (this.nowSubMenuTypeId == 50 && "1" != current.Value.Etc[0])
                            {
                                continue;
                            }
                        }
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
                        this.CallPrivateExplicit<SmHair_F>("SetButtonClickHandler", gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        cd.Add(new ObjectData {key = current.Key, obj = gameObject, toggle = component2, text = component });
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

            this.SetPrivateExplicit<SmHair_F>("nowChanging", true);
            if (this.customInfo != null)
            {
                float value = 1f;
                float value2 = 1f;
                float value3 = 1f;
                float value4 = 1f;
                switch (this.nowSubMenuTypeId)
                {
                    case 47:
                        value = this.customInfo.hairColor[1].specularIntensity;
                        value2 = this.customInfo.hairColor[1].specularSharpness;
                        value3 = this.customInfo.hairAcsColor[1].specularIntensity;
                        value4 = this.customInfo.hairAcsColor[1].specularSharpness;
                        break;
                    case 48:
                        if (this.chaInfo.Sex == 0)
                        {
                            value = this.customInfo.hairColor[0].specularIntensity;
                            value2 = this.customInfo.hairColor[0].specularSharpness;
                            value3 = this.customInfo.hairAcsColor[0].specularIntensity;
                            value4 = this.customInfo.hairAcsColor[0].specularSharpness;
                        }
                        else
                        {
                            value = this.customInfo.hairColor[0].specularIntensity;
                            value2 = this.customInfo.hairColor[0].specularSharpness;
                            value3 = this.customInfo.hairAcsColor[0].specularIntensity;
                            value4 = this.customInfo.hairAcsColor[0].specularSharpness;
                        }
                        break;
                    case 49:
                        value = this.customInfo.hairColor[2].specularIntensity;
                        value2 = this.customInfo.hairColor[2].specularSharpness;
                        value3 = this.customInfo.hairAcsColor[2].specularIntensity;
                        value4 = this.customInfo.hairAcsColor[2].specularSharpness;
                        break;
                    case 50:
                        value = this.customInfo.hairColor[0].specularIntensity;
                        value2 = this.customInfo.hairColor[0].specularSharpness;
                        value3 = this.customInfo.hairAcsColor[0].specularIntensity;
                        value4 = this.customInfo.hairAcsColor[0].specularSharpness;
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
                if (this.sldSharpness)
                {
                    this.sldSharpness.value = value2;
                }
                if (this.inputSharpness)
                {
                    this.inputSharpness.text = this.ChangeTextFromFloat(value2);
                }
                if (this.sldAcsIntensity)
                {
                    this.sldAcsIntensity.value = value3;
                }
                if (this.inputAcsIntensity)
                {
                    this.inputAcsIntensity.text = this.ChangeTextFromFloat(value3);
                }
                if (this.sldAcsSharpness)
                {
                    this.sldAcsSharpness.value = value4;
                }
                if (this.inputAcsSharpness)
                {
                    this.inputAcsSharpness.text = this.ChangeTextFromFloat(value4);
                }
            }
            this.SetPrivateExplicit<SmHair_F>("nowChanging", false);
            this.OnClickAcsColorSpecular();
            this.OnClickAcsColorDiffuse();
            this.OnClickColorSpecular();
            this.OnClickColorDiffuse();
            this._previousType = this.nowSubMenuTypeId;
        }
    }
}
