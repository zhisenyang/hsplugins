using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using ChaCustom;
using Harmony;
using Illusion.Extensions;
using StrayTech;
using TMPro;
using UILib;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoreAccessoriesKOI
{
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.moreaccessories", Name: "MoreAccessories", Version: versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    public class MoreAccessories : BaseUnityPlugin
    {
        public const string versionNum = "1.0.0";

        #region Private Types
        internal class CharAdditionalData
        {
            public List<ChaFileAccessory.PartsInfo> nowAccessories;
            public List<ListInfoBase> infoAccessory = new List<ListInfoBase>();
            public List<GameObject> objAccessory = new List<GameObject>();
            public List<GameObject[]> objAcsMove = new List<GameObject[]>();
            public List<ChaAccessoryComponent> cusAcsCmp = new List<ChaAccessoryComponent>();

            public Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = new Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>();
        }

        internal class CharaMakerSlotData
        {
            public Toggle toggle;
            public CanvasGroup canvasGroup;
            public TextMeshProUGUI text;
            public CvsAccessory cvsAccessory;

            public GameObject copySlotObject;
            public Toggle copyToggle;
            public TextMeshProUGUI copySourceText;
            public TextMeshProUGUI copyDestinationText;

            public GameObject transferSlotObject;
            public Toggle transferSourceToggle;
            public Toggle transferDestinationToggle;
            public TextMeshProUGUI transferSourceText;
            public TextMeshProUGUI transferDestinationText;
        }

        private enum Binary
        {
            Game,
            Studio
        }
        #endregion

        #region Private Variables
        internal static MoreAccessories _self;
        private GameObject _charaMakerSlotTemplate;
        private ScrollRect _charaMakerScrollView;
        internal CustomAcsChangeSlot _customAcsChangeSlot;
        internal CustomAcsParentWindow _customAcsParentWin;
        internal CustomAcsMoveWindow[] _customAcsMoveWin;
        internal CustomAcsSelectKind[] _customAcsSelectKind;
        internal CvsAccessory[] _cvsAccessory;
        internal List<CharaMakerSlotData> _additionalCharaMakerSlots;
        internal readonly Dictionary<ChaFile, CharAdditionalData> _accessoriesByChar = new Dictionary<ChaFile, CharAdditionalData>();
        internal CharAdditionalData _charaMakerData = null;
        private Vector3 _slotUIWorldPosition;
        private int _selectedSlot = 0;
        internal bool _inCharaMaker = false;
        private Binary _binary;
        private RectTransform _addButtonsGroup;
        private ScrollRect _charaMakerCopyScrollView;
        private GameObject _copySlotTemplate;
        private ScrollRect _charaMakerTransferScrollView;
        private GameObject _transferSlotTemplate;
        private List<UI_RaycastCtrl> _raycastCtrls = new List<UI_RaycastCtrl>();
        #endregion

        #region Unity Methods
        void Awake()
        {
            SceneManager.sceneLoaded += this.SceneLoaded;
            _self = this;
            switch (BepInEx.Paths.ProcessName)
            {
                case "Koikatu":
                    this._binary = Binary.Game;
                    break;
                case "CharaStudio":
                    this._binary = Binary.Studio;
                    break;
            }
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.kkplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ChaControl_ChangeAccessory_Patches.ManualPatch(harmony);
        }

        public void OnApplicationQuit()
        {

        }

        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (this._binary == Binary.Game)
            {
                if (loadMode == LoadSceneMode.Single)
                {
                    if (scene.buildIndex == 2) //Chara Maker
                    {
                        this._selectedSlot = 0;
                        this._additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                        this._raycastCtrls = new List<UI_RaycastCtrl>();
                        this._inCharaMaker = true;
                    }
                    else
                        this._inCharaMaker = false;
                }
            }
            else
            {
                
            }
        }

        void Update()
        {
            if (this._inCharaMaker && this._customAcsChangeSlot != null)
            {
                if (this._selectedSlot < 20)
                    this._customAcsChangeSlot.items[this._selectedSlot].cgItem.transform.position = this._slotUIWorldPosition;
                else
                    this._additionalCharaMakerSlots[this._selectedSlot - 20].canvasGroup.transform.position = this._slotUIWorldPosition;
                if (Singleton<CustomBase>.Instance.updateCustomUI)
                {
                    for (int i = 0; i < this._additionalCharaMakerSlots.Count; i++)
                    {
                        CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                        if (slot.toggle.gameObject.activeSelf == false)
                            continue;
                        if (i + 20 == CustomBase.Instance.selectSlot)
                            slot.cvsAccessory.UpdateCustomUI();
                        slot.cvsAccessory.UpdateSlotName();
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        internal void SpawnMakerUI()
        {
            RectTransform container = (RectTransform)GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop").transform;
            this._charaMakerScrollView = UIUtility.CreateScrollView("Slots", container);
            this._charaMakerScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerScrollView.horizontal = false;
            this._charaMakerScrollView.scrollSensitivity = 18f;
            if (this._charaMakerScrollView.horizontalScrollbar != null)
                Destroy(this._charaMakerScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerScrollView.verticalScrollbar != null)
                Destroy(this._charaMakerScrollView.verticalScrollbar.gameObject);
            Destroy(this._charaMakerScrollView.GetComponent<Image>());
            LayoutElement element = this._charaMakerScrollView.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 832;
            element.minWidth = 600f;
            VerticalLayoutGroup group = this._charaMakerScrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
            VerticalLayoutGroup parentGroup = container.GetComponent<VerticalLayoutGroup>();
            group.childAlignment = parentGroup.childAlignment;
            group.childControlHeight = parentGroup.childControlHeight;
            group.childControlWidth = parentGroup.childControlWidth;
            group.childForceExpandHeight = parentGroup.childForceExpandHeight;
            group.childForceExpandWidth = parentGroup.childForceExpandWidth;
            group.spacing = parentGroup.spacing;
            this._charaMakerScrollView.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            this._charaMakerSlotTemplate = container.GetChild(0).gameObject;
            this._slotUIWorldPosition = this._charaMakerSlotTemplate.transform.GetChild(1).position;
            for (int i = 0; i < 20; i++)
                container.GetChild(0).SetParent(this._charaMakerScrollView.content);
            this._charaMakerScrollView.transform.SetAsFirstSibling();
            Toggle toggleCopy = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglCopy").GetComponent<Toggle>();
            this._addButtonsGroup = UIUtility.CreateNewUIObject(this._charaMakerScrollView.content, "Add Buttons Group");
            element = this._addButtonsGroup.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 224f;
            element.preferredHeight = 32f;
            GameObject textModel = toggleCopy.transform.Find("imgOff").GetComponentInChildren<TextMeshProUGUI>().gameObject;

            Button addOneButton = UIUtility.CreateButton("Add One Button", this._addButtonsGroup, "+1");
            addOneButton.transform.SetRect(Vector2.zero, new Vector2(0.5f, 1f));
            addOneButton.colors = toggleCopy.colors;
            ((Image)addOneButton.targetGraphic).sprite = toggleCopy.transform.Find("imgOff").GetComponent<Image>().sprite;
            Destroy(addOneButton.GetComponentInChildren<Text>().gameObject);
            TextMeshProUGUI text = GameObject.Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addOneButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+1";
            addOneButton.onClick.AddListener(this.AddSlot);

            Button addTenButton = UIUtility.CreateButton("Add Ten Button", this._addButtonsGroup, "+10");
            addTenButton.transform.SetRect(new Vector2(0.5f, 0f), Vector2.one);
            addTenButton.colors = toggleCopy.colors;
            ((Image)addTenButton.targetGraphic).sprite = toggleCopy.transform.Find("imgOff").GetComponent<Image>().sprite;
            Destroy(addTenButton.GetComponentInChildren<Text>().gameObject);
            text = GameObject.Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addTenButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+10";
            addTenButton.onClick.AddListener(this.AddTenSlot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            for (int i = 0; i < this._customAcsChangeSlot.items.Length; i++)
            {
                UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                info.tglItem.onValueChanged = new Toggle.ToggleEvent();
                if (i < 20)
                {
                    int i1 = i;
                    info.tglItem.onValueChanged.AddListener(b =>
                    {
                        this.AccessorySlotToggleCallback(i1, info.tglItem);
                        this.AccessorySlotCanvasGroupCallback(i1, info.tglItem, info.cgItem);
                    });
                }
                else if (i == 20)
                {
                    info.tglItem.onValueChanged.AddListener(b =>
                    {
                        if (info.tglItem.isOn)
                        {
                            this._customAcsChangeSlot.CloseWindow();
                            CustomBase.Instance.updateCvsAccessoryCopy = true;
                        }
                        this.AccessorySlotCanvasGroupCallback(-1, info.tglItem, info.cgItem);
                    });
                   ((RectTransform)info.cgItem.transform).anchoredPosition += new Vector2(0f, 40f);
                }
                else if (i == 21)
                {
                    info.tglItem.onValueChanged.AddListener(b =>
                    {
                        if (info.tglItem.isOn)
                        {
                            this._customAcsChangeSlot.CloseWindow();
                            Singleton<CustomBase>.Instance.updateCvsAccessoryChange = true;
                        }
                        this.AccessorySlotCanvasGroupCallback(-2, info.tglItem, info.cgItem);
                    });
                    ((RectTransform)info.cgItem.transform).anchoredPosition += new Vector2(0f, 40f);
                }
            }


            container = (RectTransform)GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglCopy/CopyTop/rect").transform;
            this._charaMakerCopyScrollView = UIUtility.CreateScrollView("Slots", container);
            this._charaMakerCopyScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerCopyScrollView.horizontal = false;
            this._charaMakerCopyScrollView.scrollSensitivity = 18f;
            if (this._charaMakerCopyScrollView.horizontalScrollbar != null)
                Destroy(this._charaMakerCopyScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerCopyScrollView.verticalScrollbar != null)
                Destroy(this._charaMakerCopyScrollView.verticalScrollbar.gameObject);
            Destroy(this._charaMakerCopyScrollView.GetComponent<Image>());
            RectTransform content = (RectTransform)container.Find("grpClothes");
            this._charaMakerCopyScrollView.transform.SetRect(content);
            content.SetParent(this._charaMakerCopyScrollView.viewport);
            Destroy(this._charaMakerCopyScrollView.content.gameObject);
            this._charaMakerCopyScrollView.content = content;
            this._copySlotTemplate = this._charaMakerCopyScrollView.content.GetChild(0).gameObject;
            this._raycastCtrls.Add(container.parent.GetComponent<UI_RaycastCtrl>());
            this._charaMakerCopyScrollView.transform.SetAsFirstSibling();

            container = (RectTransform)GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/tglChange/ChangeTop/rect").transform;
            this._charaMakerTransferScrollView = UIUtility.CreateScrollView("Slots", container);
            this._charaMakerTransferScrollView.movementType = ScrollRect.MovementType.Clamped;
            this._charaMakerTransferScrollView.horizontal = false;
            this._charaMakerTransferScrollView.scrollSensitivity = 18f;
            if (this._charaMakerTransferScrollView.horizontalScrollbar != null)
                Destroy(this._charaMakerTransferScrollView.horizontalScrollbar.gameObject);
            if (this._charaMakerTransferScrollView.verticalScrollbar != null)
                Destroy(this._charaMakerTransferScrollView.verticalScrollbar.gameObject);
            Destroy(this._charaMakerTransferScrollView.GetComponent<Image>());
            content = (RectTransform)container.Find("grpClothes");
            this._charaMakerTransferScrollView.transform.SetRect(content);
            content.SetParent(this._charaMakerTransferScrollView.viewport);
            Destroy(this._charaMakerTransferScrollView.content.gameObject);
            this._charaMakerTransferScrollView.content = content;
            this._transferSlotTemplate = this._charaMakerTransferScrollView.content.GetChild(0).gameObject;
            this._raycastCtrls.Add(container.parent.GetComponent<UI_RaycastCtrl>());
            this._charaMakerTransferScrollView.transform.SetAsFirstSibling();

            this._charaMakerScrollView.viewport.gameObject.SetActive(false);

            this.ExecuteDelayed(() => //Fixes problems with UI masks overlapping and creating bugs
            {
                this._charaMakerScrollView.viewport.gameObject.SetActive(true);
            }, 5);
        }

        private void UpdateMakerUI()
        {
            if (this._customAcsChangeSlot == null)
                return;
            int count = this._charaMakerData.nowAccessories != null ? this._charaMakerData.nowAccessories.Count : 0;
            int i;
            for (i = 0; i < count; i++)
            {
                if (i < this._additionalCharaMakerSlots.Count)
                {
                    CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                    slot.toggle.gameObject.SetActive(true);
                    if (i + 20 == CustomBase.Instance.selectSlot)
                        slot.cvsAccessory.UpdateCustomUI();
                    slot.cvsAccessory.UpdateSlotName();

                    slot.transferSlotObject.SetActive(true);
                }
                else
                {
                    GameObject newSlot = Instantiate(this._charaMakerSlotTemplate, this._charaMakerScrollView.content);
                    CharaMakerSlotData info = new CharaMakerSlotData();
                    info.toggle = newSlot.GetComponent<Toggle>();
                    info.text = info.toggle.GetComponentInChildren<TextMeshProUGUI>();
                    info.canvasGroup = info.toggle.transform.GetChild(1).GetComponent<CanvasGroup>();
                    info.cvsAccessory = info.toggle.GetComponentInChildren<CvsAccessory>();
                    info.toggle.onValueChanged = new Toggle.ToggleEvent();
                    info.toggle.isOn = false;
                    int index = i + 20;
                    info.toggle.onValueChanged.AddListener(b =>
                    {
                        this.AccessorySlotToggleCallback(index, info.toggle);
                        this.AccessorySlotCanvasGroupCallback(index, info.toggle, info.canvasGroup);
                    });
                    info.text.text = $"スロット{index + 1:00}";
                    info.cvsAccessory.slotNo = (CvsAccessory.AcsSlotNo)index;
                    newSlot.name = "tglSlot" + (index + 1).ToString("00");
                    info.canvasGroup.Enable(false, false);

                    info.copySlotObject = Instantiate(this._copySlotTemplate, this._charaMakerCopyScrollView.content);
                    info.copyToggle = info.copySlotObject.GetComponentInChildren<Toggle>();
                    info.copySourceText = info.copySlotObject.transform.Find("srcText00").GetComponent<TextMeshProUGUI>();
                    info.copyDestinationText = info.copySlotObject.transform.Find("dstText00").GetComponent<TextMeshProUGUI>();
                    info.copyToggle.GetComponentInChildren<TextMeshProUGUI>().text = (index + 1).ToString("00");
                    info.copySourceText.text = "なし";
                    info.copyDestinationText.text = "なし";
                    info.copyToggle.onValueChanged = new Toggle.ToggleEvent();
                    info.copyToggle.isOn = false;
                    info.copyToggle.interactable = true;
                    info.copySlotObject.name = "kind" + index.ToString("00");
                    info.copyToggle.graphic.raycastTarget = true;

                    info.transferSlotObject = Instantiate(this._transferSlotTemplate, this._charaMakerTransferScrollView.content);
                    info.transferSourceToggle = info.transferSlotObject.transform.GetChild(1).GetComponentInChildren<Toggle>();
                    info.transferDestinationToggle = info.transferSlotObject.transform.GetChild(2).GetComponentInChildren<Toggle>();
                    info.transferSourceText = info.transferSourceToggle.GetComponentInChildren<TextMeshProUGUI>();
                    info.transferDestinationText = info.transferDestinationToggle.GetComponentInChildren<TextMeshProUGUI>();
                    info.transferSlotObject.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = (index + 1).ToString("00");
                    info.transferSourceText.text = "なし";
                    info.transferDestinationText.text = "なし";
                    info.transferSlotObject.name = "kind" + index.ToString("00");
                    info.transferSourceToggle.onValueChanged = new Toggle.ToggleEvent();
                    info.transferSourceToggle.onValueChanged.AddListener((b) =>
                    {
                        if (info.transferSourceToggle.isOn)
                            CvsAccessory_Patches.CvsAccessoryChange_Start_Patches.SetSourceIndex(index);
                    });
                    info.transferDestinationToggle.onValueChanged = new Toggle.ToggleEvent();
                    info.transferDestinationToggle.onValueChanged.AddListener((b) =>
                    {
                        if (info.transferDestinationToggle.isOn)
                            CvsAccessory_Patches.CvsAccessoryChange_Start_Patches.SetDestinationIndex(index);
                    });
                    info.transferSourceToggle.isOn = false;
                    info.transferDestinationToggle.isOn = false;
                    info.transferSourceToggle.graphic.raycastTarget = true;
                    info.transferDestinationToggle.graphic.raycastTarget = true;

                    this._additionalCharaMakerSlots.Add(info);
                }
            }
            for (; i < this._additionalCharaMakerSlots.Count; i++)
            {
                CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                slot.toggle.gameObject.SetActive(false);
                slot.toggle.isOn = false;
                slot.transferSlotObject.SetActive(false);
            }
            //foreach (UI_RaycastCtrl ctrl in this._raycastCtrls)
            //{
            //    ctrl.Reset();
            //}
        }

        private void AddSlot()
        {
            if (_self._accessoriesByChar.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out this._charaMakerData) == false)
            {
                this._charaMakerData = new CharAdditionalData();
                this._accessoriesByChar.Add(CustomBase.Instance.chaCtrl.chaFile, this._charaMakerData);
            }
            if (this._charaMakerData.nowAccessories == null)
            {
                this._charaMakerData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                this._charaMakerData.rawAccessoriesInfos.Add((ChaFileDefine.CoordinateType)CustomBase.Instance.chaCtrl.fileStatus.coordinateType, this._charaMakerData.nowAccessories);
            }
            ChaFileAccessory.PartsInfo partsInfo = new ChaFileAccessory.PartsInfo();
            partsInfo.MemberInit();
            this._charaMakerData.nowAccessories.Add(partsInfo);
            while (this._charaMakerData.infoAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.infoAccessory.Add(null);
            while (this._charaMakerData.objAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAccessory.Add(null);
            while (this._charaMakerData.objAcsMove.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAcsMove.Add(new GameObject[2]);
            while (this._charaMakerData.cusAcsCmp.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.cusAcsCmp.Add(null);
            this.UpdateMakerUI();
            this._addButtonsGroup.SetAsLastSibling();
        }

        private void AddTenSlot()
        {
            if (_self._accessoriesByChar.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out this._charaMakerData) == false)
            {
                this._charaMakerData = new CharAdditionalData();
                this._accessoriesByChar.Add(CustomBase.Instance.chaCtrl.chaFile, this._charaMakerData);
            }
            if (this._charaMakerData.nowAccessories == null)
            {
                this._charaMakerData.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                this._charaMakerData.rawAccessoriesInfos.Add((ChaFileDefine.CoordinateType)CustomBase.Instance.chaCtrl.fileStatus.coordinateType, this._charaMakerData.nowAccessories);
            }
            for (int i = 0; i < 10; i++)
            {
                ChaFileAccessory.PartsInfo partsInfo = new ChaFileAccessory.PartsInfo();
                partsInfo.MemberInit();
                this._charaMakerData.nowAccessories.Add(partsInfo);
            }
            while (this._charaMakerData.infoAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.infoAccessory.Add(null);
            while (this._charaMakerData.objAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAccessory.Add(null);
            while (this._charaMakerData.objAcsMove.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAcsMove.Add(new GameObject[2]);
            while (this._charaMakerData.cusAcsCmp.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.cusAcsCmp.Add(null);
            this.UpdateMakerUI();
            this._addButtonsGroup.SetAsLastSibling();
        }

        private void AccessorySlotToggleCallback(int index, Toggle toggle)
        {
            if (toggle.isOn)
            {
                this._selectedSlot = index;
                bool open = this.GetPart(index).type != 120;
                this._customAcsParentWin.ChangeSlot(index, open);
                foreach (CustomAcsMoveWindow customAcsMoveWindow in this._customAcsMoveWin)
                    customAcsMoveWindow.ChangeSlot(index, open);
                foreach (CustomAcsSelectKind customAcsSelectKind in this._customAcsSelectKind)
                    customAcsSelectKind.ChangeSlot(index, open);

                Singleton<CustomBase>.Instance.selectSlot = index;
                if (index < 20)
                    Singleton<CustomBase>.Instance.SetUpdateCvsAccessory(index, true);
                else
                {
                    CvsAccessory accessory = this.GetCvsAccessory(index);
                    if (index == CustomBase.Instance.selectSlot)
                        accessory.UpdateCustomUI();
                    accessory.UpdateSlotName();
                }
                if ((int)this._customAcsChangeSlot.GetPrivate("backIndex") != index)
                    this._customAcsChangeSlot.ChangeColorWindow(index);
                this._customAcsChangeSlot.SetPrivate("backIndex", index);
            }
        }

        private void AccessorySlotCanvasGroupCallback(int index, Toggle toggle, CanvasGroup canvasGroup)
        {
            //if (toggle.isOn == false)
            {
                for (int i = 0; i < this._customAcsChangeSlot.items.Length; i++)
                {
                    UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                    //if (info == null || i == index || (i == 20 && index == -1) || (i == 21 && index == -2))
                    //    continue;
                    if (info.cgItem != null)
                        info.cgItem.Enable(false, false);
                }
                for (int i = 0; i < this._additionalCharaMakerSlots.Count; i++)
                {
                    CharaMakerSlotData info = this._additionalCharaMakerSlots[i];
                    if (i + 20 == index || info == null)
                        continue;
                    if (info.canvasGroup != null)
                        info.canvasGroup.Enable(false, false);
                }
            }
            if (toggle.isOn && canvasGroup)
                canvasGroup.Enable(true, false);
        }

        internal void OnCoordTypeChangeCharaMaker()
        {
            this.UpdateMakerUI();
            if (this._selectedSlot >= 20)
            {
                if (this._additionalCharaMakerSlots[this._selectedSlot - 20].toggle.gameObject.activeSelf == false)
                {
                    Toggle toggle = this._customAcsChangeSlot.items[0].tglItem;
                    toggle.isOn = true;
                    this._selectedSlot = 0;
                }
            }
        }

        internal int GetSelectedMakerIndex()
        {
            for (int i = 0; i < 20; i++)
            {
                UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                if (info.tglItem.isOn)
                    return i;
            }
            for (int i = 0; i < this._additionalCharaMakerSlots.Count; i++)
            {
                CharaMakerSlotData slot = this._additionalCharaMakerSlots[i];
                if (slot.toggle.isOn)
                    return i + 20;
            }
            return -1;
        }

        internal ChaFileAccessory.PartsInfo GetPart(int index)
        {
            if (index < 20)
                return CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts[index];
            return this._charaMakerData.nowAccessories[index - 20];
        }

        internal void SetPart(int index, ChaFileAccessory.PartsInfo value)
        {
            if (index < 20)
                CustomBase.Instance.chaCtrl.nowCoordinate.accessory.parts[index] = value;
            else
                this._charaMakerData.nowAccessories[index - 20] = value;
        }

        internal int GetPartsLength()
        {
            return this._charaMakerData.nowAccessories.Count + 20;
        }

        internal CvsAccessory GetCvsAccessory(int index)
        {
            if (index < 20)
                return this._cvsAccessory[index];
            return this._additionalCharaMakerSlots[index - 20].cvsAccessory;
        }
        #endregion
    }
}
