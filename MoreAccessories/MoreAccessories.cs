using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionPlugin;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoreAccessories
{
    public class MoreAccessories : IEnhancedPlugin
    {
        #region private Types
        private enum Binary
        {
            Neo,
            Game,
        }
        #endregion

        #region Public Types
        public class CharAdditionalData
        {
            public List<GameObject> objAccessory = new List<GameObject>();
            public List<CharFileInfoClothes.Accessory> clothesInfoAccessory = new List<CharFileInfoClothes.Accessory>();
            public List<ListTypeFbx> infoAccessory = new List<ListTypeFbx>();
            public List<GameObject> objAcsMove = new List<GameObject>();
            public Dictionary<int, List<GameObject>> charInfoDictTagObj = new Dictionary<int, List<GameObject>>();

            public Dictionary<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> rawAccessoriesInfos = new Dictionary<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>>();
        }
        public class SlotData
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
        private readonly List<SlotData> _displayedSlots = new List<SlotData>();
        private int _level;
        #endregion

        #region Public Accessors
        public static MoreAccessories self { get; private set; }
        public Dictionary<CharFile, CharAdditionalData> accessoriesByChar { get { return (this._accessoriesByChar); } }
        public CharAdditionalData charaMakerAdditionalData { get; private set; }
        public SubMenuItem smItem { get; } = new SubMenuItem();
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64" }; } }
        public string Name { get { return "MoreAccessories"; } }
        public string Version { get { return "1.0.0"; } }
        public List<SlotData> displayedSlots { get { return this._displayedSlots; } }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            self = this;
            CharExtSave.CharExtSave.RegisterHandler("moreAccessories", this.OnCharaLoad, this.OnCharaSave);


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
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelWasLoaded(int level)
        {
            this._level = level;
            if (this._binary == Binary.Game && level == 21)
            {
                this._prefab = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_Clothes/Accessory/AcsSlot10").transform as RectTransform;

                Dictionary<CharFile, CharAdditionalData> newDic = new Dictionary<CharFile, CharAdditionalData>();
                foreach (KeyValuePair<CharFile, CharAdditionalData> pair in this._accessoriesByChar)
                {
                    if (pair.Key != null)
                        newDic.Add(pair.Key, pair.Value);
                }
                this._accessoriesByChar = newDic;
                this._displayedSlots.Clear();

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
            this.GUILogic();
        }

        public void OnLateUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Private Methods

        private void GUILogic()
        {
            if (this._binary == Binary.Game && this._ready && this._level == 21 && Input.GetKeyDown(KeyCode.A))
            {
                this.AddSlot();
            }
        }

        internal void UpdateGUI()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null || this._prefab == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            int i;
            for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
            {
                if (i < this.displayedSlots.Count)
                    this.displayedSlots[i].treeView.SetUnused(false);
                else
                {
                    SlotData sd = new SlotData();
                    GameObject obj = GameObject.Instantiate(this._prefab.gameObject);
                    obj.transform.SetParent(this._prefab.parent);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localScale = this._prefab.localScale;
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

                    this.displayedSlots.Add(sd);
                }
            }
            for (; i < this.displayedSlots.Count; i++)
                this.displayedSlots[i].treeView.SetUnused(true);
            this.CustomControl_UpdateAcsName();
            this._prefab.parent.GetComponent<UI_TreeView>().UpdateView();
        }

        internal void UIFallbackToCoordList()
        {
            this._smControl.ChangeSubMenu(SubMenuControl.SubMenuType.SM_ClothesLoad.ToString());
            this._smControl.ExecuteDelayed(() =>
            {
                this._mainMenuSelect.OnClickScript(GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_System/SaveDelete") as RectTransform); //TODO faire mieux
            }, 2);
        }


        internal void CustomControl_UpdateAcsName()
        {
            for (int i = 0; i < this.charaMakerAdditionalData.clothesInfoAccessory.Count; ++i)
                this.displayedSlots[i].text.text = this.CustomControl_GetAcsName(i, 14);
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
            CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this._charaMakerCharInfo.chaBody, additionalData, additionalData.clothesInfoAccessory.Count - 1, -1, -1, "", true);
            this.UpdateGUI();
        }

        private void OnCharaSave(CharFile charFile, XmlTextWriter writer)
        {
            CharAdditionalData additionalData;
            if (!this._accessoriesByChar.TryGetValue(charFile, out additionalData))
                return;
            foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> kvp in additionalData.rawAccessoriesInfos)
            {
                writer.WriteStartElement("accessorySet");
                writer.WriteAttributeString("type", XmlConvert.ToString((int)kvp.Key));
                foreach (CharFileInfoClothes.Accessory accessory in kvp.Value)
                {
                    writer.WriteStartElement("accessory");

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

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        private void OnCharaLoad(CharFile charFile, XmlNode node)
        {
            foreach (CharInfo charInfo in Resources.FindObjectsOfTypeAll<CharInfo>())
                if (charInfo.chaFile == charFile)
                    this._charaMakerCharInfo = charInfo;

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

            if (this._charaMakerCharInfo != null)
                this.charaMakerAdditionalData = additionalData;
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
                                CharFileInfoClothes.Accessory accessory = new CharFileInfoClothes.Accessory();

                                accessory.type = XmlConvert.ToInt32(grandChildNode.Attributes["type"].Value);
                                accessory.id = XmlConvert.ToInt32(grandChildNode.Attributes["id"].Value);
                                accessory.parentKey = grandChildNode.Attributes["parentKey"].Value;
                                accessory.addPos.x = XmlConvert.ToSingle(grandChildNode.Attributes["addPosX"].Value);
                                accessory.addPos.y = XmlConvert.ToSingle(grandChildNode.Attributes["addPosY"].Value);
                                accessory.addPos.z = XmlConvert.ToSingle(grandChildNode.Attributes["addPosZ"].Value);
                                accessory.addRot.x = XmlConvert.ToSingle(grandChildNode.Attributes["addRotX"].Value);
                                accessory.addRot.y = XmlConvert.ToSingle(grandChildNode.Attributes["addRotY"].Value);
                                accessory.addRot.z = XmlConvert.ToSingle(grandChildNode.Attributes["addRotZ"].Value);
                                accessory.addScl.x = XmlConvert.ToSingle(grandChildNode.Attributes["addSclX"].Value);
                                accessory.addScl.y = XmlConvert.ToSingle(grandChildNode.Attributes["addSclY"].Value);
                                accessory.addScl.z = XmlConvert.ToSingle(grandChildNode.Attributes["addSclZ"].Value);

                                accessory.color.hsvDiffuse.H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseH"].Value);
                                accessory.color.hsvDiffuse.S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseS"].Value);
                                accessory.color.hsvDiffuse.V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVDiffuseV"].Value);
                                accessory.color.alpha = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorAlpha"].Value);
                                accessory.color.hsvSpecular.H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularH"].Value);
                                accessory.color.hsvSpecular.S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularS"].Value);
                                accessory.color.hsvSpecular.V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorHSVSpecularV"].Value);
                                accessory.color.specularIntensity = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorSpecularIntensity"].Value);
                                accessory.color.specularSharpness = (float)XmlConvert.ToDouble(grandChildNode.Attributes["colorSpecularSharpness"].Value);

                                accessory.color2.hsvDiffuse.H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseH"].Value);
                                accessory.color2.hsvDiffuse.S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseS"].Value);
                                accessory.color2.hsvDiffuse.V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVDiffuseV"].Value);
                                accessory.color2.alpha = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2Alpha"].Value);
                                accessory.color2.hsvSpecular.H = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularH"].Value);
                                accessory.color2.hsvSpecular.S = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularS"].Value);
                                accessory.color2.hsvSpecular.V = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2HSVSpecularV"].Value);
                                accessory.color2.specularIntensity = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2SpecularIntensity"].Value);
                                accessory.color2.specularSharpness = (float)XmlConvert.ToDouble(grandChildNode.Attributes["color2SpecularSharpness"].Value);

                                accessories2.Add(accessory);
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
            this.UpdateGUI();
        }
        #endregion
    }
}
