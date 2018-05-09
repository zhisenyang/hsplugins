using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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

        private delegate bool TranslationDelegate(ref string text);
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
        private RectTransform _addButtons;
        private RoutinesComponent _routines;
        private Studio.OCIChar _selectedStudioCharacter;
        private readonly List<StudioSlotData> _displayedStudioSlots = new List<StudioSlotData>();
        private StudioSlotData _toggleAll;
        private TranslationDelegate _translationMethod;
        private readonly string[] _femaleMoreAttachPointsPaths = 
        {
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02/cf_J_sk_00_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02/cf_J_sk_00_03/cf_J_sk_00_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_00_00_dam/cf_J_sk_00_00/cf_J_sk_00_01/cf_J_sk_00_02/cf_J_sk_00_03/cf_J_sk_00_04/cf_J_sk_00_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02/cf_J_sk_01_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02/cf_J_sk_01_03/cf_J_sk_01_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_01_00_dam/cf_J_sk_01_00/cf_J_sk_01_01/cf_J_sk_01_02/cf_J_sk_01_03/cf_J_sk_01_04/cf_J_sk_01_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02/cf_J_sk_02_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02/cf_J_sk_02_03/cf_J_sk_02_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_02_00_dam/cf_J_sk_02_00/cf_J_sk_02_01/cf_J_sk_02_02/cf_J_sk_02_03/cf_J_sk_02_04/cf_J_sk_02_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02/cf_J_sk_06_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02/cf_J_sk_06_03/cf_J_sk_06_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_06_00_dam/cf_J_sk_06_00/cf_J_sk_06_01/cf_J_sk_06_02/cf_J_sk_06_03/cf_J_sk_06_04/cf_J_sk_06_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02/cf_J_sk_07_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02/cf_J_sk_07_03/cf_J_sk_07_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_07_00_dam/cf_J_sk_07_00/cf_J_sk_07_01/cf_J_sk_07_02/cf_J_sk_07_03/cf_J_sk_07_04/cf_J_sk_07_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02/cf_J_sk_03_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02/cf_J_sk_03_03/cf_J_sk_03_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_03_00_dam/cf_J_sk_03_00/cf_J_sk_03_01/cf_J_sk_03_02/cf_J_sk_03_03/cf_J_sk_03_04/cf_J_sk_03_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02/cf_J_sk_04_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02/cf_J_sk_04_03/cf_J_sk_04_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_04_00_dam/cf_J_sk_04_00/cf_J_sk_04_01/cf_J_sk_04_02/cf_J_sk_04_03/cf_J_sk_04_04/cf_J_sk_04_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02/cf_J_sk_05_03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02/cf_J_sk_05_03/cf_J_sk_05_04",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_sk_top/cf_J_sk_siri_dam/cf_J_sk_05_00_dam/cf_J_sk_05_00/cf_J_sk_05_01/cf_J_sk_05_02/cf_J_sk_05_03/cf_J_sk_05_04/cf_J_sk_05_05",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s/cf_J_Ana",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s/cf_J_Kokan",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s/cf_J_Kosi03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegKnee_dam_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLow03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L/cf_J_Foot01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L/cf_J_Foot01_L/cf_J_Foot02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegLow01_L/cf_J_LegLowRoll_L/cf_J_Foot01_L/cf_J_Foot02_L/cf_J_Toes01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_L/cf_J_LegUp03_L/cf_J_LegUp03_s_L/cf_J_LegKnee_back_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegKnee_dam_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLow03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R/cf_J_Foot01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R/cf_J_Foot01_R/cf_J_Foot02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegLow01_R/cf_J_LegLowRoll_R/cf_J_Foot01_R/cf_J_Foot02_R/cf_J_Toes01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_LegUp00_R/cf_J_LegUp03_R/cf_J_LegUp03_s_R/cf_J_LegKnee_back_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L/cf_J_SiriDam01_L/cf_J_Siri_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R/cf_J_SiriDam01_R/cf_J_Siri_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_LegUpDam_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_LegUpDam_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L/cf_J_Mune02_L/cf_J_Mune02_s_L/cf_J_Mune02_t_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L/cf_J_Mune02_L/cf_J_Mune02_s_L/cf_J_Mune02_t_L/cf_J_Mune03_L/cf_J_Mune03_s_L/cf_J_Mune04_s_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_L/cf_J_Mune00_L/cf_J_Mune00_s_L/cf_J_Mune00_d_L/cf_J_Mune01_L/cf_J_Mune01_s_L/cf_J_Mune01_t_L/cf_J_Mune02_L/cf_J_Mune02_s_L/cf_J_Mune02_t_L/cf_J_Mune03_L/cf_J_Mune03_s_L/cf_J_Mune04_s_L/cf_J_Mune_Nip01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R/cf_J_Mune02_R/cf_J_Mune02_s_R/cf_J_Mune02_t_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R/cf_J_Mune02_R/cf_J_Mune02_s_R/cf_J_Mune02_t_R/cf_J_Mune03_R/cf_J_Mune03_s_R/cf_J_Mune04_s_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Mune00/cf_J_Mune00_t_R/cf_J_Mune00_R/cf_J_Mune00_s_R/cf_J_Mune00_d_R/cf_J_Mune01_R/cf_J_Mune01_s_R/cf_J_Mune01_t_R/cf_J_Mune02_R/cf_J_Mune02_s_R/cf_J_Mune02_t_R/cf_J_Mune03_R/cf_J_Mune03_s_R/cf_J_Mune04_s_R/cf_J_Mune_Nip01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekLow_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekLow_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekUp_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_CheekUp_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_Chin_rs",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_Chin_rs/cf_J_ChinTip_s",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_FaceLow_s/cf_J_ChinLow",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_Mouth_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_Mouth_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_MouthLow",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthBase_s/cf_J_MouthMove/cf_J_Mouthup",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLowBase/cf_J_MouthBase_tr/cf_J_MouthCavity",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_Eye_r_L/cf_J_Eye01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_Eye_r_L/cf_J_Eye02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_Eye_r_L/cf_J_Eye03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_Eye_r_L/cf_J_Eye04_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_EyePos_rz_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_L/cf_J_Eye_s_L/cf_J_EyePos_rz_L/cf_J_look_L/cf_J_eye_rs_L/cf_J_pupil_s_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_Eye_r_R/cf_J_Eye01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_Eye_r_R/cf_J_Eye02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_Eye_r_R/cf_J_Eye03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_Eye_r_R/cf_J_Eye04_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_EyePos_rz_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Eye_t_R/cf_J_Eye_s_R/cf_J_EyePos_rz_R/cf_J_look_R/cf_J_eye_rs_R/cf_J_pupil_s_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_L/cf_J_MayuMid_s_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_L/cf_J_MayuTip_s_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_R/cf_J_MayuMid_s_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_Neck/cf_J_Head/cf_J_Head_s/p_cf_head_bone/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceUp_ty/cf_J_FaceUp_tz/cf_J_Mayu_R/cf_J_MayuTip_s_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmElbo_dam_01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_ArmLow02_dam_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Index01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Index01_L/cf_J_Hand_Index02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Index01_L/cf_J_Hand_Index02_L/cf_J_Hand_Index03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Little01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Little01_L/cf_J_Hand_Little02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Little01_L/cf_J_Hand_Little02_L/cf_J_Hand_Little03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Middle01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Middle01_L/cf_J_Hand_Middle02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Middle01_L/cf_J_Hand_Middle02_L/cf_J_Hand_Middle03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Ring01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Ring01_L/cf_J_Hand_Ring02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Ring01_L/cf_J_Hand_Ring02_L/cf_J_Hand_Ring03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Thumb01_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Thumb01_L/cf_J_Hand_Thumb02_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmLow01_L/cf_J_Hand_L/cf_J_Hand_s_L/cf_J_Hand_Thumb01_L/cf_J_Hand_Thumb02_L/cf_J_Hand_Thumb03_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmUp01_dam_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_L/cf_J_Shoulder_L/cf_J_ArmUp00_L/cf_J_ArmUp03_dam_L",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmElbo_dam_01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_ArmLow02_dam_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Index01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Index01_R/cf_J_Hand_Index02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Index01_R/cf_J_Hand_Index02_R/cf_J_Hand_Index03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Little01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Little01_R/cf_J_Hand_Little02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Little01_R/cf_J_Hand_Little02_R/cf_J_Hand_Little03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Middle01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Middle01_R/cf_J_Hand_Middle02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Middle01_R/cf_J_Hand_Middle02_R/cf_J_Hand_Middle03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Ring01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Ring01_R/cf_J_Hand_Ring02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Ring01_R/cf_J_Hand_Ring02_R/cf_J_Hand_Ring03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Thumb01_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Thumb01_R/cf_J_Hand_Thumb02_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmLow01_R/cf_J_Hand_R/cf_J_Hand_s_R/cf_J_Hand_Thumb01_R/cf_J_Hand_Thumb02_R/cf_J_Hand_Thumb03_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmUp01_dam_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_ShoulderIK_R/cf_J_Shoulder_R/cf_J_ArmUp00_R/cf_J_ArmUp03_dam_R",
            "BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Spine01/cf_J_Spine02/cf_J_Spine03/cf_J_SpineSk00_dam"
        };
        private readonly string[] _maleMoreAttachPointsPaths =
        {
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan100_00/cm_J_dan101_00/cm_J_dan109_00",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan_f_top",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan_f_top/cm_J_dan_f_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kokan/cm_J_dan_s/cm_J_dan_top/cm_J_dan_f_top/cm_J_dan_f_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kosi02_s/cm_J_Ana",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegKnee_dam_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_Foot01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_Foot01_L/cm_J_Foot02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_Foot01_L/cm_J_Foot02_L/cm_J_Toes01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_LegLow02_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegLow01_L/cm_J_LegLow03_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_L/cm_J_LegUp03_L/cm_J_LegUp03_s_L/cm_J_LegKnee_back_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegKnee_dam_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_Foot01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_Foot01_R/cm_J_Foot02_R",
                "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_Foot01_R/cm_J_Foot02_R/cm_J_Toes01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_LegLow02_s_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegLow01_R/cm_J_LegLow03_s_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_LegUp00_R/cm_J_LegUp03_R/cm_J_LegUp03_s_R/cm_J_LegKnee_back_s_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_LegUpDam_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_LegUpDam_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03",
                "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/cm_J_Tang_S_00/cm_J_Tang_S_01_at",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/cm_J_Tang_S_00/cm_J_Tang_S_01_at/cm_J_Tang_S_02_at",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/cm_J_Tang_S_00/cm_J_Tang_S_01_at/cm_J_Tang_S_02_at/cm_J_Tang_S_04",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/cm_J_Tang_S_00/cm_J_Tang_S_01_at/cm_J_Tang_S_02_at/cm_J_Tang_S_04/cm_J_Tang_S_06",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/cm_J_Tang_S_00/cm_J_Tang_S_01_at/cm_J_Tang_S_02_at/cm_J_Tang_S_04/cm_J_Tang_S_06/cm_J_Tang_S_08",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekLow_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekLow_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekUp_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_CheekUp_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_Chin_rs",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_Chin_rs/cm_J_ChinTip_s",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_FaceLow_s/cm_J_ChinLow",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_Mouth_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_Mouth_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_MouthLow",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthBase_s/cm_J_MouthMove/cm_J_Mouthup",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceLowBase/cm_J_MouthBase_tr/cm_J_MouthCavity",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L/cm_J_Eye_s_L/cm_J_Eye_r_L/cm_J_Eye01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L/cm_J_Eye_s_L/cm_J_Eye_r_L/cm_J_Eye02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L/cm_J_Eye_s_L/cm_J_Eye_r_L/cm_J_Eye03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L/cm_J_Eye_s_L/cm_J_Eye_r_L/cm_J_Eye04_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_L/cm_J_Eye_s_L/cm_J_EyePos_rz_L/cm_J_look_L/cm_J_eye_rs_L/cm_J_pupil_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R/cm_J_Eye_s_R/cm_J_Eye_r_R/cm_J_Eye01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R/cm_J_Eye_s_R/cm_J_Eye_r_R/cm_J_Eye02_R",
                "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R/cm_J_Eye_s_R/cm_J_Eye_r_R/cm_J_Eye03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R/cm_J_Eye_s_R/cm_J_Eye_r_R/cm_J_Eye04_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Eye_t_R/cm_J_Eye_s_R/cm_J_EyePos_rz_R/cm_J_look_R/cm_J_eye_rs_R/cm_J_pupil_s_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_C",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_L/cm_J_MayuMid_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_L/cm_J_MayuTip_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_R/cm_J_MayuMid_s_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_Neck/cm_J_Head/cm_J_Head_s/p_cm_head_bone/cm_J_FaceRoot/cm_J_FaceBase/cm_J_FaceUp_ty/cm_J_FaceUp_tz/cm_J_Mayu_R/cm_J_MayuTip_s_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmElbo_dam_02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_ArmLow02_dam_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Index01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Index01_L/cm_J_Hand_Index02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Index01_L/cm_J_Hand_Index02_L/cm_J_Hand_Index03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Little01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Little01_L/cm_J_Hand_Little02_L",
                "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Little01_L/cm_J_Hand_Little02_L/cm_J_Hand_Little03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Middle01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Middle01_L/cm_J_Hand_Middle02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Middle01_L/cm_J_Hand_Middle02_L/cm_J_Hand_Middle03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Ring01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Ring01_L/cm_J_Hand_Ring02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Ring01_L/cm_J_Hand_Ring02_L/cm_J_Hand_Ring03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Thumb01_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Thumb01_L/cm_J_Hand_Thumb02_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmLow01_L/cm_J_Hand_L/cm_J_Hand_s_L/cm_J_Hand_Thumb01_L/cm_J_Hand_Thumb02_L/cm_J_Hand_Thumb03_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmUp02_dam_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_ArmUp00_L/cm_J_ArmUp03_dam_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_L/cm_J_Shoulder_L/cm_J_Shoulder02_s_L",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmElbo_dam_02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_ArmLow02_dam_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Index01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Index01_R/cm_J_Hand_Index02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Index01_R/cm_J_Hand_Index02_R/cm_J_Hand_Index03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Little01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Little01_R/cm_J_Hand_Little02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Little01_R/cm_J_Hand_Little02_R/cm_J_Hand_Little03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Middle01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Middle01_R/cm_J_Hand_Middle02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Middle01_R/cm_J_Hand_Middle02_R/cm_J_Hand_Middle03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Ring01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Ring01_R/cm_J_Hand_Ring02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Ring01_R/cm_J_Hand_Ring02_R/cm_J_Hand_Ring03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Thumb01_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Thumb01_R/cm_J_Hand_Thumb02_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmLow01_R/cm_J_Hand_R/cm_J_Hand_s_R/cm_J_Hand_Thumb01_R/cm_J_Hand_Thumb02_R/cm_J_Hand_Thumb03_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmUp02_dam_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_ArmUp00_R/cm_J_ArmUp03_dam_R",
            "BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Spine01/cm_J_Spine02/cm_J_Spine03/cm_J_ShoulderIK_R/cm_J_Shoulder_R/cm_J_Shoulder02_s_R"
        };
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
        public string[] femaleMoreAttachPointsPaths { get { return this._femaleMoreAttachPointsPaths; } }
        public string[] maleMoreAttachPointsPaths { get { return this._maleMoreAttachPointsPaths; } }
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

            HSExtSave.HSExtSave.RegisterHandler("moreAccessories", this.OnCharaLoad, this.OnCharaSave, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);

            UIUtility.Init();

            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.moreaccessories");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Type t = Type.GetType("UnityEngine.UI.Translation.TextTranslator,UnityEngine.UI.Translation");
            if (t != null)
            {
                MethodInfo info = t.GetMethod("Translate", BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    this._translationMethod = (TranslationDelegate)Delegate.CreateDelegate(typeof(TranslationDelegate), info);
                }
            }
        }

        public void OnLevelWasLoaded(int level)
        {
            this._routines = new GameObject("Routines", typeof(RoutinesComponent)).GetComponent<RoutinesComponent>();
            this._level = level;
            if (this._binary == Binary.Game)
            {
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

                    this._addButtons = UIUtility.CreateNewUIObject(this._prefab.parent, "AddAccessories");
                    this._addButtons.SetRect(this._prefab.anchorMin, this._prefab.anchorMax, this._prefab.offsetMin + new Vector2(0f, -this._prefab.rect.height * 1.2f), this._prefab.offsetMax + new Vector2(0f, -this._prefab.rect.height));
                    this._addButtons.pivot = new Vector2(0.5f, 1f);
                    this._addButtons.gameObject.AddComponent<UI_TreeView>();

                    Button addButton = UIUtility.CreateButton("AddAccessoriesButton", this._addButtons, "+ Add accessory");
                    addButton.transform.SetRect(Vector2.zero, new Vector2(0.70f, 1f), Vector2.zero, Vector2.zero);
                    addButton.onClick.AddListener(this.AddSlot);
                    addButton.colors = template.colors;
                    ((Image)addButton.targetGraphic).sprite = ((Image)template.targetGraphic).sprite;
                    Text text = addButton.GetComponentInChildren<Text>();
                    text.resizeTextForBestFit = true;
                    text.resizeTextMaxSize = 200;
                    text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));

                    Button addTenButton = UIUtility.CreateButton("AddAccessoriesButton", this._addButtons, "Add 10");
                    addTenButton.transform.SetRect(new Vector2(0.70f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
                    addTenButton.onClick.AddListener(this.AddTenSlots);
                    addTenButton.colors = template.colors;
                    ((Image)addTenButton.targetGraphic).sprite = ((Image)template.targetGraphic).sprite;
                    text = addTenButton.GetComponentInChildren<Text>();
                    text.resizeTextForBestFit = true;
                    text.resizeTextMaxSize = 200;
                    text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));
                }
            }
            else
            {
                if (level == 3)
                {
                    Transform accList = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Slot");
                    this._prefab = accList.Find("Slot10") as RectTransform;

                    MPCharCtrl ctrl = ((MPCharCtrl)Studio.Studio.Instance.rootButtonCtrl.GetPrivate("manipulate").GetPrivate("m_ManipulatePanelCtrl").GetPrivate("charaPanelInfo").GetPrivate("m_MPCharCtrl"));

                    this._toggleAll = new StudioSlotData();
                    this._toggleAll.slot = (RectTransform)GameObject.Instantiate(this._prefab.gameObject).transform;
                    this._toggleAll.name = this._toggleAll.slot.GetComponentInChildren<Text>();
                    this._toggleAll.onButton = this._toggleAll.slot.GetChild(1).GetComponent<Button>();
                    this._toggleAll.offButton = this._toggleAll.slot.GetChild(2).GetComponent<Button>();
                    this._toggleAll.name.text = "All";
                    this._toggleAll.slot.SetParent(this._prefab.parent);
                    this._toggleAll.slot.localPosition = Vector3.zero;
                    this._toggleAll.slot.localScale = Vector3.one;
                    this._toggleAll.onButton.onClick = new Button.ButtonClickedEvent();
                    this._toggleAll.onButton.onClick.AddListener(() =>
                    {
                        this._selectedStudioCharacter.charInfo.chaClothes.SetAccessoryStateAll(true);
                        ctrl.UpdateInfo();
                        this.UpdateStudioUI();
                    });
                    this._toggleAll.offButton.onClick = new Button.ButtonClickedEvent();
                    this._toggleAll.offButton.onClick.AddListener(() =>
                    {
                        this._selectedStudioCharacter.charInfo.chaClothes.SetAccessoryStateAll(false);
                        ctrl.UpdateInfo();
                        this.UpdateStudioUI();
                    });
                    this._toggleAll.slot.SetAsLastSibling();
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
            if (Input.GetKeyDown(KeyCode.A))
            {StringBuilder sb = new StringBuilder();
                List<GameObject> values = ((Dictionary<int, GameObject>)this._charaMakerCharInfo.GetPrivate("dictRefObj")).Values.ToList();
                this.Recurse(this._charaMakerCharInfo.chaBody.transform, (t) =>
                {
                    if (t != this._charaMakerCharInfo.chaBody.transform && t.gameObject.activeInHierarchy && values.Contains(t.gameObject) == false)
                    {
                        sb.AppendLine(t.GetPathFrom(this._charaMakerCharInfo.chaBody.transform));
                    }
                });
                UnityEngine.Debug.Log(sb.ToString());
            }
        }

        private void Recurse(Transform t, Action<Transform> action)
        {
            action(t);
            for (int i = 0; i < t.childCount; i++)
            {
                this.Recurse(t.GetChild(i), action);
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
            this._addButtons.SetAsLastSibling();
            this._prefab.parent.GetComponent<UI_TreeView>().UpdateView();
            this._smMoreAccessories.UpdateUI();
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
                slot.onButton.interactable = accessory != null && accessory.type != -1;
                slot.onButton.image.color = slot.onButton.interactable && additionalData.showAccessory[i] ? Color.green : Color.white;
                slot.offButton.interactable = accessory != null && accessory.type != -1;
                slot.offButton.image.color = slot.onButton.interactable && !additionalData.showAccessory[i] ? Color.green : Color.white;
            }
            for (; i < this._displayedStudioSlots.Count; ++i)
                this._displayedStudioSlots[i].slot.gameObject.SetActive(false);
            this._toggleAll.slot.SetAsLastSibling();
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
                    Dictionary<int, ListTypeFbx> accessoryFbxList = this._charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type);
                    ListTypeFbx listTypeFbx = null;
                    str2 = accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx) ? listTypeFbx.Name : "None";
                }
            }
            else if (accessory.type == -1)
            {
                str2 = "None";
            }
            else
            {
                Dictionary<int, ListTypeFbx> accessoryFbxList = this._charaMakerCharInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx)accessory.type);
                ListTypeFbx listTypeFbx = null;
                str2 = accessoryFbxList.TryGetValue(accessory.id, out listTypeFbx) ? listTypeFbx.Name : "None";
            }
            if (this._translationMethod != null)
                this._translationMethod(ref str2);
            if (addNo)
                str2 = (slotNo + 11).ToString("00") + " " + str2;
            if (addType)
                str2 = CharDefine.AccessoryTypeName[accessory.type + 1] + ":" + str2;
            str1 = str2.Substring(0, Mathf.Min(limit, str2.Length));
            return str1;
        }

        internal void DuplicateCharacter(CharInfo source, CharInfo destination)
        {
            CharAdditionalData sourceAdditionalData;
            if (this._accessoriesByChar.TryGetValue(source.chaFile, out sourceAdditionalData) == false)
            {
                return;
            }
            CharAdditionalData destinationAdditionalData;
            if (this._accessoriesByChar.TryGetValue(destination.chaFile, out destinationAdditionalData) == false)
            {
                destinationAdditionalData = new CharAdditionalData();
                this._accessoriesByChar.Add(destination.chaFile, destinationAdditionalData);
            }

            foreach (KeyValuePair<CharDefine.CoordinateType, List<CharFileInfoClothes.Accessory>> accessories in sourceAdditionalData.rawAccessoriesInfos)
            {
                List<CharFileInfoClothes.Accessory> accessories2;
                if (destinationAdditionalData.rawAccessoriesInfos.TryGetValue(accessories.Key, out accessories2))
                    accessories2.Clear();
                else
                {
                    accessories2 = new List<CharFileInfoClothes.Accessory>();
                    destinationAdditionalData.rawAccessoriesInfos.Add(accessories.Key, accessories2);
                }

                foreach (CharFileInfoClothes.Accessory accessory in accessories.Value)
                {
                    CharFileInfoClothes.Accessory newAccessory = new CharFileInfoClothes.Accessory();
                    newAccessory.Copy(accessory);
                    accessories2.Add(newAccessory);
                }
            }
            destinationAdditionalData.showAccessory.AddRange(sourceAdditionalData.showAccessory);
            while (destinationAdditionalData.infoAccessory.Count < destinationAdditionalData.clothesInfoAccessory.Count)
                destinationAdditionalData.infoAccessory.Add(null);
            while (destinationAdditionalData.objAccessory.Count < destinationAdditionalData.clothesInfoAccessory.Count)
                destinationAdditionalData.objAccessory.Add(null);
            while (destinationAdditionalData.objAcsMove.Count < destinationAdditionalData.clothesInfoAccessory.Count)
                destinationAdditionalData.objAcsMove.Add(null);
            CharBody_ChangeAccessory_Patches.Postfix(destination.chaBody, true);

            this.UpdateStudioUI();
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

        private void AddTenSlots()
        {
            if (this._binary != Binary.Game || this._level != 21 || this._ready == false || this._charaMakerCharInfo == null)
                return;
            CharAdditionalData additionalData = this._accessoriesByChar[this._charaMakerCharInfo.chaFile];
            for (int i = 0; i < 10; i++)
                additionalData.clothesInfoAccessory.Add(new CharFileInfoClothes.Accessory());
            while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.infoAccessory.Add(null);
            while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAccessory.Add(null);
            while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.objAcsMove.Add(null);
            while (additionalData.showAccessory.Count < additionalData.clothesInfoAccessory.Count)
                additionalData.showAccessory.Add(this._charaMakerCharInfo.statusInfo.showAccessory[0]);
            for (int i = 0; i < 10; i++)
            {
                int idx = additionalData.clothesInfoAccessory.Count - 10 + i;
                CharBody_ChangeAccessory_Patches.ChangeAccessoryAsync(this._charaMakerCharInfo.chaBody, additionalData, idx, -1, -1, "", true);
            }
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

        private void OnSceneImport(string path, XmlNode n)
        {
            if (n == null)
                return;
            XmlNode node = n.CloneNode(true);
            int max = -1;
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Key > max)
                    max = pair.Key;
            }
            this._routines.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).Where(p => p.Key > max).ToList();

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
