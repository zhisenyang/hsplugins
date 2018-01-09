using System;
using System.Collections.Generic;
using CustomMenu;
using IllusionUtility.GetUtility;
using IllusionUtility.SetUtility;
using UnityEngine;
using UnityEngine.UI;

namespace MoreAccessories
{
    public class SmMoreAccessories : SubMenuBase
    {
        #region Protected Variables
        protected readonly float[] movePosValue = {
            0.1f,
            1f,
            10f
        };
        protected readonly float[] moveRotValue = {
            1f,
            5f,
            10f
        };
        protected readonly float[] moveSclValue = {
            0.01f,
            0.1f,
            1f
        };
        protected bool initFlags;
        protected bool nowChanging;
        protected bool nowLoading;
        protected bool updateVisualOnly;
        protected int selectColorNo;
        protected byte modePos = 1;
        protected byte modeRot = 1;
        protected byte modeScl = 1;
        protected int firstIndex;
        protected bool nowTglAllSet;
        protected Toggle[] tglType = new Toggle[13];
        protected int acsType;
        protected Toggle[] tglParent = new Toggle[29];
        protected Dictionary<string, int> dictParentKey = new Dictionary<string, int>();
        protected string[] strParentKey = {
            "AP_Head",
            "AP_Megane",
            "AP_Nose",
            "AP_Mouth",
            "AP_Earring_L",
            "AP_Earring_R",
            "AP_Neck",
            "AP_Chest",
            "AP_Waist",
            "AP_Tikubi_L",
            "AP_Tikubi_R",
            "AP_Shoulder_L",
            "AP_Shoulder_R",
            "AP_Arm_L",
            "AP_Arm_R",
            "AP_Wrist_L",
            "AP_Wrist_R",
            "AP_Hand_L",
            "AP_Hand_R",
            "AP_Index_L",
            "AP_Middle_L",
            "AP_Ring_L",
            "AP_Index_R",
            "AP_Middle_R",
            "AP_Ring_R",
            "AP_Leg_L",
            "AP_Leg_R",
            "AP_Ankle_L",
            "AP_Ankle_R"
        };
        protected Text[] txtDstCopy = new Text[10];
        protected Text[] txtSrcCopy = new Text[10];
        protected List<GameObject> lstTagColor = new List<GameObject>();
        protected bool initEnd;
        protected bool flagMovePos;
        protected bool flagMoveRot;
        protected bool flagMoveScl;
        protected int indexMovePos;
        protected int indexMoveRot;
        protected float downTimeCnt;
        protected float loopTimeCnt;
        protected int indexMoveScl;
        #endregion

        #region Public Variables
        public Toggle tglTab;
        public Toggle tab02;
        public Toggle tab03;
        public Toggle tab04;
        public Toggle tab05;
        public ToggleGroup grpType;
        public ToggleGroup grpParent;
        public GameObject objListTop;
        public GameObject objLineBase;
        public RectTransform rtfPanel;
        public Image[] imgDiffuse;
        public Image[] imgSpecular;
        public Slider sldIntensity;
        public InputField inputIntensity;
        public Slider[] sldSharpness;
        public InputField[] inputSharpness;
        public GameObject objSubColor;
        public InputField inputPosX;
        public InputField inputPosY;
        public InputField inputPosZ;
        public InputField inputRotX;
        public InputField inputRotY;
        public InputField inputRotZ;
        public InputField inputSclX;
        public InputField inputSclY;
        public InputField inputSclZ;
        public Toggle[] tglDstCopy;
        public Toggle[] tglSrcCopy;
        public Toggle tglReversal;
        #endregion

        #region Public Accessors
        public CharFile charFile { get { return this.chaInfo.chaFile; } }
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (this.initEnd)
                MoreAccessories.self.CustomControl_UpdateAcsName();
        }

        public new virtual void Start()
        {
            if (!this.initEnd)
            {
                this.Init();
            }
        }

        public new virtual void Update()
        {
            if (this.flagMovePos)
            {
                this.downTimeCnt += Time.deltaTime;
                if (this.downTimeCnt > 0.5)
                {
                    this.loopTimeCnt += Time.deltaTime;
                    while (this.loopTimeCnt > 0.05000000074505806)
                    {
                        this.OnClickPos(this.indexMovePos);
                        this.loopTimeCnt -= 0.05f;
                    }
                }
            }
            if (this.flagMoveRot)
            {
                this.downTimeCnt += Time.deltaTime;
                if (this.downTimeCnt > 0.5)
                {
                    this.loopTimeCnt += Time.deltaTime;
                    while (this.loopTimeCnt > 0.05000000074505806)
                    {
                        this.OnClickRot(this.indexMoveRot);
                        this.loopTimeCnt -= 0.05f;
                    }
                }
            }
            if (this.flagMoveScl)
            {
                this.downTimeCnt += Time.deltaTime;
                if (this.downTimeCnt > 0.5)
                {
                    this.loopTimeCnt += Time.deltaTime;
                    while (this.loopTimeCnt > 0.05000000074505806)
                    {
                        this.OnClickScl(this.indexMoveScl);
                        this.loopTimeCnt -= 0.05f;
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void PreInit(SmAccessory original)
        {
            this.tglTab = this.transform.Find(original.tglTab.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab02 = this.transform.Find(original.tab02.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab03 = this.transform.Find(original.tab03.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab04 = this.transform.Find(original.tab04.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.tab05 = this.transform.Find(original.tab05.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();
            this.grpType = this.transform.Find(original.grpType.transform.GetPathFrom(original.transform)).GetComponent<ToggleGroup>();
            this.grpParent = this.transform.Find(original.grpParent.transform.GetPathFrom(original.transform)).GetComponent<ToggleGroup>();
            this.objListTop = this.transform.Find(original.objListTop.transform.GetPathFrom(original.transform)).gameObject;
            this.objLineBase = original.objLineBase;
            this.rtfPanel = this.transform.Find(original.rtfPanel.transform.GetPathFrom(original.transform)).GetComponent<RectTransform>();

            this.imgDiffuse = new Image[original.imgDiffuse.Length];
            for (int i = 0; i < this.imgDiffuse.Length; i++)
                this.imgDiffuse[i] = this.transform.Find(original.imgDiffuse[i].transform.GetPathFrom(original.transform)).GetComponent<Image>();

            this.imgSpecular = new Image[original.imgSpecular.Length];
            for (int i = 0; i < this.imgSpecular.Length; i++)
                this.imgSpecular[i] = this.transform.Find(original.imgSpecular[i].transform.GetPathFrom(original.transform)).GetComponent<Image>();

            this.sldIntensity = this.transform.Find(original.sldIntensity.transform.GetPathFrom(original.transform)).GetComponent<Slider>();
            this.inputIntensity = this.transform.Find(original.inputIntensity.transform.GetPathFrom(original.transform)).GetComponent<InputField>();

            this.sldSharpness = new Slider[original.sldSharpness.Length];
            for (int i = 0; i < this.sldSharpness.Length; i++)
                this.sldSharpness[i] = this.transform.Find(original.sldSharpness[i].transform.GetPathFrom(original.transform)).GetComponent<Slider>();

            this.inputSharpness = new InputField[original.inputSharpness.Length];
            for (int i = 0; i < this.inputSharpness.Length; i++)
                this.inputSharpness[i] = this.transform.Find(original.inputSharpness[i].transform.GetPathFrom(original.transform)).GetComponent<InputField>();

            this.objSubColor = this.transform.Find(original.objSubColor.transform.GetPathFrom(original.transform)).gameObject;
            this.inputPosX = this.transform.Find(original.inputPosX.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputPosX.onEndEdit = new InputField.SubmitEvent();
            this.inputPosX.onEndEdit.AddListener((s) => this.OnInputEndPos(0));

            this.inputPosY = this.transform.Find(original.inputPosY.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputPosY.onEndEdit = new InputField.SubmitEvent();
            this.inputPosY.onEndEdit.AddListener((s) => this.OnInputEndPos(1));

            this.inputPosZ = this.transform.Find(original.inputPosZ.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputPosZ.onEndEdit = new InputField.SubmitEvent();
            this.inputPosZ.onEndEdit.AddListener((s) => this.OnInputEndPos(2));


            this.inputRotX = this.transform.Find(original.inputRotX.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputRotX.onEndEdit = new InputField.SubmitEvent();
            this.inputRotX.onEndEdit.AddListener((s) => this.OnInputEndRot(0));

            this.inputRotY = this.transform.Find(original.inputRotY.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputRotY.onEndEdit = new InputField.SubmitEvent();
            this.inputRotY.onEndEdit.AddListener((s) => this.OnInputEndRot(1));

            this.inputRotZ = this.transform.Find(original.inputRotZ.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputRotZ.onEndEdit = new InputField.SubmitEvent();
            this.inputRotZ.onEndEdit.AddListener((s) => this.OnInputEndRot(2));


            this.inputSclX = this.transform.Find(original.inputSclX.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputSclX.onEndEdit = new InputField.SubmitEvent();
            this.inputSclX.onEndEdit.AddListener((s) => this.OnInputEndScl(0));

            this.inputSclY = this.transform.Find(original.inputSclY.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputSclY.onEndEdit = new InputField.SubmitEvent();
            this.inputSclY.onEndEdit.AddListener((s) => this.OnInputEndScl(1));

            this.inputSclZ = this.transform.Find(original.inputSclZ.transform.GetListPathFrom(original.transform)).GetComponent<InputField>();
            this.inputSclZ.onEndEdit = new InputField.SubmitEvent();
            this.inputSclZ.onEndEdit.AddListener((s) => this.OnInputEndScl(2));


            this.tglDstCopy = new Toggle[original.tglDstCopy.Length];
            for (int i = 0; i < this.tglDstCopy.Length; i++)
                this.tglDstCopy[i] = this.transform.Find(original.tglDstCopy[i].transform.GetPathFrom(original.transform)).GetComponent<Toggle>();

            this.tglSrcCopy = new Toggle[original.tglSrcCopy.Length];
            for (int i = 0; i < this.tglSrcCopy.Length; i++)
                this.tglSrcCopy[i] = this.transform.Find(original.tglSrcCopy[i].transform.GetPathFrom(original.transform)).GetComponent<Toggle>();

            this.tglReversal = this.transform.Find(original.tglReversal.transform.GetPathFrom(original.transform)).GetComponent<Toggle>();

            Toggle[] originalTglType = (Toggle[])original.GetPrivate("tglType");
            this.tglType = new Toggle[originalTglType.Length];

            this.SetChangeValueSharpnessHandler(this.sldSharpness[0], 0);
            this.SetChangeValueSharpnessHandler(this.sldSharpness[1], 1);
            this.SetInputEndSharpnessHandler(this.inputSharpness[0], 0);
            this.SetInputEndSharpnessHandler(this.inputSharpness[1], 1);
        }

        public virtual void Init()
        {
            for (int i = 0; i < this.strParentKey.Length; i++)
                this.dictParentKey[this.strParentKey[i]] = i;
            GameObject gameObject = this.transform.FindLoop("TypeCategory");
            if (gameObject)
            {
                for (int j = 0; j < this.tglType.Length; j++)
                {
                    string name = "Cate" + j.ToString("00");
                    GameObject gameObject2 = gameObject.transform.FindLoop(name);
                    if (gameObject2)
                        this.tglType[j] = gameObject2.GetComponent<Toggle>();
                }
            }
            GameObject gameObject3 = this.transform.FindLoop("ParentCategory");
            if (gameObject3)
            {
                for (int k = 0; k < this.tglParent.Length; k++)
                {
                    string name2 = "Cate" + k.ToString("00");
                    GameObject gameObject4 = gameObject3.transform.FindLoop(name2);
                    if (gameObject4)
                        this.tglParent[k] = gameObject4.GetComponent<Toggle>();
                }
            }
            for (int l = 0; l < 10; l++)
            {
                Transform transform = this.tglDstCopy[l].transform.FindChild("Label");
                if (transform)
                {
                    this.txtDstCopy[l] = transform.GetComponent<Text>();
                    this.txtDstCopy[l].text = "アクセサリ" + (l + 1).ToString("00");
                }
                transform = this.tglSrcCopy[l].transform.FindChild("Label");
                if (transform)
                {
                    this.txtSrcCopy[l] = transform.GetComponent<Text>();
                    this.txtSrcCopy[l].text = "アクセサリ" + (l + 1).ToString("00");
                }
            }
            this.initEnd = true;
        }


        public virtual void OnEnableSetListAccessoryName()
        {
            for (int i = 0; i < 10; i++)
            {
                string acsName = this.customControl.GetAcsName(i, 10, true, false);
                if (this.txtDstCopy[i])
                    this.txtDstCopy[i].text = acsName;
                if (this.txtSrcCopy[i])
                    this.txtSrcCopy[i].text = acsName;
            }
        }

        public virtual int CheckDstSelectNo()
        {
            for (int i = 0; i < this.tglDstCopy.Length; i++)
                if (this.tglDstCopy[i].isOn)
                    return i;
            return 0;
        }

        public virtual int CheckSrcSelectNo()
        {
            for (int i = 0; i < this.tglSrcCopy.Length; i++)
                if (this.tglSrcCopy[i].isOn)
                    return i;
            return 0;
        }

        public virtual void OnCopyAll()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num != num2)
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2]);
                if (this.tglReversal.isOn)
                {
                    switch (MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey)
                    {
                        case "AP_Earring_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Earring_R";
                            break;
                        case "AP_Earring_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Earring_L";
                            break;
                        case "AP_Tikubi_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Tikubi_R";
                            break;
                        case "AP_Tikubi_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Tikubi_L";
                            break;
                        case "AP_Shoulder_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Shoulder_R";
                            break;
                        case "AP_Shoulder_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Shoulder_L";
                            break;
                        case "AP_Arm_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Arm_R";
                            break;
                        case "AP_Arm_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Arm_L";
                            break;
                        case "AP_Wrist_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Wrist_R";
                            break;
                        case "AP_Wrist_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Wrist_L";
                            break;
                        case "AP_Hand_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Hand_R";
                            break;
                        case "AP_Hand_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Hand_L";
                            break;
                        case "AP_Index_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Index_R";
                            break;
                        case "AP_Index_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Index_L";
                            break;
                        case "AP_Middle_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Middle_R";
                            break;
                        case "AP_Middle_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Middle_L";
                            break;
                        case "AP_Ring_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Ring_R";
                            break;
                        case "AP_Ring_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Ring_L";
                            break;
                        case "AP_Leg_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Leg_R";
                            break;
                        case "AP_Leg_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Leg_L";
                            break;
                        case "AP_Ankle_L":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Ankle_R";
                            break;
                        case "AP_Ankle_R":
                            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey = "AP_Ankle_L";
                            break;
                    }
                }
                CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this.chaInfo.chaBody, MoreAccessories.self.charaMakerAdditionalData, num, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].type, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].id, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].parentKey, true);
                MoreAccessories.self.CustomControl_UpdateAcsName();
                this.OnEnableSetListAccessoryName();
                this.UpdateCharaInfoSub();
                
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][num].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num]);
            }
        }

        public virtual void OnCopyCorrect()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addPos = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addPos;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addRot;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addScl = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addScl;
            this.CharClothes_UpdateAccessoryMoveFromInfo(num);
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][num].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num]);
        }

        public virtual void OnCopyCorrectReversalLR()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addPos = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addPos;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addRot;
            if (MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot.y >= 360.0)
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot.y -= 360f;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addScl = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addScl;
            this.CharClothes_UpdateAccessoryMoveFromInfo(num);
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][num].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num]);
        }

        public virtual void OnCopyCorrectReversalTB()
        {
            int num = this.CheckDstSelectNo();
            int num2 = this.CheckSrcSelectNo();
            if (num == num2)
                return;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addPos = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addPos;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addRot;
            if (MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot.x >= 360.0)
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addRot.x -= 360f;
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num].addScl = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num2].addScl;
            this.CharClothes_UpdateAccessoryMoveFromInfo(num);
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][num].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[num]);
        }

        public virtual int GetParentIndexFromParentKey(string key)
        {
            return !this.dictParentKey.ContainsKey(key) ? 0 : this.dictParentKey[key];
        }

        public virtual int GetSlotNoFromSubMenuSelect()
        {
            return this.nowSubMenuTypeId - (int)SubMenuControl.SubMenuType.SM_Delete - 1;
        }

        public virtual void OnChangeAccessoryType(int newType)
        {
            int num = newType + 1;
            if (this.initFlags || null == this.chaInfo || null == this.grpType)
                return;
            Toggle toggle = this.tglType[num];
            if (null == toggle || !toggle.isOn)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type == newType)
                return;
            this.nowLoading = true;
            this.ChangeAccessoryTypeList(newType, -1);
            this.nowLoading = false;
            CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this.chaBody, MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect, newType, this.firstIndex, string.Empty);
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect]);
            this.UpdateShowTab();
            this.CharClothes_ResetAccessoryMove(slotNoFromSubMenuSelect);
            MoreAccessories.self.CustomControl_UpdateAcsName();
        }

        public virtual void UpdateOnEnableSelectParent()
        {
            if (this.clothesInfo == null)
                return;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglParent.Length; i++)
                this.tglParent[i].isOn = false;
            this.nowTglAllSet = false;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int parentIndexFromParentKey = this.GetParentIndexFromParentKey(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey);
            this.updateVisualOnly = true;
            this.tglParent[parentIndexFromParentKey].isOn = true;
            this.updateVisualOnly = false;
        }

        public virtual void OnChangeAccessoryParentDefault()
        {
            if (this.clothesInfo == null)
                return;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglParent.Length; i++)
                this.tglParent[i].isOn = false;
            this.nowTglAllSet = false;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int id = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id;
            string accessoryDefaultParentStr = this.chaClothes.GetAccessoryDefaultParentStr(this.acsType, id);
            int parentIndexFromParentKey = this.GetParentIndexFromParentKey(accessoryDefaultParentStr);
            this.tglParent[parentIndexFromParentKey].isOn = true;
        }

        public virtual void OnChangeAccessoryParent(int newParent)
        {
            if (this.nowTglAllSet || this.updateVisualOnly || this.initFlags || null == this.chaInfo || null == this.grpParent)
                return;
            Toggle toggle = this.tglParent[newParent];
            if (null == toggle || !toggle.isOn)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            string parentKey = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey;
            if (parentKey == this.strParentKey[newParent])
                return;
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryParent(this.chaBody, MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect, this.strParentKey[newParent]);
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].parentKey = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey;
            this.CharClothes_ResetAccessoryMove(slotNoFromSubMenuSelect);
        }

        public virtual void OnChangeIndex(GameObject objClick, bool value)
        {
            Toggle component = objClick.GetComponent<Toggle>();
            if (!component.isOn)
                return;
            FbxTypeInfo component2 = objClick.GetComponent<FbxTypeInfo>();
            if (null == component2)
                return;
            if (this.clothesInfo != null)
            {
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                string parentKey = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].parentKey;
                int id = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id;
                string accessoryDefaultParentStr = this.chaClothes.GetAccessoryDefaultParentStr(this.acsType, id);
                string parentKey2;
                string b = parentKey2 = this.chaClothes.GetAccessoryDefaultParentStr(this.acsType, component2.id);
                if (accessoryDefaultParentStr != parentKey && accessoryDefaultParentStr == b)
                    parentKey2 = parentKey;
                if (!this.nowLoading)
                {
                    CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this.chaBody, MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect, this.acsType, component2.id, parentKey2);
                    MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].Copy(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect]);
                    this.UpdateShowTab();
                }
            }
            if (!this.chaInfo.customInfo.isConcierge && CharaListInfo.CheckCustomID(component2.info.Category, component2.info.Id) != 2)
            {
                CharaListInfo.AddCustomID(component2.info.Category, component2.info.Id, 2);
                Transform transform = objClick.transform.FindChild("imgNew");
                if (transform)
                    transform.gameObject.SetActive(false);
            }
            MoreAccessories.self.CustomControl_UpdateAcsName();
        }

        public virtual void OnClickColorDiffuse(int no)
        {
            if (!this.colorMenu)
                return;
            this.selectColorNo = no;
            this.colorMenu.updateColorFunc = this.UpdateColorDiffuse;
            Color white = Color.white;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                this.colorMenu.ChangeWindowTitle("[Accessory " + (11 + slotNoFromSubMenuSelect) + "] Color");
                white = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.rgbDiffuse;
            }
            else
            {
                this.colorMenu.ChangeWindowTitle("[Accessory " + (11 + slotNoFromSubMenuSelect) + "] Shine Color");
                white = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.rgbDiffuse;
            }
            this.colorMenu.SetColor(white, UI_ColorInfo.ControlType.PresetsSample);
        }

        public virtual void UpdateColorDiffuse(Color color)
        {
            if (this.imgDiffuse[this.selectColorNo])
                this.imgDiffuse[this.selectColorNo].color = new Color(color.r, color.g, color.b);
            if (this.clothesInfo == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.SetDiffuseRGB(color);
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.SetDiffuseRGB(color);
            }
            else
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.SetDiffuseRGB(color);
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.SetDiffuseRGB(color);
            }
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
        }

        public virtual void OnClickColorSpecular(int no)
        {
            if (!this.colorMenu)
                return;
            this.selectColorNo = no;
            this.colorMenu.updateColorFunc = this.UpdateColorSpecular;
            Color white = Color.white;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                this.colorMenu.ChangeWindowTitle("【アクセサリ(スロット" + (11 + slotNoFromSubMenuSelect) + ")】ツヤの色");
                white = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.rgbSpecular;
            }
            else
            {
                this.colorMenu.ChangeWindowTitle("【アクセサリ(スロット" + (11 + slotNoFromSubMenuSelect) + ")】サブカラ\u30fcのツヤの色");
                white = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.rgbSpecular;
            }
            this.colorMenu.SetColor(white, UI_ColorInfo.ControlType.PresetsSample);
        }

        public virtual void UpdateColorSpecular(Color color)
        {
            if (this.imgSpecular[this.selectColorNo])
                this.imgSpecular[this.selectColorNo].color = new Color(color.r, color.g, color.b);
            if (this.clothesInfo == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.selectColorNo == 0)
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.SetSpecularRGB(color);
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.SetSpecularRGB(color);
            }
            else
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.SetSpecularRGB(color);
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.SetSpecularRGB(color);
            }
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
        }

        public virtual void OnValueChangeIntensity(float value)
        {
            if (this.nowChanging)
                return;
            if (this.clothesInfo != null)
            {
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity = value;
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularIntensity = value;
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.inputIntensity)
                this.inputIntensity.text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnInputEndIntensity(string text)
        {
            if (this.nowChanging)
                return;
            float num = this.ChangeFloatFromText(ref text);
            if (this.clothesInfo != null)
            {
                float specularIntensity = num;
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity = specularIntensity;
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularIntensity = specularIntensity;
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.sldIntensity)
                this.sldIntensity.value = num;
            if (this.inputIntensity)
                this.inputIntensity.text = text;
            this.nowChanging = false;
        }

        public virtual void OnClickIntensity()
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = ((this.chaInfo.Sex != 0) ? this.defClothesInfoF.accessory[slotNoFromSubMenuSelect].color.specularIntensity : this.defClothesInfoM.accessory[slotNoFromSubMenuSelect].color.specularIntensity);
            MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity = num;
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularIntensity = num;
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            this.nowChanging = true;
            float value = num;
            if (this.sldIntensity)
                this.sldIntensity.value = value;
            if (this.inputIntensity)
                this.inputIntensity.text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnValueChangeSharpness(int no, float value)
        {
            if (this.nowChanging)
                return;
            if (this.clothesInfo != null)
            {
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                if (no == 0)
                {
                    MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness = value;
                    MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularSharpness = value;
                }
                else
                {
                    MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness = value;
                    MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.specularSharpness = value;
                }
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.inputSharpness[no])
                this.inputSharpness[no].text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnInputEndSharpness(int no, string text)
        {
            if (this.nowChanging)
                return;
            float num = this.ChangeFloatFromText(ref text);
            if (this.clothesInfo != null)
            {
                float specularSharpness = num;
                int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
                if (no == 0)
                {
                    MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness = specularSharpness;
                    MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularSharpness = specularSharpness;
                }
                else
                {
                    MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness = specularSharpness;
                    MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.specularSharpness = specularSharpness;
                }
                CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            }
            this.nowChanging = true;
            if (this.sldSharpness[no])
                this.sldSharpness[no].value = num;
            if (this.inputSharpness[no])
                this.inputSharpness[no].text = text;
            this.nowChanging = false;
        }

        public virtual void OnClickSharpness(int no)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = no != 0 ? MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness;
               
            if (no == 0)
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness = num;
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color.specularSharpness = num;
            }
            else
            {
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness = num;
                MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].color2.specularSharpness = num;
            }
            CharBody_ChangeAccessory_Patches.CharClothes_ChangeAccessoryColor(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            this.nowChanging = true;
            float value = num;
            if (this.sldSharpness[no])
                this.sldSharpness[no].value = value;
            if (this.inputSharpness[no])
                this.inputSharpness[no].text = this.ChangeTextFromFloat(value);
            this.nowChanging = false;
        }

        public virtual void OnChangeModePos(UI_Parameter param)
        {
            if (!param)
                return;
            Toggle component = param.GetComponent<Toggle>();
            if (component && component.isOn)
                this.modePos = (byte)param.index;
        }

        public virtual void OnChangeModeRot(UI_Parameter param)
        {
            if (!param)
                return;
            Toggle component = param.GetComponent<Toggle>();
            if (component && component.isOn)
                this.modeRot = (byte)param.index;
        }

        public virtual void OnChangeModeScl(UI_Parameter param)
        {
            if (!param)
                return;
            Toggle component = param.GetComponent<Toggle>();
            if (component && component.isOn)
                this.modeScl = (byte)param.index;
        }

        public virtual string GetTextFormValue(float value, int keta = 0)
        {
            float num = value;
            for (int num2 = 0; num2 < keta; num2++)
                num = (float)(num * 10.0);
            num = Mathf.Round(num);
            float num3 = 1f;
            for (int num4 = 0; num4 < keta; num4++)
                num3 = (float)(num3 * 0.10000000149011612);
            num *= num3;
            switch (keta)
            {
                case 0:
                    return num.ToString();
                case 1:
                    return num.ToString("0.0");
                case 2:
                    return num.ToString("0.00");
                case 3:
                    return num.ToString("0.000");
                default:
                    return string.Empty;
            }
        }

        public virtual void OnPointerDownPos(int index)
        {
            this.OnClickPos(index);
            this.indexMovePos = index;
            this.flagMovePos = true;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnPointerDownRot(int index)
        {
            this.OnClickRot(index);
            this.indexMoveRot = index;
            this.flagMoveRot = true;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnPointerDownScl(int index)
        {
            this.OnClickScl(index);
            this.indexMoveScl = index;
            this.flagMoveScl = true;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnPointerUp()
        {
            this.flagMovePos = false;
            this.flagMoveRot = false;
            this.flagMoveScl = false;
            this.downTimeCnt = 0f;
            this.loopTimeCnt = 0f;
        }

        public virtual void OnClickPos(int index)
        {
            UnityEngine.Debug.Log("position clicked " + System.Environment.StackTrace);
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = (float)(this.movePosValue[this.modePos] * 0.0099999997764825821);
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 1);
                    this.nowChanging = true;
                    float value = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
                    if (this.inputPosX)
                        this.inputPosX.text = this.GetTextFormValue(value, 1);
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, true, 1);
                    this.nowChanging = true;
                    float value2 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
                    if (this.inputPosX)
                        this.inputPosX.text = this.GetTextFormValue(value2, 1);
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 2);
                    this.nowChanging = true;
                    float value3 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
                    if (this.inputPosY)
                        this.inputPosY.text = this.GetTextFormValue(value3, 1);
                    this.nowChanging = false;
                    break;
                case 3:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, true, 2);
                    this.nowChanging = true;
                    float value4 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
                    if (this.inputPosY)
                        this.inputPosY.text = this.GetTextFormValue(value4, 1);
                    this.nowChanging = false;
                    break;
                case 4:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 4);
                    this.nowChanging = true;
                    float value5 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
                    if (this.inputPosZ)
                        this.inputPosZ.text = this.GetTextFormValue(value5, 1);
                    this.nowChanging = false;
                    break;
                case 5:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, true, 4);
                    this.nowChanging = true;
                    float value6 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
                    if (this.inputPosZ)
                        this.inputPosZ.text = this.GetTextFormValue(value6, 1);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addPos = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos;
        }

        public virtual void OnClickRot(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = this.moveRotValue[this.modeRot];
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
                    if (this.inputRotX)
                        this.inputRotX.text = this.GetTextFormValue(x);
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, true, 1);
                    this.nowChanging = true;
                    float x2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
                    if (this.inputRotX)
                        this.inputRotX.text = this.GetTextFormValue(x2);
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
                    if (this.inputRotY)
                        this.inputRotY.text = this.GetTextFormValue(y);
                    this.nowChanging = false;
                    break;
                case 3:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, true, 2);
                    this.nowChanging = true;
                    float y2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
                    if (this.inputRotY)
                        this.inputRotY.text = this.GetTextFormValue(y2);
                    this.nowChanging = false;
                    break;
                case 4:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
                    if (this.inputRotZ)
                        this.inputRotZ.text = this.GetTextFormValue(z);
                    this.nowChanging = false;
                    break;
                case 5:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, true, 4);
                    this.nowChanging = true;
                    float z2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
                    if (this.inputRotZ)
                        this.inputRotZ.text = this.GetTextFormValue(z2);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addRot = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot;
        }

        public virtual void OnClickScl(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            float num = this.moveSclValue[this.modeScl];
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
                    if (this.inputSclX)
                        this.inputSclX.text = this.GetTextFormValue(x, 2);
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, true, 1);
                    this.nowChanging = true;
                    float x2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
                    if (this.inputSclX)
                        this.inputSclX.text = this.GetTextFormValue(x2, 2);
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
                    if (this.inputSclY)
                        this.inputSclY.text = this.GetTextFormValue(y, 2);
                    this.nowChanging = false;
                    break;
                case 3:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, true, 2);
                    this.nowChanging = true;
                    float y2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
                    if (this.inputSclY)
                        this.inputSclY.text = this.GetTextFormValue(y2, 2);
                    this.nowChanging = false;
                    break;
                case 4:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, (float)(0.0 - num), true, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
                    if (this.inputSclZ)
                        this.inputSclZ.text = this.GetTextFormValue(z, 2);
                    this.nowChanging = false;
                    break;
                case 5:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, true, 4);
                    this.nowChanging = true;
                    float z2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
                    if (this.inputSclZ)
                        this.inputSclZ.text = this.GetTextFormValue(z2, 2);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addScl = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl;
        }

        public virtual void OnInputEndPos(int index)
        {
            if (this.nowChanging)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    float num = (float)((string.Empty != this.inputPosX.text) ? ((float)double.Parse(this.inputPosX.text) * 0.0099999997764825821) : 0.0);
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num, false, 1);
                    this.nowChanging = true;
                    float value = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
                    if (this.inputPosX)
                        this.inputPosX.text = this.GetTextFormValue(value, 1);
                    this.nowChanging = false;
                    break;
                case 1:
                    float num2 = (float)((string.Empty != this.inputPosY.text) ? ((float)double.Parse(this.inputPosY.text) * 0.0099999997764825821) : 0.0);
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num2, false, 2);
                    this.nowChanging = true;
                    float value2 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
                    if (this.inputPosY)
                        this.inputPosY.text = this.GetTextFormValue(value2, 1);
                    this.nowChanging = false;
                    break;
                case 2:
                    float num3 = (float)((string.Empty != this.inputPosZ.text) ? ((float)double.Parse(this.inputPosZ.text) * 0.0099999997764825821) : 0.0);
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, num3, false, 4);
                    this.nowChanging = true;
                    float value3 = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
                    if (this.inputPosZ)
                        this.inputPosZ.text = this.GetTextFormValue(value3, 1);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addPos = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos;
        }

        public virtual void OnInputEndRot(int index)
        {
            if (this.nowChanging)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    float num = (float)((string.Empty != this.inputRotX.text) ? ((float)double.Parse(this.inputRotX.text)) : 0.0);
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num, false, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
                    if (this.inputRotX)
                        this.inputRotX.text = this.GetTextFormValue(x);
                    this.nowChanging = false;
                    break;
                case 1:
                    float num2 = (float)((string.Empty != this.inputRotY.text) ? ((float)double.Parse(this.inputRotY.text)) : 0.0);
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num2, false, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
                    if (this.inputRotY)
                        this.inputRotY.text = this.GetTextFormValue(y);
                    this.nowChanging = false;
                    break;
                case 2:
                    float num3 = (float)((string.Empty != this.inputRotZ.text) ? ((float)double.Parse(this.inputRotZ.text)) : 0.0);
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, num3, false, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
                    if (this.inputRotZ)
                        this.inputRotZ.text = this.GetTextFormValue(z);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addRot = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot;
        }

        public virtual void OnInputEndScl(int index)
        {
            if (this.nowChanging)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    float num = (float)((string.Empty != this.inputSclX.text) ? ((float)double.Parse(this.inputSclX.text)) : 0.0);
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num, false, 1);
                    this.nowChanging = true;
                    float x = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
                    if (this.inputSclX)
                        this.inputSclX.text = this.GetTextFormValue(x, 2);
                    this.nowChanging = false;
                    break;
                case 1:
                    float num2 = (float)((string.Empty != this.inputSclY.text) ? ((float)double.Parse(this.inputSclY.text)) : 0.0);
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num2, false, 2);
                    this.nowChanging = true;
                    float y = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
                    if (this.inputSclY)
                        this.inputSclY.text = this.GetTextFormValue(y, 2);
                    this.nowChanging = false;
                    break;
                case 2:
                    float num3 = (float)((string.Empty != this.inputSclZ.text) ? ((float)double.Parse(this.inputSclZ.text)) : 0.0);
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, num3, false, 4);
                    this.nowChanging = true;
                    float z = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
                    if (this.inputSclZ)
                        this.inputSclZ.text = this.GetTextFormValue(z, 2);
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addScl = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl;
        }

        public virtual void OnClickResetPos(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, 0f, false, 1);
                    this.nowChanging = true;
                    if (this.inputPosX)
                        this.inputPosX.text = "0.0";
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, 0f, false, 2);
                    this.nowChanging = true;
                    if (this.inputPosY)
                        this.inputPosY.text = "0.0";
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryPos(slotNoFromSubMenuSelect, 0f, false, 4);
                    this.nowChanging = true;
                    if (this.inputPosZ)
                        this.inputPosZ.text = "0.0";
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addPos = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos;
        }

        public virtual void OnClickResetRot(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, 0f, false, 1);
                    this.nowChanging = true;
                    if (this.inputRotX)
                        this.inputRotX.text = "0";
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, 0f, false, 2);
                    this.nowChanging = true;
                    if (this.inputRotY)
                        this.inputRotY.text = "0";
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryRot(slotNoFromSubMenuSelect, 0f, false, 4);
                    this.nowChanging = true;
                    if (this.inputRotZ)
                        this.inputRotZ.text = "0";
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addRot = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot;
        }

        public virtual void OnClickResetScl(int index)
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            switch (index)
            {
                case 0:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, 1f, false, 1);
                    this.nowChanging = true;
                    if (this.inputSclX)
                        this.inputSclX.text = "1.00";
                    this.nowChanging = false;
                    break;
                case 1:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, 1f, false, 2);
                    this.nowChanging = true;
                    if (this.inputSclY)
                        this.inputSclY.text = "1.00";
                    this.nowChanging = false;
                    break;
                case 2:
                    this.CharClothes_SetAccessoryScl(slotNoFromSubMenuSelect, 1f, false, 4);
                    this.nowChanging = true;
                    if (this.inputSclZ)
                        this.inputSclZ.text = "1.00";
                    this.nowChanging = false;
                    break;
            }
            MoreAccessories.self.charaMakerAdditionalData.rawAccessoriesInfos[this.statusInfo.coordinateType][slotNoFromSubMenuSelect].addScl = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl;
        }

        public virtual void MoveInfoAllSet()
        {
            if (this.clothesInfo == null)
                return;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            this.nowChanging = true;
            float num = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.x * 100.0);
            if (this.inputPosX)
                this.inputPosX.text = this.GetTextFormValue(num, 1);
            num = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.y * 100.0);
            if (this.inputPosY)
                this.inputPosY.text = this.GetTextFormValue(num, 1);
            num = (float)(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addPos.z * 100.0);
            if (this.inputPosZ)
                this.inputPosZ.text = this.GetTextFormValue(num, 1);
            num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.x;
            if (this.inputRotX)
                this.inputRotX.text = this.GetTextFormValue(num);
            num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.y;
            if (this.inputRotY)
                this.inputRotY.text = this.GetTextFormValue(num);
            num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addRot.z;
            if (this.inputRotZ)
                this.inputRotZ.text = this.GetTextFormValue(num);
            num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.x;
            if (this.inputSclX)
                this.inputSclX.text = this.GetTextFormValue(num, 2);
            num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.y;
            if (this.inputSclY)
                this.inputSclY.text = this.GetTextFormValue(num, 2);
            num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].addScl.z;
            if (this.inputSclZ)
                this.inputSclZ.text = this.GetTextFormValue(num, 2);
            this.nowChanging = false;
        }

        public virtual void UpdateShowTab()
        {
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (this.tab02)
                this.tab02.gameObject.SetActive(false);
            if (this.tab03)
                this.tab03.gameObject.SetActive(false);
            if (this.tab04)
                this.tab04.gameObject.SetActive(false);
            this.tab05.gameObject.SetActive(false); //TODO enlever pour la copie
            bool flag = false;
            
            GameObject exists = MoreAccessories.self.charaMakerAdditionalData.objAccessory[slotNoFromSubMenuSelect];
            if (exists)
            {
                ListTypeFbxComponent component = MoreAccessories.self.charaMakerAdditionalData.objAccessory[slotNoFromSubMenuSelect].GetComponent<ListTypeFbxComponent>();
                if (component && "0" == component.ltfData.Parent)
                    flag = true;
            }
            else
                flag = true;
            if (this.tab02)
                this.tab02.gameObject.SetActive(!flag);
            if (this.tab03)
                this.tab03.gameObject.SetActive(!flag);
            this.lstTagColor.Clear();
            this.lstTagColor = CharBody_ChangeAccessory_Patches.CharInfo_GetTagInfo(MoreAccessories.self.charaMakerAdditionalData, slotNoFromSubMenuSelect);
            if (this.tab04)
                this.tab04.gameObject.SetActive((byte)((this.lstTagColor.Count != 0) ? 1 : 0) != 0);
            bool active = ColorChange.CheckChangeSubColor(this.lstTagColor);
            if (this.objSubColor)
                this.objSubColor.SetActive(active);
        }

        public override void SetCharaInfoSub()
        {
            if (!this.initEnd)
                this.Init();
            if (null == this.chaInfo)
                return;
            this.initFlags = true;
            if (null != this.tglTab)
                this.tglTab.isOn = true;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type + 1;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglType.Length; i++)
                this.tglType[i].isOn = false;
            this.nowTglAllSet = false;
            this.tglType[num].isOn = true;
            this.ChangeAccessoryTypeList(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id);
            this.UpdateShowTab();
            this.MoveInfoAllSet();
            this.initFlags = false;
        }

        public virtual void ChangeAccessoryTypeList(int newType, int newId)
        {
            this.acsType = newType;
            if (null == this.chaInfo || null == this.objListTop || null == this.objLineBase || null == this.rtfPanel)
                return;
            for (int num = this.objListTop.transform.childCount - 1; num >= 0; num--)
            {
                Transform child = this.objListTop.transform.GetChild(num);
                Destroy(child.gameObject);
            }
            Dictionary<int, ListTypeFbx> dictionary = null;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            if (newType != -1)
            {
                CharaListInfo.TypeAccessoryFbx typeAccessoryFbx = (CharaListInfo.TypeAccessoryFbx)(int)Enum.ToObject(typeof(CharaListInfo.TypeAccessoryFbx), newType);
                dictionary = this.chaInfo.ListInfo.GetAccessoryFbxList(typeAccessoryFbx);
            }
            int num2 = 0;
            int num3 = 0;
            if (dictionary != null)
            {
                Dictionary<int, ListTypeFbx>.Enumerator enumerator = dictionary.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        KeyValuePair<int, ListTypeFbx> current = enumerator.Current;
                        bool flag = false;
                        if (this.chaInfo.customInfo.isConcierge)
                            flag = CharaListInfo.CheckSitriClothesID(current.Value.Category, current.Value.Id);
                        if (CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id) != 0 || flag)
                        {
                            if (this.chaInfo.Sex == 0)
                            {
                                if ("0" != current.Value.PrefabM)
                                    goto IL_01ad;
                            }
                            else if ("0" != current.Value.PrefabF)
                                goto IL_01ad;
                        }
                        continue;
                        IL_01ad:
                        if (num2 == 0)
                            this.firstIndex = current.Key;
                        GameObject gameObject = Instantiate(this.objLineBase);
                        FbxTypeInfo fbxTypeInfo = gameObject.AddComponent<FbxTypeInfo>();
                        fbxTypeInfo.id = current.Key;
                        fbxTypeInfo.typeName = current.Value.Name;
                        fbxTypeInfo.info = current.Value;
                        gameObject.transform.SetParent(this.objListTop.transform, false);
                        RectTransform rectTransform = gameObject.transform as RectTransform;
                        rectTransform.localScale = new Vector3(1f, 1f, 1f);
                        rectTransform.anchoredPosition = new Vector3(0f, (float)(-24.0 * num2), 0f);
                        RectTransform obj = rectTransform;
                        Vector2 sizeDelta = rectTransform.sizeDelta;
                        obj.sizeDelta = new Vector2(0f, sizeDelta.y);
                        Text component = rectTransform.FindChild("Label").GetComponent<Text>();
                        component.text = fbxTypeInfo.typeName;
                        this.SetButtonClickHandler(gameObject);
                        Toggle component2 = gameObject.GetComponent<Toggle>();
                        if (newId == -1)
                        {
                            if (current.Key == this.firstIndex)
                                component2.isOn = true;
                        }
                        else if (current.Key == newId)
                            component2.isOn = true;
                        if (component2.isOn)
                            num3 = num2;
                        ToggleGroup component3 = this.objListTop.GetComponent<ToggleGroup>();
                        component2.group = component3;
                        gameObject.SetActive(true);
                        if (!flag)
                        {
                            int num4 = CharaListInfo.CheckCustomID(current.Value.Category, current.Value.Id);
                            Transform transform = rectTransform.FindChild("imgNew");
                            if (transform && num4 == 1)
                                transform.gameObject.SetActive(true);
                        }
                        num2++;
                    }
                }
                finally
                {
                    ((IDisposable)enumerator).Dispose();
                }
                RectTransform obj2 = this.rtfPanel;
                Vector2 sizeDelta2 = this.rtfPanel.sizeDelta;
                obj2.sizeDelta = new Vector2(sizeDelta2.x, (float)(24.0 * num2));
                float b = (float)(24.0 * num2 - 168.0);
                float y = Mathf.Min((float)(24.0 * num3), b);
                this.rtfPanel.anchoredPosition = new Vector2(0f, y);
                if (this.tab02)
                    this.tab02.gameObject.SetActive(true);
                if (this.tab03)
                    this.tab03.gameObject.SetActive(true);
            }
            else
            {
                RectTransform obj3 = this.rtfPanel;
                Vector2 sizeDelta3 = this.rtfPanel.sizeDelta;
                obj3.sizeDelta = new Vector2(sizeDelta3.x, 0f);
                this.rtfPanel.anchoredPosition = new Vector2(0f, 0f);
                if (this.tab02)
                    this.tab02.gameObject.SetActive(false);
                if (this.tab03)
                    this.tab03.gameObject.SetActive(false);
                if (this.tab04)
                    this.tab04.gameObject.SetActive(false);
            }
            this.nowChanging = true;
            if (this.clothesInfo != null)
            {
                float specularIntensity = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularIntensity;
                float specularSharpness = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color.specularSharpness;
                float specularSharpness2 = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].color2.specularSharpness;
                if (this.sldIntensity)
                    this.sldIntensity.value = specularIntensity;
                if (this.inputIntensity)
                    this.inputIntensity.text = this.ChangeTextFromFloat(specularIntensity);
                if (this.sldSharpness[0])
                    this.sldSharpness[0].value = specularSharpness;
                if (this.inputSharpness[0])
                    this.inputSharpness[0].text = this.ChangeTextFromFloat(specularSharpness);
                if (this.sldSharpness[1])
                    this.sldSharpness[1].value = specularSharpness2;
                if (this.inputSharpness[1])
                    this.inputSharpness[1].text = this.ChangeTextFromFloat(specularSharpness2);
            }
            this.nowChanging = false;
            this.OnClickColorSpecular(1);
            this.OnClickColorSpecular(0);
            this.OnClickColorDiffuse(1);
            this.OnClickColorDiffuse(0);
        }

        public override void UpdateCharaInfoSub()
        {
            if (null == this.chaInfo)
                return;
            this.initFlags = true;
            if (null != this.tab05)
                this.tab05.isOn = true;
            int slotNoFromSubMenuSelect = this.GetSlotNoFromSubMenuSelect();
            int num = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type + 1;
            this.nowTglAllSet = true;
            for (int i = 0; i < this.tglType.Length; i++)
                this.tglType[i].isOn = false;
            this.nowTglAllSet = false;
            this.tglType[num].isOn = true;
            this.ChangeAccessoryTypeList(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].type, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNoFromSubMenuSelect].id);
            this.UpdateShowTab();
            this.MoveInfoAllSet();
            this.initFlags = false;
        }

        public virtual void SetButtonClickHandler(GameObject clickObj)
        {
            ButtonPlaySE buttonPlaySE = clickObj.AddComponent<ButtonPlaySE>();
            if (buttonPlaySE)
            {
                buttonPlaySE._Type = ButtonPlaySE.Type.Click;
                buttonPlaySE._SE = ButtonPlaySE.SE.sel;
            }
            Toggle component = clickObj.GetComponent<Toggle>();
            component.onValueChanged.AddListener(delegate(bool value)
            {
                this.OnChangeIndex(clickObj, value);
            });
        }

        public virtual void SetChangeValueSharpnessHandler(Slider sld, int no)
        {
            if (!(null == sld))
            {
                sld.onValueChanged.AddListener(delegate(float value)
                {
                    this.OnValueChangeSharpness(no, value);
                });
            }
        }

        public virtual void SetInputEndSharpnessHandler(InputField inp, int no)
        {
            if (!(null == inp))
            {
                inp.onEndEdit.AddListener(delegate(string value)
                {
                    this.OnInputEndSharpness(no, value);
                });
            }
        }
        #endregion

        #region Private Methods
        private bool CharClothes_ResetAccessoryMove(int slotNo, int type = 7)
        {
            bool flag = true;
            if ((type & 1) != 0)
                flag &= this.CharClothes_SetAccessoryPos(slotNo, 0.0f, false, 7);
            if ((type & 2) != 0)
                flag &= this.CharClothes_SetAccessoryRot(slotNo, 0.0f, false, 7);
            if ((type & 4) != 0)
                flag &= this.CharClothes_SetAccessoryScl(slotNo, 1f, false, 7);
            return flag;
        }

        public bool CharClothes_SetAccessoryPos(int slotNo, float value, bool _add, int flags = 7)
        {
            GameObject gameObject = MoreAccessories.self.charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            if ((flags & 1) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.x) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.x = Mathf.Clamp(num, -1f, 1f);
            }
            if ((flags & 2) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.y) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.y = Mathf.Clamp(num, -1f, 1f);
            }
            if ((flags & 4) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.z) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.z = Mathf.Clamp(num, -1f, 1f);
            }
            gameObject.transform.SetLocalPosition(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.x, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.y, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addPos.z);
            return true;
        }

        private bool CharClothes_SetAccessoryRot(int slotNo, float value, bool _add, int flags = 7)
        {
            GameObject gameObject = MoreAccessories.self.charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            if ((flags & 1) != 0)
            {
                float t = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.x) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.x = Mathf.Repeat(t, 360f);
            }
            if ((flags & 2) != 0)
            {
                float t = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.y) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.y = Mathf.Repeat(t, 360f);
            }
            if ((flags & 4) != 0)
            {
                float t = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.z) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.z = Mathf.Repeat(t, 360f);
            }
            gameObject.transform.SetLocalRotation(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.x, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.y, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addRot.z);
            return true;
        }

        private bool CharClothes_SetAccessoryScl(int slotNo, float value, bool _add, int flags = 7)
        {
            GameObject gameObject = MoreAccessories.self.charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            if ((flags & 1) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.x) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.x = Mathf.Clamp(num, 0.01f, 100f);
            }
            if ((flags & 2) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.y) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.y = Mathf.Clamp(num, 0.01f, 100f);
            }
            if ((flags & 4) != 0)
            {
                float num = (!_add ? 0.0f : MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.z) + value;
                MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.z = Mathf.Clamp(num, 0.01f, 100f);
            }
            gameObject.transform.SetLocalScale(MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.x, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.y, MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo].addScl.z);
            return true;
        }

        private bool CharClothes_UpdateAccessoryMoveFromInfo(int slotNo)
        {
            GameObject gameObject = MoreAccessories.self.charaMakerAdditionalData.objAcsMove[slotNo];
            if (null == gameObject)
                return false;
            CharFileInfoClothes.Accessory accessory = MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory[slotNo];
            gameObject.transform.SetLocalPosition(accessory.addPos.x, accessory.addPos.y, accessory.addPos.z);
            gameObject.transform.SetLocalRotation(accessory.addRot.x, accessory.addRot.y, accessory.addRot.z);
            gameObject.transform.SetLocalScale(accessory.addScl.x, accessory.addScl.y, accessory.addScl.z);
            return true;
        }
        #endregion
    }
}