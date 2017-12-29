using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Harmony;
using IllusionPlugin;
using Manager;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessories
{
    public class MoreAccessories : IEnhancedPlugin
    {
        #region private Types
        private class SlotData
        {
            public Button button;
            public Text text;
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
        #endregion

        #region Private Variables
        private readonly Dictionary<CharFile, CharAdditionalData> _accessoriesByChar = new Dictionary<CharFile, CharAdditionalData>();
        private readonly List<SlotData> _displayedSlots = new List<SlotData>();
        private RectTransform _slotsContainer;
        private RectTransform _container;
        private RectTransform _prefab;
        private Vector2 _slotsContainerCachedOffsetMin;
        private CharFile _lastLoaded;
        #endregion

        #region Public Accessors
        public static MoreAccessories self { get; private set; }
        public Dictionary<CharFile, CharAdditionalData> accessoriesByChar { get { return (this._accessoriesByChar); } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32" }; } }
        public string Name { get { return "MoreAccessories"; } }
        public string Version { get { return "1.0.0"; } }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            self = this;
            CharExtSave.CharExtSave.RegisterHandler("moreAccessories", this.OnCharaLoad, this.OnCharaSave);
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelWasInitialized(int level)
        {
            this._slotsContainer = GameObject.Find("CustomScene").transform.Find("CustomControl/CustomUI/CustomMainMenu/W_MainMenu/MainItemTop/FemaleControl/ScrollView/CustomControlPanel/TreeViewRootClothes/TT_Clothes/Accessory") as RectTransform;
            this._slotsContainerCachedOffsetMin = this._slotsContainer.offsetMin;
            this._container = new GameObject("MoreAccessoriesSlots", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            this._container.SetParent(this._slotsContainer);
            this._container.localScale = Vector3.one;
            this._container.localPosition = Vector3.zero;
            this._container.pivot = new Vector2(0.5f, 1f);
            this._container.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -this._slotsContainer.rect.height), new Vector2(1f, - this._slotsContainer.rect.height));

            this._prefab = this._slotsContainer.Find("AcsSlot10") as RectTransform;
        }

        public void OnLevelWasLoaded(int level)
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
            if (Input.GetKeyDown(KeyCode.Plus))
            {
                this.AddSlot();
            }
        }

        private void AddSlot()
        {
            SlotData sd = new SlotData();
            GameObject obj = GameObject.Instantiate(this._prefab.gameObject);
            obj.AddComponent<LayoutElement>().preferredHeight = this._prefab.rect.height;
            obj.transform.SetParent(this._container);
            sd.button = obj.GetComponent<Button>();
            sd.text = sd.button.GetComponentInChildren<Text>();
            sd.button.onClick = new Button.ButtonClickedEvent();
            sd.button.onClick.AddListener(() => UnityEngine.Debug.Log("it's alive"));
            this._displayedSlots.Add(sd);
            sd.text.text = "Accessory " + (11 + this._displayedSlots.Count);
            this._slotsContainer.offsetMin = this._slotsContainerCachedOffsetMin + new Vector2(0f, -this._prefab.rect.height * this._displayedSlots.Count);

            CharAdditionalData additionalData = this._accessoriesByChar[this._lastLoaded];
            additionalData.clothesInfoAccessory.Add(new CharFileInfoClothes.Accessory());
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
        }

        private void OnCharaSave(CharFile charFile, XmlTextWriter writer)
        {
            CharAdditionalData additionalData;
            if (this._accessoriesByChar.TryGetValue(charFile, out additionalData))
            {
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
        }

        private void OnCharaLoad(CharFile charFile, XmlNode node)
        {
            CharAdditionalData additionalData;
            if (this._accessoriesByChar.TryGetValue(charFile, out additionalData) == false)
            {
                additionalData = new CharAdditionalData();
                this._accessoriesByChar.Add(charFile, additionalData);
            }
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
            this._lastLoaded = charFile;
        }
        #endregion
    }
}
