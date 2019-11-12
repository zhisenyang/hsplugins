using AIChara;
using BepInEx;
using CharaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using MoreAccessoriesAI.Patches;
using Sideloader.AutoResolver;
using Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using TMPro;
using ToolBox;
using ToolBox.Extensions;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessoriesAI
{
    [BepInPlugin(_guid, _name, _version)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInDependency("com.bepis.bepinex.sideloader")]
    internal class MoreAccessories : GenericPlugin
    {
        #region Private Types
        internal class AdditionalData
        {
            public class AccessoryObject
            {
                public GameObject obj = null;
                public ListInfoBase info = null;
                public CmpAccessory cmp = null;
                public bool show = true;
                public Transform[] move = new Transform[2];
            }

            public List<ChaFileAccessory.PartsInfo> parts = new List<ChaFileAccessory.PartsInfo>();
            public List<AccessoryObject> objects = new List<AccessoryObject>();

        }

        internal class MakerSlot
        {
            public UI_ButtonEx slotButton;
            public Text slotText;

            public UI_ToggleEx copySrcToggle;
            public Text copySrcText;

            public UI_ToggleEx copyDstToggle;
            public Text copyDstText;
        }

        internal class StudioSlot
        {
            public GameObject slot;
            public TextMeshProUGUI slotText;
            public Button onButton;
            public Button offButton;
        }
        #endregion

        #region Private Variables
        private const string _name = "MoreAccessories";
        private const string _version = "1.1.1";
        private const string _guid = "com.joan6694.illusionplugins.moreaccessories";
        private const string _extSaveKey = "moreAccessories";
        private const int _saveVersion = 0;

        private volatile Thread _patchThread = null;
        internal static MoreAccessories _self;
        internal readonly Dictionary<ChaFile, AdditionalData> _charAdditionalData = new Dictionary<ChaFile, AdditionalData>();
        internal bool _loadAdditionalAccessories = true;

        #region Sideloader
        private object _sideloaderChaFileAccessoryPartsInfoProperties;
        #endregion

        #region Maker
        private RectTransform _makerListTop;
        private GameObject _makerButtonTemplate;
        private RectTransform _makerAddButtons;
        internal readonly List<MakerSlot> _makerSlots = new List<MakerSlot>();
        internal AdditionalData _makerAdditionalData = new AdditionalData();
        private CvsSelectWindow _makerSelectWindow;
        private CvsA_Slot _makerCanvasAccessories;
        private CvsA_Copy _makerCanvasAccessoriesCopy;
        private CanvasGroup _makerCanvasGroupSettingWindow;
        internal ChaFile _overrideChaFile;
        internal ChaFileCoordinate _overrideChaFileCoordinate;
        private RectTransform _makerListCopySrcTop;
        private RectTransform _makerListCopyDstTop;
        private GameObject _makerCopySrcToggleTemplate;
        private GameObject _makerCopyDstToggleTemplate;
        private bool _inMaker = false;
        #endregion

        #region Studio
        private RectTransform _studioListTop;
        private GameObject _studioToggleTemplate;
        private StudioSlot _studioToggleAll;
        private OCIChar _selectedStudioCharacter;
        internal readonly List<StudioSlot> _studioSlots = new List<StudioSlot>();
        #endregion
        #endregion

        #region Accessors

        #endregion

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();
            _self = this;
            
            Harmony harmony = new Harmony(_guid);

            harmony.Patch(typeof(UniversalAutoResolver).GetMethod("IterateCoordinatePrefixes", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_IterateCoordinatePrefixes_Postfix)));
            Type uarHooks = typeof(UniversalAutoResolver).GetNestedType("Hooks", BindingFlags.NonPublic | BindingFlags.Static);
            harmony.Patch(uarHooks.GetMethod("ExtendedCardLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCardSave", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCardSave_Postfix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateLoad", AccessTools.all), new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordLoad_Prefix)));
            harmony.Patch(uarHooks.GetMethod("ExtendedCoordinateSave", AccessTools.all), postfix: new HarmonyMethod(typeof(MoreAccessories), nameof(UAR_ExtendedCoordSave_Postfix)));

            this.ExecuteDelayed(() =>
            {
                this._patchThread = new Thread(this.PatchAll);
                this._patchThread.Start(harmony);
            }, 3f, false);
        }

        private void PatchAll(object arg)
        {
            Harmony harmony = (Harmony)arg;

            CvsA_Slot_Patches.PatchAll(harmony);
            CvsA_Copy_Patches.PatchAll(harmony);
            ChaControl_Patches.PatchAll(harmony);
            CustomAcsCorrectSet_Patches.PatchAll(harmony);
            Various.PatchAll(harmony);

            this._patchThread = null;
        }

        protected override void LevelLoaded(int level)
        {
            switch (this._binary)
            {
                case Binary.Game:
                    this._inMaker = false;
                    if (level == 4)
                        this.SpawnMakerUI();
                    break;
                case Binary.Studio:
                    if (level == 2)
                        this.SpawnStudioUI();
                    break;
            }
        }

        protected override void Update()
        {
            if (this._binary == Binary.Studio && Studio.Studio.Instance != null)
            {
                TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject != null)
                {
                    ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    {
                        OCIChar selected = info as OCIChar;
                        if (selected != this._selectedStudioCharacter)
                        {
                            this._selectedStudioCharacter = selected;
                            this.UpdateStudioUI();
                        }
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (this._patchThread != null)
                this._patchThread.Abort();
        }
        #endregion

        #region Maker
        private void SpawnMakerUI()
        {
            this._makerSelectWindow = GameObject.Find("CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuAccessory").GetComponent<CvsSelectWindow>();
            this._makerCanvasAccessories = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory/A_Slot").GetComponent<CvsA_Slot>();
            this._makerCanvasAccessoriesCopy = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory/A_Copy").GetComponent<CvsA_Copy>();
            this._makerCanvasGroupSettingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow").GetComponent<CanvasGroup>();

            this._makerListTop = (RectTransform)this._makerSelectWindow.transform.Find("Scroll View/Viewport/Content/Category/CategoryTop");
            this._makerButtonTemplate = this._makerListTop.Find("Slot01").gameObject;

            this._makerAddButtons = UIUtility.CreateNewUIObject("AddButtons", this._makerListTop.transform);
            this._makerAddButtons.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            this._makerAddButtons.sizeDelta = new Vector2(100, 32); //Because apparently having a LayoutElement isn't enough

            UI_ButtonEx addOneButton = Instantiate(this._makerButtonTemplate).GetComponent<UI_ButtonEx>();
            addOneButton.name = "AddOne";
            addOneButton.transform.SetParent(this._makerAddButtons);
            addOneButton.transform.localPosition = Vector3.zero;
            addOneButton.transform.localScale = Vector3.one;
            addOneButton.transform.SetRect(0f, 0f, 0.5f, 1f);
            Text t = addOneButton.GetComponentInChildren<Text>();
            t.alignByGeometry = true;
            t.resizeTextForBestFit = true;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = "+1";
            addOneButton.onClick = new Button.ButtonClickedEvent();
            addOneButton.onClick.AddListener(this.AddOneSlot);

            UI_ButtonEx addTenButton = Instantiate(this._makerButtonTemplate).GetComponent<UI_ButtonEx>();
            addTenButton.name = "AddTen";
            addTenButton.transform.SetParent(this._makerAddButtons);
            addTenButton.transform.localPosition = Vector3.zero;
            addTenButton.transform.localScale = Vector3.one;
            addTenButton.transform.SetRect(0.5f, 0f, 1f, 1f);
            t = addTenButton.GetComponentInChildren<Text>();
            t.alignByGeometry = true;
            t.resizeTextForBestFit = true;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = "+10";
            addTenButton.onClick = new Button.ButtonClickedEvent();
            addTenButton.onClick.AddListener(this.AddTenSlots);

            this._makerListCopySrcTop = (RectTransform)this._makerCanvasAccessoriesCopy.transform.Find("Setting/CopyList/SrcTop/Scroll View/Viewport/Content");
            this._makerListCopyDstTop = (RectTransform)this._makerCanvasAccessoriesCopy.transform.Find("Setting/CopyList/dstTop/Scroll View/Viewport/Content");
            this._makerCopySrcToggleTemplate = this._makerListCopySrcTop.Find("Toggle01").gameObject;
            this._makerCopyDstToggleTemplate = this._makerListCopyDstTop.Find("Toggle01").gameObject;

            this._inMaker = true;
            
            this.ExecuteDelayed(() => CustomBase.Instance == null, this.UpdateUI);
        }

        private bool CheckMakerAdditionalData()
        {
            if (CustomBase.Instance != null && CustomBase.Instance.chaCtrl != null)
            {
                if (this._charAdditionalData.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out this._makerAdditionalData) == false)
                {
                    this._makerAdditionalData = new AdditionalData();
                    this._charAdditionalData.Add(CustomBase.Instance.chaCtrl.chaFile, this._makerAdditionalData);
                }
                return true;
            }
            return false;
        }

        private void UpdateMakerUI()
        {
            if (this.CheckMakerAdditionalData() == false)
                return;

            int i = 0;
            for (; i < this._makerAdditionalData.parts.Count; ++i)
            {
                MakerSlot slot;
                if (i < this._makerSlots.Count)
                    slot = this._makerSlots[i];
                else
                {
                    int index = i + 20;

                    slot = new MakerSlot();
                    slot.slotButton = Instantiate(this._makerButtonTemplate).GetComponent<UI_ButtonEx>();
                    slot.slotText = slot.slotButton.GetComponentInChildren<Text>();

                    slot.slotButton.transform.SetParent(this._makerListTop);
                    slot.slotButton.transform.localPosition = Vector3.zero;
                    slot.slotButton.transform.localScale = Vector3.one;
                    slot.slotButton.transform.SetAsLastSibling();
                    slot.slotButton.name = $"Slot{index}";
                    slot.slotButton.onClick = new Button.ButtonClickedEvent();
                    slot.slotButton.onClick.AddListener(() =>
                    {
                        this._makerCanvasAccessoriesCopy.GetComponent<CanvasGroup>().Enable(false, false);
                        this._makerCanvasAccessories.GetComponent<CanvasGroup>().Enable(true, false);
                        this._makerCanvasGroupSettingWindow.Enable(true, false);
                        if ((int)this._makerSelectWindow.GetPrivateExplicit("backSelect") != index)
                        {
                            this._makerCanvasAccessories.titleText.text = slot.slotText.text;
                            this._makerCanvasAccessories.SNo = index;
                            this._makerCanvasAccessories.UpdateCustomUI();
                            this._makerCanvasAccessories.ChangeMenuFunc();
                            Singleton<CustomBase>.Instance.customColorCtrl.Close();
                            this._makerSelectWindow.SetPrivateExplicit("backSelect", index);
                            Singleton<CustomBase>.Instance.ChangeAcsSlotColor(index);
                        }
                    });
                    slot.slotText.text = (index + 1).ToString();
                    slot.slotText.color = new Color32(235, 226, 215, 255);

                    slot.copySrcToggle = Instantiate(this._makerCopySrcToggleTemplate).GetComponent<UI_ToggleEx>();
                    slot.copySrcText = slot.copySrcToggle.transform.Find("AcsName").GetComponent<Text>();

                    slot.copySrcToggle.transform.SetParent(this._makerListCopySrcTop);
                    slot.copySrcToggle.transform.localPosition = Vector3.zero;
                    slot.copySrcToggle.transform.localScale = Vector3.one;
                    slot.copySrcToggle.transform.SetAsLastSibling();
                    slot.copySrcToggle.name = $"Toggle{index}";
                    slot.copySrcToggle.onValueChanged = new Toggle.ToggleEvent();
                    slot.copySrcToggle.isOn = false;
                    slot.copySrcToggle.onValueChanged.AddListener(b =>
                    {
                        if (slot.copySrcToggle.isOn)
                            this._makerCanvasAccessoriesCopy.SetPrivate("selSrc", index);
                    });
                    slot.copySrcText.color = new Color32(235, 226, 215, 255);
                    Text t = slot.copySrcToggle.transform.Find("TextNo").GetComponent<Text>();
                    t.text = (index + 1).ToString();
                    t.color = new Color32(235, 226, 215, 255);

                    slot.copyDstToggle = Instantiate(this._makerCopyDstToggleTemplate).GetComponent<UI_ToggleEx>();
                    slot.copyDstText = slot.copyDstToggle.transform.Find("AcsName").GetComponent<Text>();

                    slot.copyDstToggle.transform.SetParent(this._makerListCopyDstTop);
                    slot.copyDstToggle.transform.localPosition = Vector3.zero;
                    slot.copyDstToggle.transform.localScale = Vector3.one;
                    slot.copyDstToggle.transform.SetAsLastSibling();
                    slot.copyDstToggle.name = $"Toggle{index}";
                    slot.copyDstToggle.onValueChanged = new Toggle.ToggleEvent();
                    slot.copyDstToggle.isOn = false;
                    slot.copyDstToggle.onValueChanged.AddListener(b =>
                    {
                        if (slot.copyDstToggle.isOn)
                            this._makerCanvasAccessoriesCopy.SetPrivate("selDst", index);
                    });
                    slot.copyDstText.color = new Color32(235, 226, 215, 255);
                    t = slot.copyDstToggle.transform.Find("TextNo").GetComponent<Text>();
                    t.text = (index + 1).ToString();
                    t.color = new Color32(235, 226, 215, 255);

                    this._makerSlots.Add(slot);
                }
                slot.slotButton.gameObject.SetActive(true);
                slot.copySrcToggle.gameObject.SetActive(true);
                slot.copyDstToggle.gameObject.SetActive(true);
            }
            for (; i < this._makerSlots.Count; ++i)
            {
                MakerSlot slot = this._makerSlots[i];
                slot.slotButton.gameObject.SetActive(false);
                slot.copySrcToggle.gameObject.SetActive(true);
                slot.copyDstToggle.gameObject.SetActive(true);
            }

            this._makerAddButtons.SetAsLastSibling();

            LayoutRebuilder.ForceRebuildLayoutImmediate(this._makerListTop);
        }

        private void AddOneSlot()
        {
            if (this.CheckMakerAdditionalData() == false)
                return;

            ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
            part.MemberInit();
            part.type = 350;
            this._makerAdditionalData.parts.Add(part);
            this._makerAdditionalData.objects.Add(new AdditionalData.AccessoryObject());

            this.UpdateMakerUI();
        }

        private void AddTenSlots()
        {
            if (this.CheckMakerAdditionalData() == false)
                return;

            for (int i = 0; i < 10; i++)
            {
                ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                part.MemberInit();
                part.type = 350;
                this._makerAdditionalData.parts.Add(part);
                this._makerAdditionalData.objects.Add(new AdditionalData.AccessoryObject());
            }

            this.UpdateMakerUI();
        }
        #endregion

        #region Studio
        private void SpawnStudioUI()
        {
            this._studioListTop = (RectTransform)GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot").transform;
            this._studioToggleTemplate = this._studioListTop.Find("Slot01").gameObject;

            MPCharCtrl ctrl = ((MPCharCtrl)Studio.Studio.Instance.manipulatePanelCtrl.GetPrivate("charaPanelInfo").GetPrivate("m_MPCharCtrl"));

            this._studioToggleAll = new StudioSlot();
            this._studioToggleAll.slot = GameObject.Instantiate(this._studioToggleTemplate);
            this._studioToggleAll.slotText = this._studioToggleAll.slot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            this._studioToggleAll.onButton = this._studioToggleAll.slot.transform.GetChild(1).GetComponent<Button>();
            this._studioToggleAll.offButton = this._studioToggleAll.slot.transform.GetChild(2).GetComponent<Button>();
            this._studioToggleAll.slotText.text = "全て";

            this._studioToggleAll.slot.transform.SetParent(this._studioListTop);
            this._studioToggleAll.slot.transform.localPosition = Vector3.zero;
            this._studioToggleAll.slot.transform.localScale = Vector3.one;
            this._studioToggleAll.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleAll.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateAll(true);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleAll.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleAll.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateAll(false);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleAll.slot.transform.SetAsLastSibling();

            this.UpdateUI();
        }

        private void UpdateStudioUI()
        {
            if (this._selectedStudioCharacter == null)
                return;

            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(this._selectedStudioCharacter.charInfo.chaFile, out additionalData) == false)
            {
                additionalData = new AdditionalData();
                this._charAdditionalData.Add(this._selectedStudioCharacter.charInfo.chaFile, additionalData);
            }
            int i;
            for (i = 0; i < additionalData.parts.Count; i++)
            {
                StudioSlot slot;
                ChaFileAccessory.PartsInfo accessory = additionalData.parts[i];
                if (i < this._studioSlots.Count)
                    slot = this._studioSlots[i];
                else
                {
                    int index = i + 20;

                    slot = new StudioSlot();
                    slot.slot = GameObject.Instantiate(this._studioToggleTemplate);
                    slot.slotText = slot.slot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    slot.onButton = slot.slot.transform.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.transform.GetChild(2).GetComponent<Button>();

                    slot.slot.name = $"Slot{index + 1}";
                    slot.slotText.name = $"TextMeshPro Slot{index + 1}";
                    slot.onButton.name = $"Button Slot{index + 1} 1";
                    slot.offButton.name = $"Button Slot{index + 1} 2";
                    slot.slotText.text = "スロット" + (index + 1);
                    slot.slot.transform.SetParent(this._studioListTop);
                    slot.slot.transform.localPosition = Vector3.zero;
                    slot.slot.transform.localScale = Vector3.one;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        this._selectedStudioCharacter.charInfo.SetAccessoryState(index, true);
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        this._selectedStudioCharacter.charInfo.SetAccessoryState(index, false);
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    this._studioSlots.Add(slot);
                }
                slot.slot.SetActive(true);
                slot.onButton.interactable = accessory != null && accessory.type != 350;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.objects[i].show ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null && accessory.type != 350;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.objects[i].show ? Color.green : Color.white;

            }
            for (; i < this._studioSlots.Count; ++i)
                this._studioSlots[i].slot.SetActive(false);
            this._studioToggleAll.slot.transform.SetAsLastSibling();
        }
        #endregion

        #region Common
        private void UpdateUI()
        {
            switch (this._binary)
            {
                case Binary.Game:
                    if (this._inMaker)
                    {
                        this.UpdateMakerUI();
                        CustomBase.Instance.ChangeAcsSlotName();
                    }
                    break;
                case Binary.Studio:
                    this.UpdateStudioUI();
                    break;
            }
        }
        #endregion

        #region Saves

        #region Sideloader
        private static void UAR_IterateCoordinatePrefixes_Postfix(object action, ChaFileCoordinate coordinate, ICollection<ResolveInfo> extInfo, string prefix = "")
        {
            if (_self._sideloaderChaFileAccessoryPartsInfoProperties == null)
            {
                _self._sideloaderChaFileAccessoryPartsInfoProperties = Type.GetType("Sideloader.AutoResolver.StructReference,AI_Sideloader").GetProperty("ChaFileAccessoryPartsInfoProperties", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
            }

            ChaFile owner = null;
            if (_self._overrideChaFile != null)
                owner = _self._overrideChaFile;
            else
            {
                if (_self._overrideChaFileCoordinate != null)
                    coordinate = _self._overrideChaFileCoordinate;
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == coordinate || pair.Value.chaFile.coordinate == coordinate)
                    {
                        owner = pair.Value.chaFile;
                        break;
                    }
                }
            }

            if (owner == null)
                return;

            if (_self._charAdditionalData.TryGetValue(owner, out AdditionalData additionalData) == false)
                return;

            for (int i = 0; i < additionalData.parts.Count; i++)
            {
                ChaFileAccessory.PartsInfo part = additionalData.parts[i];
                ((Delegate)action).DynamicInvoke(_self._sideloaderChaFileAccessoryPartsInfoProperties, part, extInfo, $"{prefix}accessory{i + 20}.");
            }
        }

        private static void UAR_ExtendedCardLoad_Prefix(ChaFile file)
        {
            _self.OnCharaLoad(file);
        }

        private static void UAR_ExtendedCardSave_Postfix(ChaFile file)
        {
            _self.OnCharaSave(file);
        }

        private static void UAR_ExtendedCoordLoad_Prefix(ChaFileCoordinate file)
        {
            _self.OnCoordLoad(file);
        }

        private static void UAR_ExtendedCoordSave_Postfix(ChaFileCoordinate file)
        {
            _self.OnCoordSave(file);
        }
        #endregion

        private void OnCharaLoad(ChaFile file)
        {
            if (this._patchThread != null)
                this._patchThread.Join();
            if (this._loadAdditionalAccessories == false)
                return;

            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            if (this._overrideChaFile != null)
                file = this._overrideChaFile;

            if (this._charAdditionalData.TryGetValue(file, out AdditionalData additionalData) == false)
            {
                additionalData = new AdditionalData();
                this._charAdditionalData.Add(file, additionalData);
            }
            else
                additionalData.parts.Clear();

            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }


            foreach (AdditionalData.AccessoryObject o in additionalData.objects)
                Destroy(o.obj);
            additionalData.objects.Clear();

            if (node != null)
                this.LoadAdditionalData(additionalData, node);

            while (additionalData.objects.Count < additionalData.parts.Count)
                additionalData.objects.Add(new AdditionalData.AccessoryObject());

            this.ExecuteDelayed(this.UpdateUI);
        }

        private void OnCharaSave(ChaFile file)
        {
            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(file, out additionalData) == false)
                return;

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                this.SaveAdditionalData(additionalData, xmlWriter);

                PluginData pluginData = new PluginData();
                pluginData.version = _saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }

        private void OnCoordLoad(ChaFileCoordinate file)
        {
            if (this._loadAdditionalAccessories == false)
                return;

            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);
            ChaFile owner = null;
            if (this._overrideChaFile != null)
                owner = this._overrideChaFile;
            else
            {
                if (this._overrideChaFileCoordinate != null)
                    file = this._overrideChaFileCoordinate;

                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == file || pair.Value.chaFile.coordinate == file)
                    {
                        owner = pair.Value.chaFile;
                        break;
                    }
                }
            }

            if (owner == null)
                return;

            if (this._charAdditionalData.TryGetValue(owner, out AdditionalData additionalData) == false)
            {
                additionalData = new AdditionalData();
                this._charAdditionalData.Add(owner, additionalData);
            }
            else
                additionalData.parts.Clear();

            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }

            foreach (AdditionalData.AccessoryObject o in additionalData.objects)
                Destroy(o.obj);
            additionalData.objects.Clear();

            if (node != null)
                this.LoadAdditionalData(additionalData, node);

            while (additionalData.objects.Count < additionalData.parts.Count)
                additionalData.objects.Add(new AdditionalData.AccessoryObject());

            this.ExecuteDelayed(this.UpdateUI);
        }

        private void OnCoordSave(ChaFileCoordinate file)
        {
            ChaFileControl owner = null;
            foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
            {
                if (pair.Value.nowCoordinate == file || pair.Value.chaFile.coordinate == file)
                {
                    owner = pair.Value.chaFile;
                    break;
                }
            }

            if (owner == null)
                return;

            AdditionalData additionalData;
            if (this._charAdditionalData.TryGetValue(owner, out additionalData) == false)
                return;

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                this.SaveAdditionalData(additionalData, xmlWriter);

                PluginData pluginData = new PluginData();
                pluginData.version = _saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }

        private void LoadAdditionalData(AdditionalData data, XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "accessory":
                        ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                        part.type = XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                        if (part.type != 350)
                        {
                            part.id = XmlConvert.ToInt32(childNode.Attributes["id"].Value);
                            part.parentKey = childNode.Attributes["parentKey"].Value;

                            for (int i = 0; i < 2; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    part.addMove[i, j] = new Vector3
                                    {
                                        x = XmlConvert.ToSingle(childNode.Attributes[$"addMove{i}{j}x"].Value),
                                        y = XmlConvert.ToSingle(childNode.Attributes[$"addMove{i}{j}y"].Value),
                                        z = XmlConvert.ToSingle(childNode.Attributes[$"addMove{i}{j}z"].Value)
                                    };
                                }
                            }
                            for (int i = 0; i < 4; i++)
                            {
                                part.colorInfo[i] = new ChaFileAccessory.PartsInfo.ColorInfo()
                                {
                                    color = new Color()
                                    {
                                        r = XmlConvert.ToSingle(childNode.Attributes[$"color{i}r"].Value),
                                        g = XmlConvert.ToSingle(childNode.Attributes[$"color{i}g"].Value),
                                        b = XmlConvert.ToSingle(childNode.Attributes[$"color{i}b"].Value),
                                        a = XmlConvert.ToSingle(childNode.Attributes[$"color{i}a"].Value)
                                    },
                                    glossPower = XmlConvert.ToSingle(childNode.Attributes[$"glossPower{i}"].Value),
                                    metallicPower = XmlConvert.ToSingle(childNode.Attributes[$"metallicPower{i}"].Value),
                                    smoothnessPower = XmlConvert.ToSingle(childNode.Attributes[$"smoothnessPower{i}"].Value)
                                };
                            }
                            part.hideCategory = XmlConvert.ToInt32(childNode.Attributes["hideCategory"].Value);
                            part.hideTiming = XmlConvert.ToInt32(childNode.Attributes["hideTiming"].Value);
                            part.noShake = XmlConvert.ToBoolean(childNode.Attributes["noShake"].Value);

                        }
                        data.parts.Add(part);
                        data.objects.Add(new AdditionalData.AccessoryObject());
                        if (childNode.Attributes["show"] != null)
                            data.objects[data.objects.Count - 1].show = XmlConvert.ToBoolean(childNode.Attributes["show"].Value);
                        break;
                }
            }
        }

        private void SaveAdditionalData(AdditionalData data, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("additionalAccessories");
            xmlWriter.WriteAttributeString("version", _version);
            for (int index = 0; index < data.parts.Count; index++)
            {
                ChaFileAccessory.PartsInfo part = data.parts[index];
                xmlWriter.WriteStartElement("accessory");
                xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));

                if (part.type != 350)
                {
                    xmlWriter.WriteAttributeString("id", XmlConvert.ToString(part.id));
                    xmlWriter.WriteAttributeString("parentKey", part.parentKey);

                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Vector3 v = part.addMove[i, j];
                            xmlWriter.WriteAttributeString($"addMove{i}{j}x", XmlConvert.ToString(v.x));
                            xmlWriter.WriteAttributeString($"addMove{i}{j}y", XmlConvert.ToString(v.y));
                            xmlWriter.WriteAttributeString($"addMove{i}{j}z", XmlConvert.ToString(v.z));
                        }
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        ChaFileAccessory.PartsInfo.ColorInfo colorInfo = part.colorInfo[i];
                        xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(colorInfo.color.r));
                        xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(colorInfo.color.g));
                        xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(colorInfo.color.b));
                        xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(colorInfo.color.a));

                        xmlWriter.WriteAttributeString($"glossPower{i}", XmlConvert.ToString(colorInfo.glossPower));
                        xmlWriter.WriteAttributeString($"metallicPower{i}", XmlConvert.ToString(colorInfo.metallicPower));
                        xmlWriter.WriteAttributeString($"smoothnessPower{i}", XmlConvert.ToString(colorInfo.smoothnessPower));
                    }

                    xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
                    xmlWriter.WriteAttributeString("hideTiming", XmlConvert.ToString(part.hideCategory));
                    xmlWriter.WriteAttributeString("noShake", XmlConvert.ToString(part.noShake));
                    if (this._binary == Binary.Studio)
                        xmlWriter.WriteAttributeString("show", XmlConvert.ToString(data.objects[index].show));
                }
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }
        #endregion
    }
}
