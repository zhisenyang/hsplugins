using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using HSUS;
using IllusionUtility.GetUtility;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMenu
{
    public class SmCharaLoadCustom : SmCharaLoad
    {
        private readonly Dictionary<FileInfo, GameObject> _objects = new Dictionary<FileInfo, GameObject>();
        private int _lastMenuType;
        private bool _created;
        private RectTransform _container;
        private List<FileInfo> _fileInfos;
        private List<RectTransform> _rectTransforms;
        private InputField _searchBar;

        public void LoadFrom(SmCharaLoad other)
        {
            this.LoadWith(other);
            this.ReplaceEventsOf(other);

            this._lastMenuType = this.nowSubMenuTypeId;
            this._fileInfos = (List<FileInfo>)this.GetPrivateExplicit<SmCharaLoad>("lstFileInfo");
            this._rectTransforms = ((List<RectTransform>)this.GetPrivateExplicit<SmCharaLoad>("lstRtfTgl"));

            this.customControl = other.customControl;

            foreach (CustomCheck cs in Resources.FindObjectsOfTypeAll<CustomCheck>())
            {
                if (cs.smChaLoad == other)
                {
                    cs.smChaLoad = this;
                    cs.transform.FindDescendant("checkDelete").FindChild("BtnYes").GetComponent<Button>().onClick.AddListener(this.RecreateList);
                    cs.transform.FindDescendant("checkCapture").FindChild("BtnYes").GetComponent<Button>().onClick.AddListener(this.RecreateList);
                }
            }

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

            Type t = Type.GetType("AdditionalBoneModifier.BoneControllerMgr,AdditionalBoneModifier");
            if (t != null)
                t.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null).SetPrivate("loadSubMenu", this);

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
            foreach (Text te in this._searchBar.GetComponentsInChildren<Text>())
                te.color = Color.white;

        }

        private void SearchChanged(string arg0)
        {
            string search = this._searchBar.text.Trim();
            foreach (FileInfo fi in this._fileInfos)
            {
                GameObject obj;
                if (this._objects.TryGetValue(fi, out obj) == false)
                    continue;
                if (fi.noAccess == false)
                    obj.SetActive(fi.CharaName.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1);
                else
                    obj.SetActive(false);
            }
        }

        private void RecreateList()
        {
            this.ExecuteDelayed(() =>
            {
                this._objects.Clear();
                int index = 0;
                foreach (FileInfo fi in this._fileInfos)
                {
                    if (fi.noAccess == false)
                    {
                        Transform t = this.objListTop.transform.GetChild(index);
                        t.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        this._objects.Add(fi, t.gameObject);
                        ++index;
                    }
                }

                ToggleGroup component = this.objListTop.GetComponent<ToggleGroup>();
                for (int i = 0; i < this._fileInfos.Count; i++)
                {
                    FileInfo fi = this._fileInfos[i];
                    if (fi.noAccess)
                    {
                        GameObject gameObject = Instantiate(this.objLineBase);
                        gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                        this._objects.Add(fi, gameObject);
                        FileInfoComponent fileInfoComponent = gameObject.AddComponent<FileInfoComponent>();
                        fileInfoComponent.info = fi;
                        fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                        GameObject gameObject2 = gameObject.transform.FindLoop("Background");
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
                        gameObject.transform.SetSiblingIndex(i);
                        gameObject.transform.localScale = Vector3.one;
                        (gameObject.transform as RectTransform).sizeDelta = new Vector2(this._container.rect.width, 24f);
                        Text component2 = gameObject.transform.FindChild("Label").GetComponent<Text>();
                        component2.text = fileInfoComponent.info.CharaName;
                        this.CallPrivateExplicit<SmCharaLoad>("SetButtonClickHandler", gameObject);
                        gameObject.SetActive(true);
                        this._rectTransforms.Insert(i, gameObject.transform as RectTransform);

                    }
                }
                foreach (FileInfo fi in this._fileInfos)
                    this._objects[fi].SetActive(!fi.noAccess);
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
                this.UpdateSort();
            });
        }

        public override void SetCharaInfoSub()
        {
            if (null == this.chaInfo)
                return;
            if (null == this.objListTop)
                return;
            if (null == this.objLineBase)
                return;
            if (null == this.rtfPanel)
                return;
            this._searchBar.text = "";
            this.SearchChanged("");
            this.Init();
            foreach (FileInfo current in this._fileInfos)
            {
                current.noAccess = false;
            }
            switch (this.nowSubMenuTypeId)
            {
                case 81:
                    this.btn01.SetActive(true);
                    this.btn02.SetActive(true);
                    this.btn03.SetActive(true);
                    this.texBtn01.text = "容姿読込み";
                    this.texBtn02.text = "服セット読込み";
                    this.texBtn03.text = this.customControl.modeCustom == 0 ? "読込み" : "容姿と服読込み";
                    break;
                case 82:
                    this.btn01.SetActive(false);
                    this.btn02.SetActive(true);
                    this.btn03.SetActive(true);
                    this.texBtn01.text = string.Empty;
                    this.texBtn02.text = "新規保存";
                    this.texBtn03.text = "上書き保存";
                    if (this.chaInfo.Sex != 0)
                    {
                        this.DisableEntryCustomFemale();
                    }
                    break;
                case 83:
                    this.btn01.SetActive(false);
                    this.btn02.SetActive(false);
                    this.btn03.SetActive(true);
                    this.texBtn01.text = string.Empty;
                    this.texBtn02.text = string.Empty;
                    this.texBtn03.text = "削除";
                    if (this.chaInfo.Sex != 0)
                    {
                        this.DisableEntryCustomFemale();
                    }
                    break;
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
            if (this._lastMenuType == this.nowSubMenuTypeId)
                return;
            if (this.imgPrev)
                this.imgPrev.enabled = false;
            ToggleGroup component = this.objListTop.GetComponent<ToggleGroup>();
            if (this._created == false)
            {
                foreach (FileInfo fi in this._fileInfos)
                {
                    GameObject gameObject = Instantiate(this.objLineBase);
                    this._objects.Add(fi, gameObject);
                    gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
                    FileInfoComponent fileInfoComponent = gameObject.AddComponent<FileInfoComponent>();
                    fileInfoComponent.info = fi;
                    fileInfoComponent.tgl = gameObject.GetComponent<Toggle>();
                    GameObject gameObject2 = gameObject.transform.FindLoop("Background");
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
                    gameObject.transform.localScale = Vector3.one;
                    (gameObject.transform as RectTransform).sizeDelta = new Vector2(this._container.rect.width, 24f);
                    Text component2 = gameObject.transform.FindChild("Label").GetComponent<Text>();
                    component2.text = fileInfoComponent.info.CharaName;
                    this.CallPrivateExplicit<SmCharaLoad>("SetButtonClickHandler", gameObject);
                    gameObject.SetActive(true);
                    this._rectTransforms.Add(gameObject.transform as RectTransform);
                }
                this._created = true;
            }
            foreach (FileInfo fi in this._fileInfos)
                this._objects[fi].SetActive(!fi.noAccess);
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
            this._lastMenuType = this.nowSubMenuTypeId;
        }


        public new virtual void OnSortName()
        {
            this.SortName(!(bool)this.GetPrivateExplicit<SmCharaLoad>("ascendName"));
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
        }

        public new virtual void SortName(bool ascend)
        {
            this.SetPrivateExplicit<SmCharaLoad>("ascendName", ascend);
            if (this._fileInfos.Count == 0)
            {
                return;
            }
            this.SetPrivateExplicit<SmCharaLoad>("lastSort", (byte)0);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
            {
                this._fileInfos.Sort((a, b) => a.CharaName.CompareTo(b.CharaName));
            }
            else
            {
                this._fileInfos.Sort((a, b) => b.CharaName.CompareTo(a.CharaName));
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                fi.no = i;
                ++i;
            }
            int num = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                foreach (RectTransform rt in this._rectTransforms)
                {
                    if (rt.gameObject.activeSelf == false)
                        continue;
                    FileInfoComponent component = rt.GetComponent<FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        num++;
                        break;
                    }
                }
            }
        }

        public new virtual void OnSortDate()
        {
            this.SortDate(!(bool)this.GetPrivateExplicit<SmCharaLoad>("ascendDate"));
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.rtfPanel);
        }

        public new virtual void SortDate(bool ascend)
        {
            this.SetPrivateExplicit<SmCharaLoad>("ascendDate", ascend);
            if (this._fileInfos.Count == 0)
            {
                return;
            }
            this.SetPrivateExplicit<SmCharaLoad>("lastSort", (byte)1);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            if (ascend)
            {
                this._fileInfos.Sort((a, b) => a.time.CompareTo(b.time));
            }
            else
            {
                this._fileInfos.Sort((a, b) => b.time.CompareTo(a.time));
            }
            Thread.CurrentThread.CurrentCulture = currentCulture;
            int i = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                fi.no = i;
                ++i;
            }
            int num = 0;
            foreach (FileInfo fi in this._fileInfos)
            {
                foreach (RectTransform rt in this._rectTransforms)
                {
                    if (rt.gameObject.activeSelf == false)
                        continue;
                    FileInfoComponent component = rt.GetComponent<FileInfoComponent>();
                    if (component.info == fi)
                    {
                        rt.SetAsLastSibling();
                        num++;
                        break;
                    }
                }
            }
        }

        public new virtual void UpdateSort()
        {
            if ((byte)this.GetPrivateExplicit<SmCharaLoad>("lastSort") == 0)
            {
                this.SortDate((bool)this.GetPrivateExplicit<SmCharaLoad>("ascendDate"));
                this.SortName((bool)this.GetPrivateExplicit<SmCharaLoad>("ascendName"));
            }
            else
            {
                this.SortName((bool)this.GetPrivateExplicit<SmCharaLoad>("ascendName"));
                this.SortDate((bool)this.GetPrivateExplicit<SmCharaLoad>("ascendDate"));
            }
        }
    }
}
