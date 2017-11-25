using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using HSUS;
using IllusionUtility.GetUtility;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmClothesLoadCustom : SmClothesLoad
    {
        private bool _created;
        private RectTransform _container;
        private readonly Dictionary<FileInfo, GameObject> _objects = new Dictionary<FileInfo, GameObject>();
        private List<FileInfo> _fileInfos;
        private List<RectTransform> _rectTransforms;
        private InputField _searchBar;

        public void LoadFrom(SmClothesLoad other)
        {
            this.LoadWith(other);
            this.ReplaceEventsOf(other);

            this._fileInfos = (List<FileInfo>)this.GetPrivateExplicit<SmClothesLoad>("lstFileInfo");
            this._rectTransforms = ((List<RectTransform>)this.GetPrivateExplicit<SmClothesLoad>("lstRtfTgl"));

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

            RectTransform rt = this.transform.FindChild("ScrollView") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);
            float newY = rt.offsetMax.y;
            rt = this.transform.FindChild("Scrollbar") as RectTransform;
            rt.offsetMax += new Vector2(0f, -24f);

            this._searchBar = UIUtility.CreateInputField("Search Bar", this.transform);
            rt = this._searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, newY), new Vector2(0f, newY + 24f));
            this._searchBar.placeholder.GetComponent<Text>().text = "Search...";
            this._searchBar.onValueChanged.AddListener(this.SearchChanged);

            foreach (CustomCheck customCheck in Resources.FindObjectsOfTypeAll<CustomCheck>())
            {
                CustomCheck check = customCheck;
                if (check.smClothesLoad == other)
                    check.smClothesLoad = this;
                customCheck.transform.FindChild("checkPng/checkInputName/BtnYes").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (((int)check.GetPrivateExplicit<CustomCheck>("mode")) == 10 && check.smClothesLoad)
                        this.RecreateList();
                });
                customCheck.transform.FindChild("checkPng/checkOverwriteWithInput/BtnYes").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (((int)check.GetPrivateExplicit<CustomCheck>("mode")) == 9 && check.smClothesLoad)
                        this.RecreateList();
                });
                customCheck.transform.FindChild("checkPng/checkDelete/BtnYes").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (((int)check.GetPrivateExplicit<CustomCheck>("mode")) == 8 && check.smClothesLoad)
                        this.RecreateList();
                });
            }
        }

        private void SearchChanged(string arg0)
        {
            string search = this._searchBar.text.Trim();
            if (this._objects != null && this._objects.Count != 0)
                foreach (FileInfo fi in this._fileInfos)
                    this._objects[fi].SetActive(fi.comment.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
        }

        private void RecreateList()
        {
            this.ExecuteDelayed(() =>
            {
                this._objects.Clear();
                for (int i = 0; i < this._fileInfos.Count; i++)
                {
                    Transform t = this.objListTop.transform.GetChild(i);
                    t.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    this._objects.Add(this._fileInfos[i], t.gameObject);
                }
                this.UpdateSort();
            }, 3);
        }

        public new virtual void SetCharaInfo(int smTypeId, bool sameSubMenu)
        {
            if (null != this.customControl)
            {
                this.colorMenu = this.customControl.colorMenu;
                this.chaInfo = this.customControl.chainfo;
                if (null != this.chaInfo)
                {
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
                    this.nowSubMenuTypeId = smTypeId;
                    this.SetCharaInfoSub();
                }
            }
        }

        public override void SetCharaInfoSub()
        {
            if (this.chaInfo == null || this.objListTop == null || this.objLineBase == null || this.rtfPanel == null)
                return;
            this._searchBar.text = "";
            this.SearchChanged("");

            this.Init();
            switch (this.nowSubMenuTypeId)
            {
                case 52:
                    {
                        this.texBtn01.text = "服のみ読込み";
                        this.texBtn02.text = "アクセのみ読込み";
                        this.texBtn03.text = "全て読込み";
                        break;
                    }
                case 51:
                    {
                        this.texBtn01.text = "削除";
                        this.texBtn02.text = "新規保存";
                        this.texBtn03.text = "上書き保存";
                        break;
                    }
            }
            this.CreateListObject();
            this.UpdateSort();
            Toggle[] componentsInChildren = this.objListTop.GetComponentsInChildren<Toggle>(true);
            foreach (Toggle t in componentsInChildren)
            {
                t.isOn = false;
            }
        }

        public new virtual void CreateListObject()
        {
            if (this.imgPrev)
                this.imgPrev.enabled = false;
            if (this._created)
                return;
            while (this.objListTop.transform.childCount != 0)
            {
                GameObject obj = this.objListTop.transform.GetChild(0).gameObject;
                obj.transform.SetParent(null);
                Destroy(obj);
            }
            this._rectTransforms.Clear();
            ToggleGroup component = this.objListTop.GetComponent<ToggleGroup>();
            int num2 = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                GameObject gameObject = Instantiate(this.objLineBase);
                this._objects.Add(fi, gameObject);
                gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                FileInfoComponent fileInfoComponent = gameObject.AddComponent<FileInfoComponent>();
                fileInfoComponent.info = fi;
                GameObject gameObject2 = null;
                fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                gameObject2 = gameObject.transform.FindLoop("Background");
                if (gameObject2)
                {
                    fileInfoComponent.imgBG = gameObject2.GetComponent<Image>();
                }
                gameObject2 = gameObject.transform.FindLoop("Checkmark");
                if (gameObject2)
                {
                    fileInfoComponent.imgCheck = gameObject2.GetComponent<Image>();
                }
                gameObject2 = gameObject.transform.FindLoop("Label");
                if (gameObject2)
                {
                    fileInfoComponent.text = gameObject2.GetComponent<Text>();
                }
                fileInfoComponent.tgl.group = component;
                gameObject.transform.SetParent(this.objListTop.transform, false);
                RectTransform rectTransform = gameObject.transform as RectTransform;
                rectTransform.localScale = new Vector3(1f, 1f, 1f);
                rectTransform.anchoredPosition = new Vector3(0f, (float)(-24.0 * num2), 0f);
                Text component2 = rectTransform.FindChild("Label").GetComponent<Text>();
                component2.text = fileInfoComponent.info.comment;
                this.CallPrivateExplicit<SmClothesLoad>("SetButtonClickHandler", gameObject);
                gameObject.SetActive(true);
                this._rectTransforms.Add(rectTransform);
                num2++;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
            this._created = true;
        }

        public new virtual void OnSortDate()
        {
            this.SortDate(!(bool)this.GetPrivateExplicit<SmClothesLoad>("ascendDate"));
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
        }
        public new virtual void OnSortName()
        {
            this.SortName(!(bool)this.GetPrivateExplicit<SmClothesLoad>("ascendName"));
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
        }
        public new virtual void UpdateSort()
        {
            if (((byte)this.GetPrivateExplicit<SmClothesLoad>("lastSort")) == 0)
            {
                this.SortDate((bool)this.GetPrivateExplicit<SmClothesLoad>("ascendDate"));
                this.SortName((bool)this.GetPrivateExplicit<SmClothesLoad>("ascendName"));
            }
            else
            {
                this.SortName((bool)this.GetPrivateExplicit<SmClothesLoad>("ascendName"));
                this.SortDate((bool)this.GetPrivateExplicit<SmClothesLoad>("ascendDate"));
            }
        }
        public new virtual void SortDate(bool ascend)
        {
            this.SetPrivateExplicit<SmClothesLoad>("ascendDate", ascend);
            if (this._fileInfos.Count == 0)
                return;
            this.SetPrivateExplicit<SmClothesLoad>("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                this._fileInfos.Sort((a, b) => a.time.CompareTo(b.time));
            else
                this._fileInfos.Sort((a, b) => b.time.CompareTo(a.time));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                fi.no = i;
                ++i;
            }
            foreach (FileInfo fi in this._fileInfos)
            {
                foreach (RectTransform rt in this._rectTransforms)
                {
                    FileInfoComponent component = rt.GetComponent<FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        break;
                    }
                }
            }
        }
        public new virtual void SortName(bool ascend)
        {
            this.SetPrivateExplicit<SmClothesLoad>("ascendName", ascend);
            if (this._fileInfos.Count == 0)
                return;
            this.SetPrivateExplicit<SmClothesLoad>("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
                this._fileInfos.Sort((a, b) => a.comment.CompareTo(b.comment));
            else
                this._fileInfos.Sort((a, b) => b.comment.CompareTo(a.comment));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                fi.no = i;
                ++i;
            }
            foreach (FileInfo fi in this._fileInfos)
            {
                foreach (RectTransform rt in this._rectTransforms)
                {
                    FileInfoComponent component = rt.GetComponent<FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        break;
                    }
                }
            }
        }
    }
}
