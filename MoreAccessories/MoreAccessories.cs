using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionPlugin;
using Manager;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessories
{
    public class MoreAccessories : IEnhancedPlugin
    {
        #region Private Types
        private enum Binary
        {
            Neo,
            Game,
        }

        private class StudioSlotData
        {
            public RectTransform slot;
            public Text name;
            public Button onButton;
            public Button offButton;
        }
        #endregion

        #region Public Types
        public class CharAdditionalData
        {
            public List<GameObject> objAccessory = new List<GameObject>();
            public List<CharFileInfoClothes.Accessory> clothesInfoAccessory = new List<CharFileInfoClothes.Accessory>();
            public List<ListTypeFbx> infoAccessory = new List<ListTypeFbx>();
            public List<GameObject> objAcsMove = new List<GameObject>();
            public List<bool> showAccessory = new List<bool>();
            public Dictionary<int, List<GameObject>> charInfoDictTagObj = new Dictionary<int, List<GameObject>>();

            public Dictionary<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> rawAccessoriesInfos = new Dictionary<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>>();
        }
        public class MakerSlotData
        {
            public Button button;
            public Text text;
            public UI_TreeView treeView;
        }
        #endregion

        #region Private Variables
        private Dictionary<CharFile, CharAdditionalData> _accessoriesByChar = new Dictionary<CharFile, CharAdditionalData>();
        private RectTransform _prefab;
        private Binary _binary;
        private SubMenuControl _smControl;
        private SmMoreAccessories _smMoreAccessories;
        private CharInfo _charaMakerCharInfo;
        private MainMenuSelect _mainMenuSelect;
        private bool _ready = false;
        private readonly List<MakerSlotData> _displayedMakerSlots = new List<MakerSlotData>();
        private int _level;
        private Button _addButton;
        private RoutinesComponent _routines;
        private Studio.OCIChar _selectedStudioCharacter;
        private readonly List<StudioSlotData> _displayedStudioSlots = new List<StudioSlotData>();
        #endregion

        #region Public Accessors
        public static MoreAccessories self { get; private set; }
        public Dictionary<CharFile, CharAdditionalData> accessoriesByChar { get { return (this._accessoriesByChar); } }
        public CharAdditionalData charaMakerAdditionalData { get; private set; }
        public SubMenuItem smItem { get; } = new SubMenuItem();
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64" }; } }
        public string Name { get { return "MoreAccessories"; } }
        public string Version { get { return "1.0.0"; } }
        public List<MakerSlotData> displayedMakerSlots { get { return this._displayedMakerSlots; } }
        public CharInfo charaMakerCharInfo
        {
            get { return this._charaMakerCharInfo; }
            set
            {
                this._charaMakerCharInfo = value;
                CharAdditionalData additionalData;
                if (this._accessoriesByChar.TryGetValue(this._charaMakerCharInfo.chaFile, out additionalData) == false)
                {
                    additionalData = new CharAdditionalData();
                    this._accessoriesByChar.Add(this._charaMakerCharInfo.chaFile, additionalData);
                }

                this.charaMakerAdditionalData = additionalData;
            }
        }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            self = this;

            switch (Process.GetCurrentProcess().ProcessName)
            {
                case "HoneySelect_32":
                case "HoneySelect_64":
                    this._binary = Binary.Game;
                    break;
                case "StudioNEO_32":
                case "StudioNEO_64":
                    this._binary = Binary.Neo;
                    break;
            }

            HSExtSave.HSExtSave.RegisterHandler("moreAccessories", this.OnCharaLoad, this.OnCharaSave, this.OnSceneLoad, null, this.OnSceneSave, null, null);

            UIUtility.Init();

            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelWasLoaded(int level)
        {
            this._routines = new GameObject("Routines", typeof(RoutinesComponent)).GetComponent<RoutinesComponent>();
            this._level = level;
            if (this._binary == Binary.Game)
            {
                //if (level == 15)
                //{
                //    HScene hScene = GameObject.FindObjectOfType<HScene>();
                //    CharFemale[] females = (CharFemale[])hScene.GetPrivate("chaFemales");
                //    CharAdditionalData additionalData;
                //    UnityEngine.Debug.Log("bite1");
                //    if (this._accessoriesByChar.TryGetValue(females[0].chaFile, out additionalData) && additionalData.rawAccessoriesInfos != null && additionalData.rawAccessoriesInfos.Count > 0 && additionalData.rawAccessoriesInfos.First().Value.Count > 0)
                //    {
                //        UnityEngine.Debug.Log("bite");
                //        GameObject accList = GameObject.Find("Canvas").transform.Find("AccessoryCategory/tglOnePlayer/AccessoryCharacterCategory/AccessoryMenu").gameObject;
                //        GameObject button = GameObject.Instantiate(accList.transform.GetChild(accList.transform.childCount - 1).gameObject);
                //        button.transform.SetParent(accList.transform);
                //        button.gameObject.SetActive(true);
                //        Button b = button.GetComponent<Button>();
                //        b.onClick = new Button.ButtonClickedEvent();
                //        b.onClick.AddListener(() =>
                //        {
                //            CharClothes_SetAccessoryStateAll_Patches.Prefix(females[0].femaleClothes, additionalData.showAccessory.Count <= 0 || !additionalData.showAccessory[0]);
                //        });

                //    }
                //    if (this._accessoriesByChar.TryGetValue(females[1].chaFile, out additionalData) && additionalData.rawAccessoriesInfos != null && additionalData.rawAccessoriesInfos.Count > 0 && additionalData.rawAccessoriesInfos.First().Value.Count > 0)
                //    {
                //        UnityEngine.Debug.Log("bite2");

                //        GameObject accList = GameObject.Find("Canvas").transform.Find("AccessoryCategory/tglTwoPlayer/AccessoryCharacterCategory/AccessoryMenu").gameObject;
                //        GameObject button = GameObject.Instantiate(accList.transform.GetChild(accList.transform.childCount - 1).gameObject);
                //        button.transform.SetParent(accList.transform);
                //        button.gameObject.SetActive(true);
                //        Button b = button.GetComponent<Button>();
                //        b.onClick = new Button.ButtonClickedEvent();
                //        b.onClick.AddListener(() =>
                //        {
                //            CharClothes_SetAccessoryStateAll_Patches.Prefix(females[1].femaleClothes, additionalData.showAccessory.Count <= 0 || !additionalData.showAccessory[0]);
                //        });

                //    }
                //}
                if (level == 21)
                {
                    UIUtility.SetCustomFont("mplus-1c-medium");
                    if (Game.Instance.customSceneInfo.isFemale)
                        this._prefab = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_Clothes/Accessory/AcsSlot10").transform as RectTransform;
                    else
                        this._prefab = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/MaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_Clothes/Accessory/AcsSlot10").transform as RectTransform;

                    Dictionary<CharFile, CharAdditionalData> newDic = new Dictionary<CharFile, CharAdditionalData>();
                    foreach (KeyValuePair<CharFile, CharAdditionalData> pair in this._accessoriesByChar)
                    {
                        if (pair.Key != null)
                            newDic.Add(pair.Key, pair.Value);
                    }
                    this._accessoriesByChar = newDic;
                    this._displayedMakerSlots.Clear();

                    foreach (SubMenuControl subMenuControl in Resources.FindObjectsOfTypeAll<SubMenuControl>())
                    {
                        this._smControl = subMenuControl;
                        break;
                    }

                    foreach (SmAccessory smAccessory in Resources.FindObjectsOfTypeAll<SmAccessory>())
                    {
                        GameObject obj = GameObject.Instantiate(smAccessory.gameObject);
                        obj.transform.SetParent(smAccessory.transform.parent);
                        obj.transform.localScale = smAccessory.transform.localScale;
                        obj.transform.localPosition = smAccessory.transform.localPosition;
                        obj.transform.localRotation = smAccessory.transform.localRotation;
                        (obj.transform as RectTransform).SetRect(smAccessory.transform as RectTransform);
                        SmAccessory original = obj.GetComponent<SmAccessory>();
                        this._smMoreAccessories = obj.AddComponent<SmMoreAccessories>();
                        this._smMoreAccessories.ReplaceEventsOf(original);
                        this._smMoreAccessories.LoadWith<SubMenuBase>(smAccessory);
                        this._smMoreAccessories.PreInit(smAccessory);
                        GameObject.Destroy(original);
                        this.smItem.menuName = "Test";
                        this.smItem.objTop = obj;
                        break;
                    }

                    Selectable template = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/TabMenu/Tab01").GetComponent<Selectable>();

                    this._addButton = UIUtility.CreateButton("AddAccessoriesButton", this._prefab.parent, "+ Add accessory");
                    this._addButton.transform.SetRect(this._prefab.anchorMin, this._prefab.anchorMax, this._prefab.offsetMin + new Vector2(0f, -this._prefab.rect.height * 1.2f), this._prefab.offsetMax + new Vector2(0f, -this._prefab.rect.height));
                    ((RectTransform)this._addButton.transform).pivot = new Vector2(0.5f, 1f);
                    this._addButton.gameObject.AddComponent<UI_TreeView>();
                    this._addButton.onClick.AddListener(this.AddSlot);
                    this._addButton.colors = template.colors;
                    ((Image)this._addButton.targetGraphic).sprite = ((Image)template.targetGraphic).sprite;
                    Text text = this._addButton.GetComponentInChildren<Text>();
                    text.resizeTextForBestFit = true;
                    text.resizeTextMaxSize = 200;
                    text.rectTransform.SetRect();
                }
            }
            else
            {
                if (level == 3)
                {
                    Transform accList = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot");
                    this._prefab = accList.Find("Slot10") as RectTransform;
                }
            }
            this._ready = true;
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnApplicationQuit()
        {
        }

        public void OnUpdate()
        {
            if (this._binary == Binary.Neo && this._level == 3)
            {
                Studio.TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject != null)
                {
                    Studio.ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    {
                        Studio.OCIChar selected = info as Studio.OCIChar;
                        if (selected != this._selectedStudioCharacter)
                        {
                            this._selectedStudioCharacter = selected;
                            this.UpdateStudioUI();
                        }
                    }
                }
            }
        }

        public void OnLateUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Private Methods
        internal void UpdateMakerGUI()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null || this._prefab == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
            {
                if (i < this.displayedMakerSlots.Count)
                    this.displayedMakerSlots[i].treeView.SetUnused(false);
                else
                {
                    MakerSlotData sd = new MakerSlotData();
                    GameObject obj = GameObject.Instantiate(this._prefab.gameObject);
                    obj.transform.SetParent(this._prefab.parent);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localScale = this._prefab.localScale;
                    Transform selectRect = obj.transform.Find("MainSelectClothes");
                    if (selectRect != null)
                        GameObject.Destroy(selectRect.transform);
                    RectTransform rt = obj.transform as RectTransform;
                    rt.SetRect(this._prefab.anchorMin, this._prefab.anchorMax, this._prefab.offsetMin + new Vector2(0f, -this._prefab.rect.height), this._prefab.offsetMax + new Vector2(0f, -this._prefab.rect.height));
                    sd.button = obj.GetComponent<Button>();
                    sd.text = sd.button.GetComponentInChildren<Text>();
                    sd.treeView = sd.button.GetComponent<UI_TreeView>();
                    sd.button.onClick = new Button.ButtonClickedEvent();
                    string menuStr = "SM_MoreAccessories_" + i;
                    this._mainMenuSelect = GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MMSelectCtrlClothes").GetComponent<MainMenuSelect>();
                    sd.button.onClick.AddListener(() =>
                    {
                        this._smControl.ChangeSubMenu(menuStr);
                        this._mainMenuSelect.OnClick(rt);
                    });

                    this.displayedMakerSlots.Add(sd);
                }
            }
            for (; i < this.displayedMakerSlots.Count; i++)
                this.displayedMakerSlots[i].treeView.SetUnused(true);
            this.CustomControl_UpdateAcsName();
            this._addButton.transform.SetAsLastSibling();
            this._prefab.parent.GetComponent<UI_TreeView>().UpdateView();
        }

        internal void UpdateStudioUI()
        {
            if (this._binary != Binary.Neo || this._selectedStudioCharacter == null || this._level != 3)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
            {
                StudioSlotData slot;
                CharFileInfoClothes.Accessory accessory = additionalData.clothesInfoAccessory[i];
                if (i < this._displayedStudioSlots.Count)
                {
                    slot = this._displayedStudioSlots[i];
                }
                else
                {
                    slot = new StudioSlotData();
                    slot.slot = (RectTransform)GameObject.Instantiate(this._prefab.gameObject).transform;
                    slot.name = slot.slot.GetComponentInChildren<Text>();
                    slot.onButton = slot.slot.GetChild(1).GetComponent<Button>();
                    slot.offButton = slot.slot.GetChild(2).GetComponent<Button>();
                    slot.name.text = "Accessory " + (11 + i);
                    slot.slot.SetParent(this._prefab.parent);
                    slot.slot.localPosition = Vector3.zero;
                    slot.slot.localScale = Vector3.one;
                    int i1 = i;
                    slot.onButton.onClick = new Button.ButtonClickedEvent();
                    slot.onButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessory[i1] = true;
                        slot.onButton.image.color = Color.green;
                        slot.offButton.image.color = Color.white;
                    });
                    slot.offButton.onClick = new Button.ButtonClickedEvent();
                    slot.offButton.onClick.AddListener(() =>
                    {
                        this._accessoriesByChar[this._selectedStudioCharacter.charInfo.chaFile].showAccessory[i1] = false;
                        slot.offButton.image.color = Color.green;
                        slot.onButton.image.color = Color.white;
                    });
                    this._displayedStudioSlots.Add(slot);
                }
                slot.slot.gameObject.SetActive(true);
                slot.onButton.interactable = accessory != null;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.showAccessory[i] ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.showAccessory[i] ? Color.green : Color.white;
            }
            for (; i < this._displayedStudioSlots.Count; ++i)
                this._displayedStudioSlots[i].slot.gameObject.SetActive(false);
        }

        internal void UIFallbackToCoordList()
        {
            this._smControl.ChangeSubMenu(SubMenuControl.SubMenuType.SM_ClothesLoad.ToString());
            this._smControl.ExecuteDelayed(() =>
            {
                if (Manager.Game.Instance.customSceneInfo.isFemale)
                this._mainMenuSelect.OnClickScript(GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_System/SaveDelete") as RectTransform);
                else
                    this._mainMenuSelect.OnClickScript(GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/MaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_System/SaveDelete") as RectTransform);
            }, 2);
        }


        internal void CustomControl_UpdateAcsName()
        {
            for (int i = 0; i < this.charaMakerAdditionalData.clothesInfoAccessory.Count; ++i)
                this.displayedMakerSlots[i].text.text = this.CustomControl_GetAcsName(i, 14);
        }

        internal string CustomControl_GetAcsName(int slotNo, int limit, bool addType = false, bool addNo = true)
        {
            string str1 = string.Empty;
            if (null == this._charaMakerCharInfo)
            {
                Debug.LogWarning("まだ初期化されてない");
                return str1;
            }
            CharFileInfoClothes.Accessory accessory = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo];
            string str2;
            if (this._charaMakerCharInfo.Sex == 0)
            {
                if (accessory.type == -1)
                {
                    str2 = "None";
                }
                else
                {
                    Dictionary<int, ListTypeFbx> accessoryFbxList = this._charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type, true);
                    ListTypeFbx listTypeFbx = null;
                    accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx);
                    str2 = listTypeFbx == null ? "なし" : listTypeFbx.Name;
                }
            }
            else if (accessory.type == -1)
            {
                str2 = "None";
            }
            else
            {
                Dictionary<int, ListTypeFbx> accessoryFbxList = this._charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type, true);
                ListTypeFbx listTypeFbx = null;
                accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx);
                str2 = listTypeFbx == null ? "None" : listTypeFbx.Name;
            }
            if (addNo)
                str2 = (slotNo + 11).ToString("00") + " " + str2;
            if (addType)
                str2 = CharDefine.AccessoryTypeName[accessory.type + 1] + ":" + str2;
            str1 = str2.Substring(0, Mathf.Min(limit, str2.Length));
            return str1;
        }

        private void AddSlot()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            additionalData.clothesInfoAccessory.Add(new CharFileInfoClothes.Accessory());
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this._charaMakerCharInfo.chaBody, additionalData, additionalData.clothesInfoAccessory.Count - 1, -1, -1, "", true);
            this.UpdateMakerGUI();
        }

        private void OnCharaSave(CharFile charFile, XmlTextWriter writer)
        {
            this.OnCharaSaveGeneric(charFile, writer);
        }

        private void OnCharaSaveGeneric(CharFile charFile, XmlTextWriter writer, bool writeVisibility = false)
        {
            CharAdditionalData additionalData;
            if (!this._accessoriesByChar.TryGetValue(charFile, out additionalData))
                return;
            int maxCount = 0;
            foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> kvp in additionalData.rawAccessoriesInfos)
            {
                writer.WriteStartElement("accessorySet");
                writer.WriteAttributeString("type", XmlConvert.ToString((int)kvp.Key));
                foreach (CharFileInfoClothes.Accessory accessory in kvp.Value)
                {
                    writer.WriteStartElement("accessory");

                    if (accessory.type != -1)
                    {
                        writer.WriteAttributeString("type", XmlConvert.ToString(accessory.type));
                        writer.WriteAttributeString("id", XmlConvert.ToString(accessory.id));
                        writer.WriteAttributeString("parentKey", accessory.parentKey);
                        writer.WriteAttributeString("addPosX", XmlConvert.ToString(accessory.addPos.x));
                        writer.WriteAttributeString("addPosY", XmlConvert.ToString(accessory.addPos.y));
                        writer.WriteAttributeString("addPosZ", XmlConvert.ToString(accessory.addPos.z));
                        writer.WriteAttributeString("addRotX", XmlConvert.ToString(accessory.addRot.x));
                        writer.WriteAttributeString("addRotY", XmlConvert.ToString(accessory.addRot.y));
                        writer.WriteAttributeString("addRotZ", XmlConvert.ToString(accessory.addRot.z));
                        writer.WriteAttributeString("addSclX", XmlConvert.ToString(accessory.addScl.x));
                        writer.WriteAttributeString("addSclY", XmlConvert.ToString(accessory.addScl.y));
                        writer.WriteAttributeString("addSclZ", XmlConvert.ToString(accessory.addScl.z));

                        writer.WriteAttributeString("colorHSVDiffuseH", XmlConvert.ToString((double)accessory.color.hsvDiffuse.H));
                        writer.WriteAttributeString("colorHSVDiffuseS", XmlConvert.ToString((double)accessory.color.hsvDiffuse.S));
                        writer.WriteAttributeString("colorHSVDiffuseV", XmlConvert.ToString((double)accessory.color.hsvDiffuse.V));
                        writer.WriteAttributeString("colorAlpha", XmlConvert.ToString((double)accessory.color.alpha));
                        writer.WriteAttributeString("colorHSVSpecularH", XmlConvert.ToString((double)accessory.color.hsvSpecular.H));
                        writer.WriteAttributeString("colorHSVSpecularS", XmlConvert.ToString((double)accessory.color.hsvSpecular.S));
                        writer.WriteAttributeString("colorHSVSpecularV", XmlConvert.ToString((double)accessory.color.hsvSpecular.V));
                        writer.WriteAttributeString("colorSpecularIntensity", XmlConvert.ToString((double)accessory.color.specularIntensity));
                        writer.WriteAttributeString("colorSpecularSharpness", XmlConvert.ToString((double)accessory.color.specularSharpness));

                        writer.WriteAttributeString("color2HSVDiffuseH", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.H));
                        writer.WriteAttributeString("color2HSVDiffuseS", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.S));
                        writer.WriteAttributeString("color2HSVDiffuseV", XmlConvert.ToString((double)accessory.color2.hsvDiffuse.V));
                        writer.WriteAttributeString("color2Alpha", XmlConvert.ToString((double)accessory.color2.alpha));
                        writer.WriteAttributeString("color2HSVSpecularH", XmlConvert.ToString((double)accessory.color2.hsvSpecular.H));
                        writer.WriteAttributeString("color2HSVSpecularS", XmlConvert.ToString((double)accessory.color2.hsvSpecular.S));
                        writer.WriteAttributeString("color2HSVSpecularV", XmlConvert.ToString((double)accessory.color2.hsvSpecular.V));
                        writer.WriteAttributeString("color2SpecularIntensity", XmlConvert.ToString((double)accessory.color2.specularIntensity));
                        writer.WriteAttributeString("color2SpecularSharpness", XmlConvert.ToString((double)accessory.color2.specularSharpness));
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                if (kvp.Value.Count > maxCount)
                    maxCount = kvp.Value.Count;
            }
            if (writeVisibility)
            {
                writer.WriteStartElement("visibility");
                for (int i = 0; i < maxCount && i < additionalData.showAccessory.Count; i++)
                {
                    writer.WriteStartElement("visible");
                    writer.WriteAttributeString("value", XmlConvert.ToString(additionalData.showAccessory[i]));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        private void OnCharaLoad(CharFile charFile, XmlNode node)
        {
            this.OnCharaLoadGeneric(charFile, node);
        }

        private void OnCharaLoadGeneric(CharFile charFile, XmlNode node, bool readVisibility = false)
        {
            CharAdditionalData additionalData;
            if (this._accessoriesByChar.TryGetValue(charFile, out additionalData) == false)
            {
                additionalData = new CharAdditionalData();
                this._accessoriesByChar.Add(charFile, additionalData);
            }
            else if (node == null)
            {
                foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> pair in additionalData.rawAccessoriesInfos) // Useful only in the chara maker
                    pair.Value.Clear();
            }
            if (node != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "accessorySet":
                            CharDefine.CoordinateType type = (CharDefine.CoordinateType)XmlConvert.ToInt32(childNode.Attributes["type"].Value);
                            List<CharFileInfoClothes.Accessory> accessories2;
                            if (additionalData.rawAccessoriesInfos.TryGetValue(type, out accessories2))
                                accessories2.Clear();
                            else
                            {
                                accessories2 = new List<CharFileInfoClothes.Accessory>();
                                additionalData.rawAccessoriesInfos.Add(type, accessories2);
                            }
                            foreach (XmlNode grandChildNode in childNode.ChildNodes)
                            {
                                CharFileInfoClothes.Accessory accessory;
                                if (grandChildNode.Attributes != null && grandChildNode.Attributes["type"] != null && XmlConvert.ToInt32(grandChildNode.Attributes["type"].Value) != -1)
                                    accessory = new CharFileInfoClothes.Accessory
                                    {
                                        type = XmlConvert.ToInt32(grandChildNode.Attributes["type"].Value),
                                        id = XmlConvert.ToInt32(grandChildNode.Attributes["id"].Value),
                                        parentKey = grandChildNode.Attributes["parentKey"].Value,
                                        addPos =
                                        {
                                            x = XmlConvert.ToSingle(grandChildNode.Attributes["addPosX"].Value),
                                            y = XmlConvert.ToSingle(grandChildNode.Attributes["addPosY"].Value),
                                            z = XmlConvert.ToSingle(grandChildNode.Attributes["addPosZ"].Value)
                                        },
                                        addRot =
                                        {
                                            x = XmlConvert.ToSingle(grandChildNode.Attributes["addRotX"].Value),
                                            y = XmlConvert.ToSingle(grandChildNode.Attributes["addRotY"].Value),
                                            z = XmlConvert.ToSingle(grandChildNode.Attributes["addRotZ"].Value)
                                        },
                                        addScl =
                                        {
                                            x = XmlConvert.ToSingle(grandChildNode.Attributes["addSclX"].Value),
                                            y = XmlConvert.ToSingle(grandChildNode.Attributes["addSclY"].Value),
                                            z = XmlConvert.ToSingle(grandChildNode.Attributes["addSclZ"].Value)
                                        },
                                        color = new HSColorSet
                                        {
                                            hsvDiffuse =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseV"].Value)
                                            },
                                            alpha = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorAlpha"].Value),
                                            hsvSpecular =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularV"].Value)
                                            },
                                            specularIntensity = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorSpecularIntensity"].Value),
                                            specularSharpness = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorSpecularSharpness"].Value)
                                        },
                                        color2 = new HSColorSet
                                        {
                                            hsvDiffuse =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseV"].Value)
                                            },
                                            alpha = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2Alpha"].Value),
                                            hsvSpecular =
                                            {
                                                H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularH"].Value),
                                                S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularS"].Value),
                                                V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularV"].Value)
                                            },
                                            specularIntensity = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2SpecularIntensity"].Value),
                                            specularSharpness = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2SpecularSharpness"].Value)
                                        }
                                    };
                                else
                                    accessory = new CharFileInfoClothes.Accessory();
                                accessories2.Add(accessory);
                            }
                            break;
                        case "visibility":
                            if (readVisibility == false)
                                break;
                            additionalData.showAccessory = new List<bool>();
                            foreach (XmlNode grandChildNode in childNode.ChildNodes)
                            {
                                switch (grandChildNode.Name)
                                {
                                    case "visible":
                                        additionalData.showAccessory.Add(grandChildNode.Attributes?["value"] == null || XmlConvert.ToBoolean(grandChildNode.Attributes["value"].Value));
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo == null || this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            this.UpdateMakerGUI();
        }

        private void OnSceneSave(string path, XmlTextWriter xmlWriter)
        {
            SortedDictionary<int, Studio.ObjectCtrlInfo> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> kvp in dic)
            {
                Studio.OCIChar ociChar = kvp.Value as Studio.OCIChar;
                if (ociChar != null)
                {
                    xmlWriter.WriteStartElement("characterInfo");
                    xmlWriter.WriteAttributeString("name", ociChar.charInfo.customInfo.name);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));
                    this.OnCharaSaveGeneric(ociChar.charInfo.chaFile, xmlWriter, true);
                    xmlWriter.WriteEndElement();
                }
            }
        }

        private void OnSceneLoad(string path, XmlNode n)
        {
            if (n == null)
                return;
            XmlNode node = n.CloneNode(true);
            this._routines.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                int i = 0;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    Studio.OCIChar ociChar = null;
                    while (i < dic.Count && (ociChar = dic[i].Value as Studio.OCIChar) == null)
                        ++i;
                    if (i == dic.Count)
                        break;
                    this.OnCharaLoadGeneric(ociChar.charInfo.chaFile, childNode, true);
                    ociChar.charBody.ChangeAccessory();
                    ++i;
                }
            }, 3);
        }
        #endregion

    }
}
