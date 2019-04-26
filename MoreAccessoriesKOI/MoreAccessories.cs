using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using BepInEx;
using ChaCustom;
using ExtensibleSaveFormat;
using Harmony;
using Illusion.Extensions;
using Illusion.Game;
using Manager;
using Studio;
using TMPro;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Scene = UnityEngine.SceneManagement.Scene;

namespace MoreAccessoriesKOI
{
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.moreaccessories", Name: "MoreAccessories", Version: versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    public class MoreAccessories : BaseUnityPlugin
    {
        public const string versionNum = "1.0.4";

        #region Events
        /// <summary>
        /// Fires when a new accessory UI element is created in the maker.
        /// </summary>
        public event Action<int, Transform> onCharaMakerSlotAdded;
        #endregion

        #region Private Types
        public class CharAdditionalData
        {
            public List<ChaFileAccessory.PartsInfo> nowAccessories;
            public readonly List<ListInfoBase> infoAccessory = new List<ListInfoBase>();
            public readonly List<GameObject> objAccessory = new List<GameObject>();
            public readonly List<GameObject[]> objAcsMove = new List<GameObject[]>();
            public readonly List<ChaAccessoryComponent> cusAcsCmp = new List<ChaAccessoryComponent>();
            public List<bool> showAccessories = new List<bool>();

            public readonly Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = new Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>();
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

        private class StudioSlotData
        {
            public RectTransform slot;
            public Text name;
            public Button onButton;
            public Button offButton;
        }

        private class HSceneSlotData
        {
            public RectTransform slot;
            public TextMeshProUGUI text;
            public Button button;
        }

        private enum Binary
        {
            Game,
            Studio
        }
        #endregion

        #region Private Variables
        public static MoreAccessories _self; //Not internal because other plugins might access this
        private const int _saveVersion = 1;
        private const string _extSaveKey = "moreAccessories";
        private GameObject _charaMakerSlotTemplate;
        private ScrollRect _charaMakerScrollView;
        internal CustomAcsChangeSlot _customAcsChangeSlot;
        internal CustomAcsParentWindow _customAcsParentWin;
        internal CustomAcsMoveWindow[] _customAcsMoveWin;
        internal CustomAcsSelectKind[] _customAcsSelectKind;
        internal CvsAccessory[] _cvsAccessory;
        internal List<CharaMakerSlotData> _additionalCharaMakerSlots;
        internal Dictionary<ChaFile, CharAdditionalData> _accessoriesByChar = new Dictionary<ChaFile, CharAdditionalData>();
        public CharAdditionalData _charaMakerData = null;
        private float _slotUIPositionY;
        private bool _usingSideloader = false;

        private bool _inCharaMaker = false;
        private Binary _binary;
        private RectTransform _addButtonsGroup;
        private ScrollRect _charaMakerCopyScrollView;
        private GameObject _copySlotTemplate;
        private ScrollRect _charaMakerTransferScrollView;
        private GameObject _transferSlotTemplate;
        private List<UI_RaycastCtrl> _raycastCtrls = new List<UI_RaycastCtrl>();
        private ChaFile _overrideCharaLoadingFile;
        private bool _loadAdditionalAccessories = true;
        private CustomFileWindow _loadCoordinatesWindow;

        private bool _inH;
        internal List<ChaControl> _hSceneFemales;
        private List<HSprite.FemaleDressButtonCategory> _hSceneMultipleFemaleButtons;
        private List<List<HSceneSlotData>> _additionalHSceneSlots = new List<List<HSceneSlotData>>();
        private HSprite _hSprite;
        private HSceneSpriteCategory _hSceneSoloFemaleAccessoryButton;

        private StudioSlotData _studioToggleAll;
        private RectTransform _studioToggleTemplate;
        private bool _inStudio;
        private OCIChar _selectedStudioCharacter;
        private readonly List<StudioSlotData> _additionalStudioSlots = new List<StudioSlotData>();
        private StudioSlotData _studioToggleMain;
        private StudioSlotData _studioToggleSub;

        #endregion

        #region Unity Methods
        void Awake()
        {
            SceneManager.sceneLoaded += this.SceneLoaded;
            _self = this;
            switch (Paths.ProcessName)
            {
                case "Koikatu":
                    this._binary = Binary.Game;
                    break;
                case "CharaStudio":
                    this._binary = Binary.Studio;
                    break;
            }
            ExtendedSave.CardBeingLoaded += this.OnCharaLoad;
            ExtendedSave.CardBeingSaved += this.OnCharaSave;
            ExtendedSave.CoordinateBeingLoaded += this.OnCoordLoad;
            ExtendedSave.CoordinateBeingSaved += this.OnCoordSave;
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.kkplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ChaControl_ChangeAccessory_Patches.ManualPatch(harmony);
        }

        public void OnApplicationQuit()
        {

        }

        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            switch (loadMode)
            {
                case LoadSceneMode.Single:
                    if (this._binary == Binary.Game)
                    {
                        this._inCharaMaker = false;
                        this._inH = false;
                        switch (scene.buildIndex)
                        {
                            case 2: //Chara maker
                                CustomBase.Instance.selectSlot = 0;
                                this._additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                                this._raycastCtrls = new List<UI_RaycastCtrl>();
                                this._inCharaMaker = true;
                                this._loadCoordinatesWindow = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow").GetComponent<CustomFileWindow>();
                                break;
                            case 17: //Hscenes
                                this._inH = true;
                                break;
                        }
                    }
                    else
                    {
                        if (scene.buildIndex == 1) //Studio
                        {
                            this.SpawnStudioUI();
                            this._inStudio = true;
                        }
                        else
                            this._inStudio = false;
                    }
                    if (this._accessoriesByChar.Any(p => p.Key == null))
                    {
                        Dictionary<ChaFile, CharAdditionalData> newDic = new Dictionary<ChaFile, CharAdditionalData>();
                        foreach (KeyValuePair<ChaFile, CharAdditionalData> pair in this._accessoriesByChar)
                            if (pair.Key != null)
                                newDic.Add(pair.Key, pair.Value);
                        this._accessoriesByChar = newDic;
                    }
                    break;
                case LoadSceneMode.Additive:
                    if (this._binary == Binary.Game && scene.buildIndex == 2) //Class chara maker
                    {
                        CustomBase.Instance.selectSlot = 0;
                        this._additionalCharaMakerSlots = new List<CharaMakerSlotData>();
                        this._raycastCtrls = new List<UI_RaycastCtrl>();
                        this._inCharaMaker = true;
                        this._loadCoordinatesWindow = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop/cosFileControl/charaFileWindow").GetComponent<CustomFileWindow>();
                    }
                    break;
            }
        }

        void Update()
        {
            if (this._inCharaMaker)
            {
                if (this._customAcsChangeSlot != null)
                {
                    if (CustomBase.Instance.updateCustomUI)
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
                if (this._loadCoordinatesWindow == null) //Handling maker with additive loading
                    this._inCharaMaker = false;
            }
            if (this._inStudio)
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

        void LateUpdate()
        {
            if (this._inCharaMaker && this._customAcsChangeSlot != null)
            {
                Transform t;
                if (CustomBase.Instance.selectSlot < 20)
                    t = this._customAcsChangeSlot.items[CustomBase.Instance.selectSlot].cgItem.transform;
                else
                    t = this._additionalCharaMakerSlots[CustomBase.Instance.selectSlot - 20].canvasGroup.transform;
                t.position = new Vector3(t.position.x, this._slotUIPositionY);
            }
        }
        #endregion

        #region Public Methods (aka the stuff other plugins use)
        /// <summary>
        /// Returns the ChaAccessoryComponent of <paramref name="character"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public ChaAccessoryComponent GetChaAccessoryComponent(ChaControl character, int index)
        {
            if (index < 20)
                return character.cusAcsCmp[index];
            CharAdditionalData data;
            index -= 20;
            if (this._accessoriesByChar.TryGetValue(character.chaFile, out data) && index < data.cusAcsCmp.Count)
                return data.cusAcsCmp[index];
            return null;
        }

        /// <summary>
        /// Returns the index of a certain ChaAccessoryComponent held by <paramref name="character"/>.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        public int GetChaAccessoryComponentIndex(ChaControl character, ChaAccessoryComponent component)
        {
            int index = character.cusAcsCmp.IndexOf(component);
            if (index == -1)
            {
                CharAdditionalData data;
                if (this._accessoriesByChar.TryGetValue(character.chaFile, out data) == false)
                    return -1;
                index = data.cusAcsCmp.IndexOf(component);
                if (index == -1)
                    return -1;
                index += 20;
            }
            return index;
        }

        /// <summary>
        /// Get the total of accessory UI element in the chara maker (vanilla + additional).
        /// </summary>
        /// <returns></returns>
        public int GetCvsAccessoryCount()
        {
            if(this._inCharaMaker)
                return this._additionalCharaMakerSlots.Count + 20;
            return 0;
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
            this._charaMakerSlotTemplate = container.GetChild(0).gameObject;
            RectTransform rootCanvas = ((RectTransform)this._charaMakerSlotTemplate.GetComponentInParent<Canvas>().transform);
            LayoutElement element = this._charaMakerScrollView.gameObject.AddComponent<LayoutElement>();
            element.minHeight = rootCanvas.rect.height / 1.298076f;
            element.minWidth = 622f; //Because trying to get the value dynamically fails for some reason so fuck it.
            VerticalLayoutGroup group = this._charaMakerScrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
            VerticalLayoutGroup parentGroup = container.GetComponent<VerticalLayoutGroup>();
            group.childAlignment = parentGroup.childAlignment;
            group.childControlHeight = parentGroup.childControlHeight;
            group.childControlWidth = parentGroup.childControlWidth;
            group.childForceExpandHeight = parentGroup.childForceExpandHeight;
            group.childForceExpandWidth = parentGroup.childForceExpandWidth;
            group.spacing = parentGroup.spacing;
            this._charaMakerScrollView.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            this.ExecuteDelayed(() =>
            {
                this._slotUIPositionY = this._charaMakerSlotTemplate.transform.parent.position.y;
            }, 15);

            Type kkus = Type.GetType("HSUS.HSUS,KKUS");
            if (kkus != null)
            {
                object self = kkus.GetField("_self", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                float scale = (float)self.GetPrivate("_gameUIScale");
                element.minHeight = element.minHeight / scale + 160f * (1f - scale);
            }
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
            TextMeshProUGUI text = Instantiate(textModel).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(addOneButton.transform);
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 4f), new Vector2(-5f, -4f));
            text.text = "+1";
            addOneButton.onClick.AddListener(this.AddSlot);

            Button addTenButton = UIUtility.CreateButton("Add Ten Button", this._addButtonsGroup, "+10");
            addTenButton.transform.SetRect(new Vector2(0.5f, 0f), Vector2.one);
            addTenButton.colors = toggleCopy.colors;
            ((Image)addTenButton.targetGraphic).sprite = toggleCopy.transform.Find("imgOff").GetComponent<Image>().sprite;
            Destroy(addTenButton.GetComponentInChildren<Text>().gameObject);
            text = Instantiate(textModel).GetComponent<TextMeshProUGUI>();
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

            this.ExecuteDelayed(() =>
            {
                this._cvsAccessory[0].UpdateCustomUI();
                ((Toggle)this._cvsAccessory[0].GetPrivate("tglTakeOverParent")).isOn = false;
                ((Toggle)this._cvsAccessory[0].GetPrivate("tglTakeOverColor")).isOn = false;
            }, 5);

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
            this._charaMakerCopyScrollView.transform.SetRect(new  Vector2(0f, 1f), Vector2.one, new Vector2(16f, -570f), new Vector2(-16f, -80f));

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
            this._charaMakerTransferScrollView.transform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -530f), new Vector2(-16f, -48f));

            this._charaMakerScrollView.viewport.gameObject.SetActive(false);

            this.ExecuteDelayed(() => //Fixes problems with UI masks overlapping and creating bugs
            {
                this._charaMakerScrollView.viewport.gameObject.SetActive(true);
            }, 5);
            this.ExecuteDelayed(() =>
            {
                this.UpdateMakerUI();
                CustomBase.Instance.updateCustomUI = true;
            }, 2);
        }

        private void SpawnStudioUI()
        {
            Transform accList = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot").transform;
            this._studioToggleTemplate = accList.Find("Slot20") as RectTransform;
            
            MPCharCtrl ctrl = ((MPCharCtrl)Studio.Studio.Instance.manipulatePanelCtrl.GetPrivate("charaPanelInfo").GetPrivate("m_MPCharCtrl"));

            this._studioToggleAll = new StudioSlotData();
            this._studioToggleAll.slot = (RectTransform)GameObject.Instantiate(this._studioToggleTemplate.gameObject).transform;
            this._studioToggleAll.name = this._studioToggleAll.slot.GetComponentInChildren<Text>();
            this._studioToggleAll.onButton = this._studioToggleAll.slot.GetChild(1).GetComponent<Button>();
            this._studioToggleAll.offButton = this._studioToggleAll.slot.GetChild(2).GetComponent<Button>();
            this._studioToggleAll.name.text = "全て";
            this._studioToggleAll.slot.SetParent(this._studioToggleTemplate.parent);
            this._studioToggleAll.slot.localPosition = Vector3.zero;
            this._studioToggleAll.slot.localScale = Vector3.one;
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
            this._studioToggleAll.slot.SetAsLastSibling();

            this._studioToggleMain = new StudioSlotData();
            this._studioToggleMain.slot = (RectTransform)GameObject.Instantiate(this._studioToggleTemplate.gameObject).transform;
            this._studioToggleMain.name = this._studioToggleMain.slot.GetComponentInChildren<Text>();
            this._studioToggleMain.onButton = this._studioToggleMain.slot.GetChild(1).GetComponent<Button>();
            this._studioToggleMain.offButton = this._studioToggleMain.slot.GetChild(2).GetComponent<Button>();
            this._studioToggleMain.name.text = "メイン";
            this._studioToggleMain.slot.SetParent(this._studioToggleTemplate.parent);
            this._studioToggleMain.slot.localPosition = Vector3.zero;
            this._studioToggleMain.slot.localScale = Vector3.one;
            this._studioToggleMain.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleMain.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(0, true);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleMain.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleMain.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(0, false);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleMain.slot.SetAsLastSibling();

            this._studioToggleSub = new StudioSlotData();
            this._studioToggleSub.slot = (RectTransform)GameObject.Instantiate(this._studioToggleTemplate.gameObject).transform;
            this._studioToggleSub.name = this._studioToggleSub.slot.GetComponentInChildren<Text>();
            this._studioToggleSub.onButton = this._studioToggleSub.slot.GetChild(1).GetComponent<Button>();
            this._studioToggleSub.offButton = this._studioToggleSub.slot.GetChild(2).GetComponent<Button>();
            this._studioToggleSub.name.text = "サブ";
            this._studioToggleSub.slot.SetParent(this._studioToggleTemplate.parent);
            this._studioToggleSub.slot.localPosition = Vector3.zero;
            this._studioToggleSub.slot.localScale = Vector3.one;
            this._studioToggleSub.onButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleSub.onButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(1, true);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleSub.offButton.onClick = new Button.ButtonClickedEvent();
            this._studioToggleSub.offButton.onClick.AddListener(() =>
            {
                this._selectedStudioCharacter.charInfo.SetAccessoryStateCategory(1, false);
                ctrl.CallPrivate("UpdateInfo");
                this.UpdateStudioUI();
            });
            this._studioToggleSub.slot.SetAsLastSibling();

        }

        internal void SpawnHUI(HSceneProc hSceneProc)
        {
            this._hSceneFemales = (List<ChaControl>)hSceneProc.GetPrivate("lstFemale");
            this._additionalHSceneSlots = new List<List<HSceneSlotData>>();
            for (int i = 0; i < 2; i++)
                this._additionalHSceneSlots.Add(new List<HSceneSlotData>());
            this._hSprite = hSceneProc.sprite;
                this._hSceneMultipleFemaleButtons = this._hSprite.lstMultipleFemaleDressButton;
            this._hSceneSoloFemaleAccessoryButton = this._hSprite.categoryAccessory;
            this.UpdateHUI();
        }

        internal void UpdateUI()
        {
            if (this._inCharaMaker)
                this.UpdateMakerUI();
            else if (this._inStudio)
                this.UpdateStudioUI();
            else if (this._inH)
                this.ExecuteDelayed(this.UpdateHUI);
        }

        private void UpdateMakerUI()
        {
            if (this._customAcsChangeSlot == null)
                return;
            int count = this._charaMakerData.nowAccessories != null ? this._charaMakerData.nowAccessories.Count : 0;
            int i;
            for (i = 0; i < count; i++)
            {
                CharaMakerSlotData info;
                if (i < this._additionalCharaMakerSlots.Count)
                {
                    info = this._additionalCharaMakerSlots[i];
                    info.toggle.gameObject.SetActive(true);
                    if (i + 20 == CustomBase.Instance.selectSlot)
                        info.cvsAccessory.UpdateCustomUI();

                    info.transferSlotObject.SetActive(true);
                }
                else
                {
                    GameObject newSlot = Instantiate(this._charaMakerSlotTemplate, this._charaMakerScrollView.content);
                    info = new CharaMakerSlotData();
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
                    info.cvsAccessory.UpdateCustomUI();

                    if (this.onCharaMakerSlotAdded != null)
                        this.onCharaMakerSlotAdded(index, newSlot.transform);
                }
                info.cvsAccessory.UpdateSlotName();

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
            this._addButtonsGroup.SetAsLastSibling();
        }

        internal void UpdateStudioUI()
        {
            if (this._selectedStudioCharacter == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.nowAccessories.Count; i++)
            {
                StudioSlotData slot;
                ChaFileAccessory.PartsInfo accessory = additionalData.nowAccessories[i];
                if (i < this._additionalStudioSlots.Count)
                {
                    slot = this._additionalStudioSlots[i];
                }
                else
                {
                    slot = new StudioSlotData();
                    slot.slot = (RectTransform)GameObject.Instantiate(this._studioToggleTemplate.gameObject).transform;
                    slot.name = slot.slot.GetComponentInChildren<Text>();
                    slot.onButton = slot.slot.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.GetChild(2).GetComponent<Button>();
                    slot.name.text = "スロット" + (21 + i);
                    slot.slot.SetParent(this._studioToggleTemplate.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    int i1 = i;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessories[i1] = true;
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessories[i1] = false;
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    this._additionalStudioSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.onButton.interactable = accessory != null && accessory.type != 120;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.showAccessories[i] ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null && accessory.type != 120;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.showAccessories[i] ? Color.green : Color.white;
            }
            for (; i < this._additionalStudioSlots.Count; ++i)
                this._additionalStudioSlots[i].slot.gameObject.SetActive(false);
            this._studioToggleAll.slot.SetAsLastSibling();
            this._studioToggleMain.slot.SetAsLastSibling();
            this._studioToggleSub.slot.SetAsLastSibling();
        }

        private void UpdateHUI()
        {
            if (this._hSprite == null)
                return;
            for (int i = 0; i < this._hSceneFemales.Count; i++)
            {
                ChaControl female = this._hSceneFemales[i];

                CharAdditionalData additionalData = this._accessoriesByChar[female.chaFile];
                int j;
                List<HSceneSlotData> additionalSlots = this._additionalHSceneSlots[i];
                Transform buttonsParent = this._hSceneFemales.Count == 1 ? this._hSceneSoloFemaleAccessoryButton.transform : this._hSceneMultipleFemaleButtons[i].accessory.transform;
                for (j = 0; j < additionalData.nowAccessories.Count; j++)
                {
                    HSceneSlotData slot;
                    if (j < additionalSlots.Count)
                        slot = additionalSlots[j];
                    else
                    {
                        slot = new HSceneSlotData();
                        slot.slot = (RectTransform)GameObject.Instantiate(buttonsParent.GetChild(0).gameObject).transform;
                        slot.text = slot.slot.GetComponentInChildren<TextMeshProUGUI>(true);
                        slot.button = slot.slot.GetComponentInChildren<Button>(true);
                        slot.slot.SetParent(buttonsParent);
                        slot.slot.localPosition = Vector3.zero;
                        slot.slot.localScale = Vector3.one;
                        int i1 = j;
                        slot.button.onClick = new Button.ButtonClickedEvent();
                        slot.button.onClick.AddListener(() =>
                        {
                            if (!Input.GetMouseButtonUp(0))
                                return;
                            if (!this._hSprite.IsSpriteAciotn())
                                return;
                            additionalData.showAccessories[i1] = !additionalData.showAccessories[i1];
                            Utils.Sound.Play(SystemSE.sel);
                        });
                        additionalSlots.Add(slot);
                    }
                    GameObject objAccessory = additionalData.objAccessory[j];
                    if (objAccessory == null)
                        slot.slot.gameObject.SetActive(false);
                    else
                    {
                        slot.slot.gameObject.SetActive(true);
                        ListInfoComponent component = objAccessory.GetComponent<ListInfoComponent>();
                        slot.text.text = component.data.Name;
                    }
                }

                for (; j < additionalSlots.Count; ++j)
                    additionalSlots[j].slot.gameObject.SetActive(false);
            }
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
            this._charaMakerData.nowAccessories.Add(partsInfo);
            while (this._charaMakerData.infoAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.infoAccessory.Add(null);
            while (this._charaMakerData.objAccessory.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAccessory.Add(null);
            while (this._charaMakerData.objAcsMove.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.objAcsMove.Add(new GameObject[2]);
            while (this._charaMakerData.cusAcsCmp.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.cusAcsCmp.Add(null);
            while (this._charaMakerData.showAccessories.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.showAccessories.Add(true);
            this.UpdateMakerUI();
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
            while (this._charaMakerData.showAccessories.Count < this._charaMakerData.nowAccessories.Count)
                this._charaMakerData.showAccessories.Add(true);
            this.UpdateMakerUI();
        }

        private void AccessorySlotToggleCallback(int index, Toggle toggle)
        {
            if (toggle.isOn)
            {
                CustomBase.Instance.selectSlot = index;
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
            for (int i = 0; i < this._customAcsChangeSlot.items.Length; i++)
            {
                UI_ToggleGroupCtrl.ItemInfo info = this._customAcsChangeSlot.items[i];
                if (info.cgItem != null)
                    info.cgItem.Enable(false, false);
            }
            for (int i = 0; i < this._additionalCharaMakerSlots.Count; i++)
            {
                CharaMakerSlotData info = this._additionalCharaMakerSlots[i];
                if (info.canvasGroup != null)
                    info.canvasGroup.Enable(false, false);
            }
            if (toggle.isOn && canvasGroup)
                canvasGroup.Enable(true, false);
        }

        internal void OnCoordTypeChange()
        {
            if (this._inCharaMaker)
            {
                if (CustomBase.Instance.selectSlot >= 20 && !this._additionalCharaMakerSlots[CustomBase.Instance.selectSlot - 20].toggle.gameObject.activeSelf)
                {
                    Toggle toggle = this._customAcsChangeSlot.items[0].tglItem;
                    toggle.isOn = true;
                    CustomBase.Instance.selectSlot = 0;
                }
            }
            this.UpdateUI();
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

        #region Saves

        #region Sideloader
        [HarmonyPatch]
        private static class SideloaderAutoresolverHooks_IterateCardPrefixes_Patches
        {
            private static PropertyInfo _resolveInfoProperty;
            private static CharAdditionalData _currentAdditionalData;
            private static object _sideLoaderChaFileAccessoryPartsInfoProperties;

            private static bool Prepare()
            {
                Type t = Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader");
                if (t != null)
                {
                    _resolveInfoProperty = Type.GetType("Sideloader.AutoResolver.ResolveInfo,Sideloader").GetProperty("Property", BindingFlags.Public | BindingFlags.Instance);
                    _self._usingSideloader = true;
                    return true;
                }
                return false;
            }
            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader").GetMethod("IterateCardPrefixes", BindingFlags.NonPublic | BindingFlags.Static);
            }

            [HarmonyBefore("com.deathweasel.bepinex.guidmigration")]
            private static void Prefix(ChaFile file, object extInfo)
            {
                if (extInfo != null)
                {
                    int i = 0;
                    foreach (object o in (IList)extInfo)
                    {
                        string property = (string)_resolveInfoProperty.GetValue(o, null);
                        if (property.StartsWith("outfit.")) //Sorry to whoever reads this, I fucked up
                        {
                            char[] array = property.ToCharArray();
                            array[6] = array[7];
                            array[7] = '.';
                            _resolveInfoProperty.SetValue(o, new string(array), null);
                        }
                        ++i;
                    }
                }
                if (_self._overrideCharaLoadingFile != null)
                {
                    file = _self._overrideCharaLoadingFile;
                }

                _self._accessoriesByChar.TryGetValue(file, out _currentAdditionalData);

                if (_sideLoaderChaFileAccessoryPartsInfoProperties == null)
                {
                    _sideLoaderChaFileAccessoryPartsInfoProperties = Type.GetType("Sideloader.AutoResolver.StructReference,Sideloader").GetProperty("ChaFileAccessoryPartsInfoProperties", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                }
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (set == false &&
                        (instructionsList[i + 1].opcode == OpCodes.Blt || instructionsList[i + 1].opcode == OpCodes.Brtrue) &&
                        instructionsList[i + 2].opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_0); //i
                        yield return new CodeInstruction(OpCodes.Ldarg_0); //action
                        yield return new CodeInstruction(OpCodes.Ldarg_2); //extInfo
                        yield return new CodeInstruction(OpCodes.Call, typeof(SideloaderAutoresolverHooks_IterateCardPrefixes_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        set = true;
                    }
                }
            }

            private static void Injected(int i, object action, object extInfo)
            {
                if (_currentAdditionalData == null || _currentAdditionalData.rawAccessoriesInfos.TryGetValue((ChaFileDefine.CoordinateType)i, out List<ChaFileAccessory.PartsInfo> parts) == false)
                    return;
                for (int j = 0; j < parts.Count; j++)
                    ((Delegate)action).DynamicInvoke(_sideLoaderChaFileAccessoryPartsInfoProperties, parts[j], extInfo, $"outfit{i}.accessory{(j + 20)}.");
            }
        }

        [HarmonyPatch]
        private static class SideloaderAutoresolverHooks_IterateCoordinatePrefixes_Patches
        {
            private static object _sideLoaderChaFileAccessoryPartsInfoProperties;

            private static bool Prepare()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader") != null;
            }
            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader").GetMethod("IterateCoordinatePrefixes", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private static void Postfix(object action, ChaFileCoordinate coordinate, object extInfo)
            {
                if (_sideLoaderChaFileAccessoryPartsInfoProperties == null)
                {
                    _sideLoaderChaFileAccessoryPartsInfoProperties = Type.GetType("Sideloader.AutoResolver.StructReference,Sideloader").GetProperty("ChaFileAccessoryPartsInfoProperties", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                }
                ChaFile owner = null;
                foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
                {
                    if (pair.Value.nowCoordinate == coordinate)
                    {
                        owner = pair.Value.chaFile;
                        break;
                    }
                    foreach (ChaFileCoordinate c in pair.Value.chaFile.coordinate)
                    {
                        if (c == coordinate)
                        {
                            owner = pair.Value.chaFile;
                            goto DOUBLEBREAK;
                        }
                    }
                }
                DOUBLEBREAK:
                if (owner == null)
                    return;

                CharAdditionalData additionalData;
                if (_self._accessoriesByChar.TryGetValue(owner, out additionalData) == false)
                    return;

                for (int j = 0; j < additionalData.nowAccessories.Count; j++)
                    ((Delegate)action).DynamicInvoke(_sideLoaderChaFileAccessoryPartsInfoProperties, additionalData.nowAccessories[j], extInfo, $"accessory{(j + 20)}.");
            }
        }

        [HarmonyPatch]
        private static class SideloaderAutoResolverHooks_ExtendedCardLoad_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader") != null;
            }
            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader").GetMethod("ExtendedCardLoad", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private static void Prefix(ChaFile file)
            {
                _self.OnActualCharaLoad(file);
            }
        }

        [HarmonyPatch]
        private static class SideloaderAutoResolverHooks_ExtendedCardSave_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader") != null;
            }
            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader").GetMethod("ExtendedCardSave", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private static void Postfix(ChaFile file)
            {
                _self.OnActualCharaSave(file);
            }
        }

        [HarmonyPatch]
        private static class SideloaderAutoResolverHooks_ExtendedCoordinateLoad_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader") != null;
            }
            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader").GetMethod("ExtendedCoordinateLoad", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private static void Prefix(ChaFileCoordinate file)
            {
                _self.OnActualCoordLoad(file);
            }
        }

        [HarmonyPatch]
        private static class SideloaderAutoResolverHooks_ExtendedCoordinateSave_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader") != null;
            }
            private static MethodInfo TargetMethod()
            {
                return Type.GetType("Sideloader.AutoResolver.Hooks,Sideloader").GetMethod("ExtendedCoordinateSave", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private static void Postfix(ChaFileCoordinate file)
            {
                _self.OnActualCoordSave(file);
            }
        }
        #endregion

        [HarmonyPatch(typeof(ChaFileControl), "LoadFileLimited", new []{typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        private static class ChaFileControl_LoadFileLimited_Patches
        {
            private static void Prefix(ChaFileControl __instance, bool coordinate = true)
            {
                if (_self._inCharaMaker && _self._customAcsChangeSlot != null)
                {
                    _self._overrideCharaLoadingFile = __instance;
                    _self._loadAdditionalAccessories = coordinate;
                }
            }

            private static void Postfix()
            {
                _self._overrideCharaLoadingFile = null;
                _self._loadAdditionalAccessories = true;
            }
        }

        private void OnCharaLoad(ChaFile file)
        {
            if (this._usingSideloader == false)
                this.OnActualCharaLoad(file);
        }

        private void OnActualCharaLoad(ChaFile file)
        {
            if (this._loadAdditionalAccessories == false)
                return;
            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);

            if (this._overrideCharaLoadingFile != null)
                file = this._overrideCharaLoadingFile;

            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(file, out data) == false)
            {
                data = new CharAdditionalData();
                this._accessoriesByChar.Add(file, data);
            }
            else
            {
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> pair in data.rawAccessoriesInfos)
                    pair.Value.Clear();
            }
            XmlNode node = null;
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "accessorySet":
                            ChaFileDefine.CoordinateType coordinateType = (ChaFileDefine.CoordinateType)XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                            List<ChaFileAccessory.PartsInfo> parts;

                            if (data.rawAccessoriesInfos.TryGetValue(coordinateType, out parts) == false)
                            {
                                parts = new List<ChaFileAccessory.PartsInfo>();
                                data.rawAccessoriesInfos.Add(coordinateType, parts);
                            }

                            foreach (XmlNode accessoryNode in childNode.ChildNodes)
                            {

                                ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                                part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
                                if (part.type != 120)
                                {
                                    part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                                    part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                                    for (int i = 0; i < 2; i++)
                                    {
                                        for (int j = 0; j < 3; j++)
                                        {
                                            part.addMove[i, j] = new Vector3
                                            {
                                                x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                                y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                                z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                            };
                                        }
                                    }
                                    for (int i = 0; i < 4; i++)
                                    {
                                        part.color[i] = new Color
                                        {
                                            r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                            g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                            b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                            a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                                        };
                                    }
                                    part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
                                }
                                parts.Add(part);
                            }
                            break;
                        case "visibility":
                            if (this._inStudio)
                            {
                                data.showAccessories = new List<bool>();
                                foreach (XmlNode grandChildNode in childNode.ChildNodes)
                                    data.showAccessories.Add(grandChildNode.Attributes?["value"] == null || XmlConvert.ToBoolean(grandChildNode.Attributes["value"].Value));
                            }
                            break;
                    }
                }

            }
            if (data.rawAccessoriesInfos.TryGetValue((ChaFileDefine.CoordinateType)file.status.coordinateType, out data.nowAccessories) == false)
            {
                data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                data.rawAccessoriesInfos.Add((ChaFileDefine.CoordinateType)file.status.coordinateType, data.nowAccessories);
            }
            while (data.infoAccessory.Count < data.nowAccessories.Count)
                data.infoAccessory.Add(null);
            while (data.objAccessory.Count < data.nowAccessories.Count)
                data.objAccessory.Add(null);
            while (data.objAcsMove.Count < data.nowAccessories.Count)
                data.objAcsMove.Add(new GameObject[2]);
            while (data.cusAcsCmp.Count < data.nowAccessories.Count)
                data.cusAcsCmp.Add(null);
            while (data.showAccessories.Count < data.nowAccessories.Count)
                data.showAccessories.Add(true);

            if (this._inH || this._inCharaMaker)
                this.ExecuteDelayed(this.UpdateUI);
            else
                this.UpdateUI();
        }

        private void OnCharaSave(ChaFile file)
        {
            if (this._usingSideloader == false)
                this.OnActualCharaSave(file);
        }

        private void OnActualCharaSave(ChaFile file)
        {
            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(file, out data) == false)
                return;

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                int maxCount = 0;
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", MoreAccessories.versionNum);
                foreach (KeyValuePair<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> pair in data.rawAccessoriesInfos)
                {
                    if (pair.Value.Count == 0)
                        continue;
                    xmlWriter.WriteStartElement("accessorySet");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString((int)pair.Key));
                    if (maxCount < pair.Value.Count)
                        maxCount = pair.Value.Count;

                    for (int index = 0; index < pair.Value.Count; index++)
                    {
                        ChaFileAccessory.PartsInfo part = pair.Value[index];
                        xmlWriter.WriteStartElement("accessory");
                        xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));

                        if (part.type != 120)
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
                                Color c = part.color[i];
                                xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                                xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                                xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                                xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                            }

                            xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
                        }
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                }

                if (this._inStudio)
                {
                    xmlWriter.WriteStartElement("visibility");
                    for (int i = 0; i < maxCount && i < data.showAccessories.Count; i++)
                    {
                        xmlWriter.WriteStartElement("visible");
                        xmlWriter.WriteAttributeString("value", XmlConvert.ToString(data.showAccessories[i]));
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();

                PluginData pluginData = new PluginData();
                pluginData.version = MoreAccessories._saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }

        private void OnCoordLoad(ChaFileCoordinate file)
        {
            if (this._usingSideloader == false)
                this.OnActualCoordLoad(file);
        }

        private void OnActualCoordLoad(ChaFileCoordinate file)
        {
            if (this._inCharaMaker && this._loadCoordinatesWindow != null && this._loadCoordinatesWindow.tglCoordeLoadAcs != null && this._loadCoordinatesWindow.tglCoordeLoadAcs.isOn == false)
                this._loadAdditionalAccessories = false;
            if (this._loadAdditionalAccessories == false) // This stuff is done this way because some user might want to change _loadAdditionalAccessories manually through reflection.
            {
                this._loadAdditionalAccessories = true;
                return;                
            }
            ChaFile chaFile = null;
            foreach (KeyValuePair<int, ChaControl> pair in Character.Instance.dictEntryChara)
            {
                if (pair.Value.nowCoordinate == file)
                {
                    chaFile = pair.Value.chaFile;
                    break;
                }
            }
            if (chaFile == null)
                return;
            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(chaFile, out data) == false)
            {
                data = new CharAdditionalData();
                this._accessoriesByChar.Add(chaFile, data);
            }
            if (this._inH)
                data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
            else
            {
                if (data.rawAccessoriesInfos.TryGetValue((ChaFileDefine.CoordinateType)chaFile.status.coordinateType, out data.nowAccessories) == false)
                {
                    data.nowAccessories = new List<ChaFileAccessory.PartsInfo>();
                    data.rawAccessoriesInfos.Add((ChaFileDefine.CoordinateType)chaFile.status.coordinateType, data.nowAccessories);
                }
                else
                    data.nowAccessories.Clear();
            }

            XmlNode node = null;
            PluginData pluginData = ExtendedSave.GetExtendedDataById(file, _extSaveKey);
            if (pluginData != null && pluginData.data.TryGetValue("additionalAccessories", out object xmlData))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml((string)xmlData);
                node = doc.FirstChild;
            }
            if (node != null)
            {
                foreach (XmlNode accessoryNode in node.ChildNodes)
                {
                    ChaFileAccessory.PartsInfo part = new ChaFileAccessory.PartsInfo();
                    part.type = XmlConvert.ToInt32(accessoryNode.Attributes["type"].Value);
                    if (part.type != 120)
                    {
                        part.id = XmlConvert.ToInt32(accessoryNode.Attributes["id"].Value);
                        part.parentKey = accessoryNode.Attributes["parentKey"].Value;

                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                part.addMove[i, j] = new Vector3
                                {
                                    x = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}x"].Value),
                                    y = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}y"].Value),
                                    z = XmlConvert.ToSingle(accessoryNode.Attributes[$"addMove{i}{j}z"].Value)
                                };
                            }
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            part.color[i] = new Color
                            {
                                r = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}r"].Value),
                                g = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}g"].Value),
                                b = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}b"].Value),
                                a = XmlConvert.ToSingle(accessoryNode.Attributes[$"color{i}a"].Value)
                            };
                        }
                        part.hideCategory = XmlConvert.ToInt32(accessoryNode.Attributes["hideCategory"].Value);
                    }
                    data.nowAccessories.Add(part);
                }
            }

            while (data.infoAccessory.Count < data.nowAccessories.Count)
                data.infoAccessory.Add(null);
            while (data.objAccessory.Count < data.nowAccessories.Count)
                data.objAccessory.Add(null);
            while (data.objAcsMove.Count < data.nowAccessories.Count)
                data.objAcsMove.Add(new GameObject[2]);
            while (data.cusAcsCmp.Count < data.nowAccessories.Count)
                data.cusAcsCmp.Add(null);
            while (data.showAccessories.Count < data.nowAccessories.Count)
                data.showAccessories.Add(true);

            if (this._inH || this._inCharaMaker)
                this.ExecuteDelayed(this.UpdateUI);
            else
                this.UpdateUI();
        }

        private void OnCoordSave(ChaFileCoordinate file)
        {
            if (this._usingSideloader == false)
                this.OnActualCoordSave(file);
        }

        private void OnActualCoordSave(ChaFileCoordinate file)
        {
            if (this._inCharaMaker == false) //Need to see if that can happen
                return;
            CharAdditionalData data;
            if (this._accessoriesByChar.TryGetValue(CustomBase.Instance.chaCtrl.chaFile, out data) == false)
                return;
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.WriteStartElement("additionalAccessories");
                xmlWriter.WriteAttributeString("version", MoreAccessories.versionNum);
                foreach (ChaFileAccessory.PartsInfo part in data.nowAccessories)
                {
                    xmlWriter.WriteStartElement("accessory");
                    xmlWriter.WriteAttributeString("type", XmlConvert.ToString(part.type));
                    if (part.type != 120)
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
                            Color c = part.color[i];
                            xmlWriter.WriteAttributeString($"color{i}r", XmlConvert.ToString(c.r));
                            xmlWriter.WriteAttributeString($"color{i}g", XmlConvert.ToString(c.g));
                            xmlWriter.WriteAttributeString($"color{i}b", XmlConvert.ToString(c.b));
                            xmlWriter.WriteAttributeString($"color{i}a", XmlConvert.ToString(c.a));
                        }
                        xmlWriter.WriteAttributeString("hideCategory", XmlConvert.ToString(part.hideCategory));
                    }
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

                PluginData pluginData = new PluginData();
                pluginData.version = MoreAccessories._saveVersion;
                pluginData.data.Add("additionalAccessories", stringWriter.ToString());
                ExtendedSave.SetExtendedDataById(file, _extSaveKey, pluginData);
            }
        }
        #endregion
    }
}
