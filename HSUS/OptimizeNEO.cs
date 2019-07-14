using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Harmony;
using Manager;
using Studio;
using ToolBox;
using UILib;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace StudioFileCheck
{
    [HarmonyPatch(typeof(ItemList), "Awake")]
    public class ItemList_Awake_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        public static void Prefix(ItemList __instance)
        {
            ItemList_InitList_Patches.ItemListData data;
            if (ItemList_InitList_Patches._dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new ItemList_InitList_Patches.ItemListData();
                ItemList_InitList_Patches._dataByInstance.Add(__instance, data);
            }

            Transform transformRoot = (Transform)__instance.GetPrivate("transformRoot");

            RectTransform rt = transformRoot.parent as RectTransform;
            rt.offsetMin += new Vector2(0f, 18f);
            float newY = rt.offsetMin.y;

            data.searchBar = UIUtility.CreateInputField("Search Bar", transformRoot.parent.parent, "Search...");
            Image image = data.searchBar.GetComponent<Image>();
            image.color = UIUtility.grayColor;
            //image.sprite = null;
            rt = data.searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
#if HONEYSELECT
            rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(HSUS.HSUS._self._improveNeoUI ? 10f : 7f, 16f), new Vector2(HSUS.HSUS._self._improveNeoUI ? -22f : -14f, newY));
#elif KOIKATSU
            rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 20f), new Vector2(-18f, newY));
#endif
            data.searchBar.onValueChanged.AddListener(s => SearchChanged(__instance));
            foreach (Text t in data.searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
        }

        public static void ResetSearch(ItemList instance)
        {
            ItemList_InitList_Patches.ItemListData data;
            if (ItemList_InitList_Patches._dataByInstance.TryGetValue(instance, out data) == false)
            {
                data = new ItemList_InitList_Patches.ItemListData();
                ItemList_InitList_Patches._dataByInstance.Add(instance, data);
            }
            if (data.searchBar != null)
            {
                data.searchBar.text = "";
                SearchChanged(instance);
            }
        }

        private static void SearchChanged(ItemList instance)
        {
            ItemList_InitList_Patches.ItemListData data;
            if (ItemList_InitList_Patches._dataByInstance.TryGetValue(instance, out data) == false)
            {
                data = new ItemList_InitList_Patches.ItemListData();
                ItemList_InitList_Patches._dataByInstance.Add(instance, data);
            }
            string search = data.searchBar.text.Trim();
#if HONEYSELECT
            int currentGroup = (int)instance.GetPrivate("group");
#elif KOIKATSU
            int currentGroup = (int)instance.GetPrivate("category");
#endif
            List<StudioNode> list;
            if (data.objects.TryGetValue(currentGroup, out list) == false)
                return;
            foreach (StudioNode objectData in list)
            {
                objectData.active = objectData.textUI.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1;
            }
        }
    }

    [HarmonyPatch(typeof(ItemList), "InitList", new[]
    {
        typeof(int),
#if KOIKATSU
        typeof(int),
#endif        
    })]
    public class ItemList_InitList_Patches
    {
        public class ItemListData
        {
            public readonly Dictionary<int, List<StudioNode>> objects = new Dictionary<int, List<StudioNode>>();
            public InputField searchBar;
        }

        public static readonly Dictionary<ItemList, ItemListData> _dataByInstance = new Dictionary<ItemList, ItemListData>();

#if KOIKATSU
        private static int _lastCategory = -1;
#endif

        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

#if HONEYSELECT
        public static bool Prefix(ItemList __instance, int _group)
        {
            ItemListData data;
            if (_dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new ItemListData();
                _dataByInstance.Add(__instance, data);
            }

            ItemList_Awake_Patches.ResetSearch(__instance);

            int currentGroup = (int)__instance.GetPrivate("group");
            if (currentGroup == _group)
                return false;
            ((ScrollRect)__instance.GetPrivate("scrollRect")).verticalNormalizedPosition = 1f;

            List<StudioNode> list;
            if (data.objects.TryGetValue(currentGroup, out list))
                foreach (StudioNode studioNode in list)
                    studioNode.active = false;
            if (data.objects.TryGetValue(_group, out list))
                foreach (StudioNode studioNode in list)
                    studioNode.active = true;
            else
            {
                list = new List<StudioNode>();
                data.objects.Add(_group, list);

                Transform transformRoot = (Transform)__instance.GetPrivate("transformRoot");
                GameObject objectNode = (GameObject)__instance.GetPrivate("objectNode");
                foreach (KeyValuePair<int, Studio.Info.ItemLoadInfo> item in Singleton<Studio.Info>.Instance.dicItemLoadInfo)
                {
                    if (item.Value.@group != _group)
                        continue;
                    GameObject gameObject = Object.Instantiate(objectNode);
                    gameObject.transform.SetParent(transformRoot, false);
                    StudioNode component = gameObject.GetComponent<StudioNode>();
                    component.active = true;
                    int no = item.Key;
                    component.addOnClick = () => { Studio.Studio.Instance.AddItem(no); };
                    component.text = item.Value.name;
                    component.textColor = ((!(item.Value.isColor & item.Value.isColor2)) ? ((!(item.Value.isColor | item.Value.isColor2)) ? Color.white : Color.red) : Color.cyan);
                    if (item.Value.isColor || item.Value.isColor2)
                    {
                        Shadow shadow = (component.textUI).gameObject.AddComponent<Shadow>();
                        shadow.effectColor = Color.black;
                    }
                    list.Add(component);
                }
            }
            if (!__instance.gameObject.activeSelf)
                __instance.gameObject.SetActive(true);
            __instance.SetPrivate("group", _group);
            return false;
        }
#elif KOIKATSU
        public static bool Prefix(ItemList __instance, int _group, int _category)
        {
            if ((int)__instance.GetPrivate("group") == _group && (int)__instance.GetPrivate("category") == _category)
            {
                return false;
            }

            ItemListData data;
            if (_dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new ItemListData();
                _dataByInstance.Add(__instance, data);
            }


            ((ScrollRect)__instance.GetPrivate("scrollRect")).verticalNormalizedPosition = 1f;

            List<StudioNode> list;
            UnityEngine.Debug.LogError("last category " + _lastCategory + " now category " + _category);
            if (data.objects.TryGetValue(_lastCategory, out list))
                foreach (StudioNode studioNode in list)
                    studioNode.active = false;
            if (data.objects.TryGetValue(_category, out list))
                foreach (StudioNode studioNode in list)
                    studioNode.active = true;
            else
            {
                list = new List<StudioNode>();
                data.objects.Add(_category, list);

                foreach (KeyValuePair<int, Info.ItemLoadInfo> keyValuePair in Singleton<Info>.Instance.dicItemLoadInfo[_group][_category])
                {
                    GameObject gameObject = Object.Instantiate((GameObject)__instance.GetPrivate("objectNode"));
                    gameObject.transform.SetParent((Transform)__instance.GetPrivate("transformRoot"), false);
                    StudioNode component = gameObject.GetComponent<StudioNode>();
                    component.active = true;
                    int no = keyValuePair.Key;
                    component.addOnClick = delegate
                    {
                        __instance.CallPrivate("OnSelect", no);
                    };
                    component.text = keyValuePair.Value.name;
                    int num = keyValuePair.Value.color.Count((bool b) => b) + ((!keyValuePair.Value.isGlass) ? 0 : 1);
                    switch (num)
                    {
                        case 1:
                            component.textColor = Color.red;
                            break;
                        case 2:
                            component.textColor = Color.cyan;
                            break;
                        case 3:
                            component.textColor = Color.green;
                            break;
                        case 4:
                            component.textColor = Color.yellow;
                            break;
                        default:
                            component.textColor = Color.white;
                            break;
                    }
                    if (num != 0 && component.textUI)
                    {
                        Shadow shadow = component.textUI.gameObject.AddComponent<Shadow>();
                        shadow.effectColor = Color.black;
                    }
                    list.Add(component);
                }
            }
            if (!__instance.gameObject.activeSelf)
            {
                __instance.gameObject.SetActive(true);
            }
            __instance.SetPrivate("group", _group);
            __instance.SetPrivate("category", _category);
            _lastCategory = _category;
            ItemList_Awake_Patches.ResetSearch(__instance);
            return false;
        }
#endif

    }

#if KOIKATSU
    [HarmonyPatch(typeof(CharaList), "Awake")]
    internal class CharaList_Awake_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static void Postfix(CharaList __instance)
        {
            Transform viewport = __instance.transform.Find("Scroll View/Viewport");

            RectTransform rt = viewport as RectTransform;
            rt.offsetMin += new Vector2(0f, 18f);
            float newY = rt.offsetMin.y;

            InputField searchBar = UIUtility.CreateInputField("Search Bar", viewport.parent, "Search...");
            searchBar.image.color = UIUtility.grayColor;
            searchBar.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 11f), new Vector2(-18f, newY));
            List<CharaFileInfo> items = ((CharaFileSort)__instance.GetPrivate("charaFileSort")).cfiList;
            searchBar.onValueChanged.AddListener(s => SearchUpdated(searchBar.text, items));
            foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
        }

        private static void SearchUpdated(string text, List<CharaFileInfo> items)
        {
            foreach (CharaFileInfo info in items)
                info.node.gameObject.SetActive(info.node.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1);
        }
    }

    [HarmonyPatch]
    internal class CostumeInfo_Init_Patches
    {
        internal static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }
        
        internal static MethodInfo TargetMethod()
        {
            return typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        private static void Postfix(object __instance)
        {
            CharaFileSort fileSort = (CharaFileSort)__instance.GetPrivate("fileSort");
            Transform viewport = fileSort.root.parent;

            RectTransform rt = viewport as RectTransform;
            rt.offsetMin += new Vector2(0f, 18f);

            InputField searchBar = UIUtility.CreateInputField("Search Bar", viewport.parent, "Search...");
            searchBar.image.color = UIUtility.grayColor;
            searchBar.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 11f), new Vector2(-18f, 33f));
            List<CharaFileInfo> items = fileSort.cfiList;
            searchBar.onValueChanged.AddListener(s => SearchUpdated(searchBar.text, items));
            foreach (Text t in searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
        }

        private static void SearchUpdated(string text, List<CharaFileInfo> items)
        {
            foreach (CharaFileInfo info in items)
                info.node.gameObject.SetActive(info.node.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1);
        }
    }

#elif HONEYSELECT
    internal class EntryListData
    {
        public class FolderData
        {
            public string fullPath;
            public string name;
            public GameObject displayObject;
            public Button button;
            public Text text;
        }

        public class EntryData
        {
            public GameSceneNode node;
            public Button button;
            public bool enabled;
        }

        public static Dictionary<object, EntryListData> dataByInstance = new Dictionary<object, EntryListData>();

        public string currentPath = "";
        public GameObject parentFolder;
        public readonly List<FolderData> folders = new List<FolderData>();
        public readonly List<EntryData> entries = new List<EntryData>();
        public CharaFileSort sort;
    }

    [HarmonyPatch(typeof(CharaList), "InitCharaList", typeof(bool))]
    internal static class CharaList_InitCharaList_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static bool Prefix(CharaList __instance, bool _force, CharaFileSort ___charaFileSort, int ___sex, GameObject ___objectNode, RawImage ___imageChara, Button ___buttonLoad, Button ___buttonChange)
        {
            if (__instance.isInit && !_force)
            {
                return false;
            }
            EntryListData data;
            if (EntryListData.dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new EntryListData();
                data.sort = ___charaFileSort;
                EntryListData.dataByInstance.Add(__instance, data);
            }
            ___charaFileSort.DeleteAllNode();
            string basePath;
            if (___sex == 1)
            {
                basePath = global::UserData.Path + "chara/female";
                __instance.CallPrivate("InitFemaleList");
            }
            else
            {
                basePath = global::UserData.Path + "chara/male";
                __instance.CallPrivate("InitMaleList");
            }
            string path = basePath + data.currentPath;

            if (data.parentFolder == null)
            {
                data.parentFolder = Object.Instantiate(___objectNode);
                data.parentFolder.transform.SetParent(___charaFileSort.root, false);
                Object.Destroy(data.parentFolder.GetComponent<GameSceneNode>());
                Text t = data.parentFolder.GetComponentInChildren<Text>();
                t.text = "../ (Parent folder)";
                t.alignment = TextAnchor.MiddleCenter;
                t.fontStyle = FontStyle.BoldAndItalic;
                data.parentFolder.GetComponent<Image>().color = HSUS.HSUS._self._subFoldersColor;
                data.parentFolder.GetComponent<Button>().onClick.AddListener(() =>
                {
                    int index = data.currentPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                    data.currentPath = data.currentPath.Remove(index);
                    Prefix(__instance, true, ___charaFileSort, ___sex, ___objectNode, ___imageChara, ___buttonLoad, ___buttonChange);
                });
            }

            data.parentFolder.SetActive(data.currentPath.Length > 1);

            string[] directories = Directory.GetDirectories(path);
            int i = 0;
            for (; i < directories.Length; i++)
            {
                string directory = directories[i];
                EntryListData.FolderData folder;
                if (i < data.folders.Count)
                    folder = data.folders[i];
                else
                {
                    folder = new EntryListData.FolderData();
                    folder.displayObject = Object.Instantiate(___objectNode);
                    folder.button = folder.displayObject.GetComponent<Button>();
                    folder.text = folder.displayObject.GetComponentInChildren<Text>();

                    folder.displayObject.SetActive(true);
                    folder.displayObject.transform.SetParent(___charaFileSort.root, false);
                    Object.Destroy(folder.displayObject.GetComponent<GameSceneNode>());
                    folder.text.alignment = TextAnchor.MiddleCenter;
                    folder.text.fontStyle = FontStyle.BoldAndItalic;
                    folder.displayObject.GetComponent<Image>().color = HSUS.HSUS._self._subFoldersColor;

                    data.folders.Add(folder);
                }
                folder.displayObject.SetActive(true);
                string localDirectory = directory.Replace("\\", "/").Replace(path, "");
                folder.text.text = localDirectory.Substring(1);
                folder.fullPath = directory;
                folder.name = localDirectory.Substring(1);
                folder.button.onClick = new Button.ButtonClickedEvent();
                folder.button.onClick.AddListener(() =>
                {
                    data.currentPath += localDirectory;
                    Prefix(__instance, true, ___charaFileSort, ___sex, ___objectNode, ___imageChara, ___buttonLoad, ___buttonChange);
                });
            }
            for (; i < data.folders.Count; ++i)
                data.folders[i].displayObject.SetActive(false);

            int count = ___charaFileSort.cfiList.Count;
            i = 0;
            MethodInfo onSelectCharaMI = __instance.GetType().GetMethod("OnSelectChara", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo loadCharaImageMI = __instance.GetType().GetMethod("LoadCharaImage", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            for (; i < count; i++)
            {
                CharaFileInfo info = ___charaFileSort.cfiList[i];
                info.index = i;
                EntryListData.EntryData chara;
                if (i < data.entries.Count)
                    chara = data.entries[i];
                else
                {
                    chara = new EntryListData.EntryData();
                    chara.node = Object.Instantiate(___objectNode).GetComponent<GameSceneNode>();
                    chara.button = chara.node.GetComponent<Button>();

                    chara.node.gameObject.transform.SetParent(___charaFileSort.root, false);
                    data.entries.Add(chara);
                }
                chara.enabled = true;
                chara.node.gameObject.SetActive(true);
                info.gameSceneNode = chara.node;
                info.button = chara.button;
                info.button.onClick = new Button.ButtonClickedEvent();
                Action<int> onSelect = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, onSelectCharaMI);
                info.gameSceneNode.AddActionToButton(delegate
                {
                    onSelect(info.index);
                });
                info.gameSceneNode.text = info.name;
                info.gameSceneNode.listEnterAction.Clear();
                Action<int> loadImage = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, loadCharaImageMI);
                info.gameSceneNode.listEnterAction.Add(delegate
                {
                    loadImage(info.index);
                });
            }
            for (; i < data.entries.Count; ++i)
            {
                data.entries[i].node.gameObject.SetActive(false);
                data.entries[i].enabled = false;
            }
            ___imageChara.color = Color.clear;
            ___charaFileSort.Sort(0, false);
            ___buttonLoad.interactable = false;
            ___buttonChange.interactable = false;
            __instance.GetType().GetProperty("isInit", BindingFlags.Instance|BindingFlags.Public).SetValue(__instance, true, null);
            return false;
        }

    }

    [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
    internal static class CharaList_InitFemaleList_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static bool Prefix(CharaList __instance, CharaFileSort ___charaFileSort)
        {
            EntryListData data = EntryListData.dataByInstance[__instance];
            string path = global::UserData.Path + "chara/female" + data.currentPath;
            List<string> list = Directory.GetFiles(path, "*.png").ToList();
            ___charaFileSort.cfiList.Clear();
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                CharFemaleFile charFemaleFile = null;
                if (SceneAssist.Assist.LoadFemaleCustomInfoAndParamerer(ref charFemaleFile, list[i]))
                {
                    if (!charFemaleFile.femaleCustomInfo.isConcierge)
                    {
                        ___charaFileSort.cfiList.Add(new CharaFileInfo(string.Empty, string.Empty)
                        {
                            file = list[i],
                            name = charFemaleFile.femaleCustomInfo.name,
                            time = File.GetLastWriteTime(list[i])
                        });
                    }
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(CharaList), "InitMaleList")]
    internal static class CharaList_InitMaleList_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static bool Prefix(CharaList __instance, CharaFileSort ___charaFileSort)
        {
            EntryListData data = EntryListData.dataByInstance[__instance];
            string path = global::UserData.Path + "chara/male" + data.currentPath;
            List<string> list = Directory.GetFiles(path, "*.png").ToList();
            ___charaFileSort.cfiList.Clear();
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                CharMaleFile charMaleFile = new CharMaleFile();
                if (Path.GetFileNameWithoutExtension(list[i]) != "ill_Player")
                {
                    if (charMaleFile.LoadBlockData(charMaleFile.maleCustomInfo, list[i]))
                    {
                        ___charaFileSort.cfiList.Add(new CharaFileInfo(string.Empty, string.Empty)
                        {
                            file = list[i],
                            name = charMaleFile.maleCustomInfo.name,
                            time = File.GetLastWriteTime(list[i])
                        });
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch]
    internal static class CostumeInfo_InitList_Patches
    {
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(Type.GetType("Studio.MPCharCtrl+CostumeInfo,Assembly-CSharp"), "InitList", new []{typeof(int)});
        }

        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static bool Prefix(object __instance, int _sex, ref int ___sex, CharaFileSort ___fileSort, GameObject ___prefabNode, Button ___buttonLoad, RawImage ___imageThumbnail)
        {
            EntryListData data;
            if (EntryListData.dataByInstance.TryGetValue(__instance, out data) == false)
            {
                data = new EntryListData();
                data.sort = ___fileSort;
                EntryListData.dataByInstance.Add(__instance, data);
            }
            if (___sex != _sex)
                data.currentPath = "";

            ___fileSort.DeleteAllNode();
            string basePath = _sex == 1 ? (global::UserData.Path + "coordinate/female") : (global::UserData.Path + "coordinate/male");
            string path = basePath + data.currentPath;

            if (data.parentFolder == null)
            {
                data.parentFolder = Object.Instantiate(___prefabNode);
                data.parentFolder.transform.SetParent(___fileSort.root, false);
                Object.Destroy(data.parentFolder.GetComponent<GameSceneNode>());
                Text t = data.parentFolder.GetComponentInChildren<Text>();
                t.text = "../ (Parent folder)";
                t.alignment = TextAnchor.MiddleCenter;
                t.fontStyle = FontStyle.BoldAndItalic;
                data.parentFolder.GetComponent<Image>().color = HSUS.HSUS._self._subFoldersColor;
                data.parentFolder.GetComponent<Button>().onClick.AddListener(() =>
                {
                    int index = data.currentPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                    data.currentPath = data.currentPath.Remove(index);
                    int refSex = _sex;
                    Prefix(__instance, _sex, ref refSex, ___fileSort, ___prefabNode, ___buttonLoad, ___imageThumbnail);
                });
            }

            data.parentFolder.SetActive(data.currentPath.Length > 1);

            string[] directories = Directory.GetDirectories(path);
            int i = 0;
            for (; i < directories.Length; i++)
            {
                EntryListData.FolderData folder;
                if (i < data.folders.Count)
                    folder = data.folders[i];
                else
                {
                    folder = new EntryListData.FolderData();
                    folder.displayObject = Object.Instantiate(___prefabNode);
                    folder.text = folder.displayObject.GetComponentInChildren<Text>();
                    folder.button = folder.displayObject.GetComponent<Button>();

                    folder.text.alignment = TextAnchor.MiddleCenter;
                    folder.text.fontStyle = FontStyle.BoldAndItalic;
                    folder.displayObject.GetComponent<Image>().color = HSUS.HSUS._self._subFoldersColor;
                    folder.displayObject.transform.SetParent(___fileSort.root, false);
                    Object.Destroy(folder.displayObject.GetComponent<GameSceneNode>());
                    data.folders.Add(folder);
                }
                folder.displayObject.SetActive(true);
                string directory = directories[i];
                string localDirectory = directory.Replace("\\", "/").Replace(path, "");
                folder.text.text = localDirectory.Substring(1);
                folder.fullPath = directory;
                folder.name = localDirectory.Substring(1);
                folder.button.onClick = new Button.ButtonClickedEvent();
                folder.button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    data.currentPath += localDirectory;
                    int refSex = _sex;
                    Prefix(__instance, _sex, ref refSex, ___fileSort, ___prefabNode, ___buttonLoad, ___imageThumbnail);
                });
            }
            for (; i < data.folders.Count; ++i)
                data.folders[i].displayObject.SetActive(false);

            InitFileList(_sex, ___fileSort, data);
            int count = ___fileSort.cfiList.Count;
            MethodInfo onSelectMI = __instance.GetType().GetMethod("OnSelect", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            MethodInfo loadImageMI = __instance.GetType().GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            i = 0;
            for (; i < count; i++)
            {
                EntryListData.EntryData coord;
                if (i < data.entries.Count)
                    coord = data.entries[i];
                else
                {
                    coord = new EntryListData.EntryData();
                    coord.node = Object.Instantiate(___prefabNode).GetComponent<GameSceneNode>();
                    coord.button = coord.node.GetComponent<Button>();
                    coord.node.transform.SetParent(___fileSort.root, false);
                    data.entries.Add(coord);
                }
                coord.enabled = true;
                coord.node.gameObject.SetActive(true);
                CharaFileInfo info = ___fileSort.cfiList[i];
                info.gameSceneNode = coord.node;
                info.index = i;
                info.button = coord.button;
                Action<int> onSelect = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, onSelectMI);
                info.button.onClick = new Button.ButtonClickedEvent();
                info.gameSceneNode.AddActionToButton(delegate
                {
                    onSelect(info.index);
                });
                info.gameSceneNode.text = info.name;
                Action<int> loadImage = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), __instance, loadImageMI);
                info.gameSceneNode.listEnterAction.Clear();
                info.gameSceneNode.listEnterAction.Add(delegate
                {
                    loadImage(info.index);
                });
            }
            for (; i < data.entries.Count; ++i)
            {
                data.entries[i].node.gameObject.SetActive(false);
                data.entries[i].enabled = false;
            }
            ___sex = _sex;
            ___fileSort.Sort(0, false);
            ___buttonLoad.interactable = false;
            ___imageThumbnail.color = Color.clear;
            return false;
        }

        private static void InitFileList(int _sex, CharaFileSort ___fileSort, EntryListData data)
        {
            string path = global::UserData.Path + (_sex != 1 ? "coordinate/male" : "coordinate/female") + data.currentPath;
            List<string> list = Directory.GetFiles(path, "*.png").ToList();
            ___fileSort.cfiList.Clear();
            int count = list.Count;
            CharFileInfoClothes charFileInfoClothes;
            if (_sex == 1)
                charFileInfoClothes = new CharFileInfoClothesFemale();
            else
                charFileInfoClothes = new CharFileInfoClothesMale();
            for (int i = 0; i < count; i++)
            {
                if (charFileInfoClothes.Load(list[i], true))
                {
                    ___fileSort.cfiList.Add(new CharaFileInfo(list[i], charFileInfoClothes.comment)
                    {
                        time = File.GetLastWriteTime(list[i])
                    });
                }
            }
        }
    }

    [HarmonyPatch(typeof(CharaFileSort), "DeleteAllNode")]
    internal static class CharaFileSort_DeleteAllNode_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static bool Prefix(ref int ___m_Select)
        {
            ___m_Select = -1;
            return false;
        }
    }

    [HarmonyPatch(typeof(CharaFileSort), "SortTime", typeof(bool))]
    internal static class CharaFileSort_SortTime_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static void Postfix(CharaFileSort __instance, bool _ascend, bool[] ___sortType)
        {
            EntryListData data = EntryListData.dataByInstance.FirstOrDefault(e => e.Value.sort == __instance).Value;
            if (data != null)
            {
                ___sortType[1] = _ascend;
                CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
                if (_ascend)
                {
                    data.folders.Sort((a, b) => Directory.GetLastWriteTime(a.fullPath).CompareTo(Directory.GetLastWriteTime(b.fullPath)));
                }
                else
                {
                    data.folders.Sort((a, b) => Directory.GetLastWriteTime(b.fullPath).CompareTo(Directory.GetLastWriteTime(a.fullPath)));
                }
                Thread.CurrentThread.CurrentCulture = currentCulture;
                for (int i = data.folders.Count - 1; i >= 0; i--)
                {
                    data.folders[i].displayObject.transform.SetAsFirstSibling();
                }
                data.parentFolder.transform.SetAsFirstSibling();
            }
        }
    }

    [HarmonyPatch(typeof(CharaFileSort), "SortName", typeof(bool))]
    internal static class CharaFileSort_SortName_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static void Postfix(CharaFileSort __instance, bool _ascend, bool[] ___sortType)
        {
            EntryListData data = EntryListData.dataByInstance.FirstOrDefault(e => e.Value.sort == __instance).Value;
            if (data != null)
            {
                ___sortType[1] = _ascend;
                CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
                if (_ascend)
                {
                    data.folders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase));
                }
                else
                {
                    data.folders.Sort((a, b) => string.Compare(b.name, a.name, StringComparison.CurrentCultureIgnoreCase));
                }
                Thread.CurrentThread.CurrentCulture = currentCulture;
                for (int i = data.folders.Count - 1; i >= 0; i--)
                {
                    data.folders[i].displayObject.transform.SetAsFirstSibling();
                }
                data.parentFolder.transform.SetAsFirstSibling();
            }
        }
    }

    [HarmonyPatch]
    internal static class StudioCharaListSortUtil_ExecuteSort_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo && Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon") != null;
        }

        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(Type.GetType("HSStudioNEOAddon.StudioCharaListSortUtil,HSStudioNEOAddon"), "ExecuteSort");
        }

        private static void Postfix(CharaFileSort ___charaFileSort)
        {
            EntryListData data = EntryListData.dataByInstance.FirstOrDefault(e => e.Value.sort == ___charaFileSort).Value;
            if (data == null)
                return;
            foreach (EntryListData.EntryData chara in data.entries)
                chara.node.gameObject.SetActive(chara.node.gameObject.activeSelf && chara.enabled);
        }
    }

    [HarmonyPatch]
    internal static class TextSlideEffectCtrl_Check_Patches
    {
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(Type.GetType("Studio.TextSlideEffectCtrl,Assembly-CSharp"), "Check");
        }

        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo && Type.GetType("Studio.TextSlideEffectCtrl,Assembly-CSharp") != null;
        }

        private static bool Prefix(object __instance, Text ___text, object ___textSlideEffect)
        {
            float preferredWidth = ___text.preferredWidth;
            if (preferredWidth < 104)
            {
                ObservableLateUpdateTrigger component = ((Component)__instance).GetComponent<ObservableLateUpdateTrigger>();
                if (component != null)
                    Object.Destroy(component);
                Object.Destroy((Component)__instance);
                Object.Destroy((Component)___textSlideEffect);
                return false;
            }
            ___text.alignment = (TextAnchor)3;
            ___text.horizontalOverflow = (HorizontalWrapMode)1;
            ___text.raycastTarget = true;
            __instance.CallPrivate("AddFunc");
            return false;
        }
    }


#endif

#if HONEYSELECT
    [HarmonyPatch(typeof(BackgroundList), "InitList")]
    public class BackgroundList_InitList_Patches
    {
        private static InputField _searchBar;
        private static RectTransform _parent;

        public static bool Prepare()
        {
            return HSUS.HSUS.self._optimizeNeo && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }

        public static void Postfix(object __instance)
        {
            _parent = (RectTransform)((RectTransform)__instance.GetPrivate("transformRoot")).parent.parent;

            _searchBar = UIUtility.CreateInputField("Search Bar", _parent, "Search...");
            Image image = _searchBar.GetComponent<Image>();
            image.color = UIUtility.grayColor;
            RectTransform rt = _searchBar.transform as RectTransform;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;
            rt.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, -21f), new Vector2(0f, 1f));
            _searchBar.onValueChanged.AddListener(s => SearchChanged(__instance));
            foreach (Text t in _searchBar.GetComponentsInChildren<Text>())
                t.color = Color.white;
        }

        private static void SearchChanged(object instance)
        {
            Dictionary<int, StudioNode> dicNode = (Dictionary<int, StudioNode>)instance.GetPrivate("dicNode");
            string search = _searchBar.text.Trim();
            foreach (KeyValuePair<int, StudioNode> pair in dicNode)
            {
                pair.Value.active = pair.Value.textUI.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) != -1;
            }
            LayoutRebuilder.MarkLayoutForRebuild(_parent);
        }
    }
#endif

#if HONEYSELECT
    [HarmonyPatch]
#elif KOIKATSU
    [HarmonyPatch(typeof(OICharInfo), new []{typeof(ChaFileControl), typeof(int)})]
#endif
    public class OICharInfo_Ctor_Patches
    {
#if HONEYSELECT
        internal static MethodBase TargetMethod()
        {
            return typeof(OICharInfo).GetConstructor(new[] { typeof(CharFile), typeof(int) });
        }
#endif

        public static bool Prepare()
        {
            return HSUS.HSUS._self._autoJointCorrection;
        }

        public static void Postfix(OICharInfo __instance)
        {
            for (int i = 0; i < __instance.expression.Length; i++)
                __instance.expression[i] = true;
        }
    }

#if HONEYSELECT
    [HarmonyPatch]
#elif KOIKATSU
    [HarmonyPatch(typeof(ChaFileStatus))]
#endif
    public class CharFileInfoStatus_Ctor_Patches
    {
#if HONEYSELECT
        internal static MethodBase TargetMethod()
        {
            return typeof(CharFileInfoStatus).GetConstructor(new Type[]{});
        }

        public static void Postfix(CharFileInfoStatus __instance)
#elif KOIKATSU
        public static void Postfix(ChaFileStatus __instance)
#endif
        {
            __instance.eyesBlink = HSUS.HSUS._self._eyesBlink;
        }
    }

#if HONEYSELECT
    [HarmonyPatch(typeof(StartScene), "Start")]
    public class StartScene_Start_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static bool Prefix(System.Object __instance)
        {
            if (__instance as StartScene)
            {
                Studio.Info.Instance.LoadExcelData();
                Scene.Instance.SetFadeColor(Color.black);
                Scene.Instance.LoadReserv("Studio", true);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GuideObject), "Start")]
    public class GuideObject_Start_Patches
    {
        public static bool Prepare(HarmonyInstance instance)
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(GuideObject __instance)
        {
            Action a = () => GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            Action<Vector3> a2 = (v) => GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            __instance.changeAmount.onChangePos = (Action)Delegate.Combine(__instance.changeAmount.onChangePos, a);
            __instance.changeAmount.onChangeRot = (Action)Delegate.Combine(__instance.changeAmount.onChangeRot, a);
            __instance.changeAmount.onChangeScale = (Action<Vector3>)Delegate.Combine(__instance.changeAmount.onChangeScale, a2);
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
            __instance.ExecuteDelayed(() =>
            {
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance); //Probably for HSPE
            }, 4);
        }
    }

    [HarmonyPatch]
    public class ABMStudioNEOSaveLoadHandler_OnLoad_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo && HSUS.HSUS._self._optimizeNeo && Type.GetType("AdditionalBoneModifier.ABMStudioNEOSaveLoadHandler,AdditionalBoneModifierStudioNEO") != null;
        }

        private static MethodInfo TargetMethod()
        {
            return Type.GetType("AdditionalBoneModifier.ABMStudioNEOSaveLoadHandler,AdditionalBoneModifierStudioNEO").GetMethod("OnLoad", BindingFlags.Public | BindingFlags.Instance);
        }

        private static void Postfix()
        {
            GuideObjectManager.Instance.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<Transform, GuideObject> pair in (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject"))
                {
                    GuideObject_LateUpdate_Patches.ScheduleForUpdate(pair.Value);
                }
            });
        }
    }

    [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
    public class GuideObject_LateUpdate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static readonly Dictionary<GuideObject, bool> _instanceData = new Dictionary<GuideObject, bool>(); //Apparently, doing this is faster than having a simple HashSet...

        public static void ScheduleForUpdate(GuideObject obj)
        {
            if (_instanceData.ContainsKey(obj) == false)
                _instanceData.Add(obj, true);
            else
                _instanceData[obj] = true;
        }


        public static bool Prefix(GuideObject __instance, GameObject[] ___roots)
        {
            __instance.transform.position = __instance.transformTarget.position;
            __instance.transform.rotation = __instance.transformTarget.rotation;
            if (_instanceData.TryGetValue(__instance, out bool b) && b)
            {
                switch (__instance.mode)
                {
                    case GuideObject.Mode.Local:
                        ___roots[0].transform.rotation = __instance.parent != null ? __instance.parent.rotation : Quaternion.identity;
                        break;
                    case GuideObject.Mode.World:
                        ___roots[0].transform.rotation = Quaternion.identity;
                        break;
                }
                if (__instance.calcScale)
                {
                    Vector3 localScale = __instance.transformTarget.localScale;
                    Vector3 lossyScale = __instance.transformTarget.lossyScale;
                    Vector3 vector3 = !__instance.enableScale ? Vector3.one : __instance.changeAmount.scale;
                    __instance.transformTarget.localScale = new Vector3(localScale.x / lossyScale.x * vector3.x, localScale.y / lossyScale.y * vector3.y, localScale.z / lossyScale.z * vector3.z);
                }
                _instanceData[__instance] = false;
            }
            return false;
        }

    }


    [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
    public class Studio_Duplicate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        public static void Postfix(Studio.Studio __instance)
        {
            foreach (GuideObject guideObject in Resources.FindObjectsOfTypeAll<GuideObject>())
            {
                
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(guideObject);
            }
        }
    }

    [HarmonyPatch(typeof(ChangeAmount), "set_scale", new[] {typeof(Vector3)})]
    public class ChangeAmount_set_scale_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        public static void Postfix(ChangeAmount __instance)
        {
            foreach (KeyValuePair<TreeNodeObject, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicInfo)
            {
                if (pair.Value.guideObject.changeAmount == __instance)
                {
                    Recurse(pair.Key, info => GuideObject_LateUpdate_Patches.ScheduleForUpdate(info.guideObject));
                    break;
                }
            }
        }

        private static void Recurse(TreeNodeObject node, Action<ObjectCtrlInfo> action)
        {
            if (Studio.Studio.Instance.dicInfo.TryGetValue(node, out ObjectCtrlInfo info))
                action(info);
            foreach (TreeNodeObject child in node.child)
                Recurse(child, action);
        }
    }

    [HarmonyPatch(typeof(GuideObject), "set_calcScale", new[] { typeof(bool) })]
    public class GuideObject_set_calcScale_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }
        public static void Postfix(GuideObject __instance)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
        }
    }
    [HarmonyPatch(typeof(GuideObject), "set_enableScale", new[] { typeof(bool) })]
    public class GuideObject_set_enableScale_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(GuideObject __instance)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
        }
    }
    [HarmonyPatch(typeof(GuideObject), "set_enablePos", new[] { typeof(bool) })]
    public class GuideObject_set_enablePos_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(GuideObject __instance)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
        }
    }
    [HarmonyPatch(typeof(GuideObject), "set_enableRot", new[] { typeof(bool) })]
    public class GuideObject_set_enableRot_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(GuideObject __instance)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(OCIChar), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCIFolder), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCIItem), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCILight), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCIPathMove), "OnAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    public class ObjectCtrlInfo_OnAttach_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS.self._optimizeNeo;
        }
        public static void Postfix(ObjectCtrlInfo __instance, TreeNodeObject _parent, ObjectCtrlInfo _child)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance.guideObject);
        }
    }
    [HarmonyPatch(typeof(OCIChar), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCIFolder), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCIItem), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCILight), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    [HarmonyPatch(typeof(OCIPathMove), "OnLoadAttach", new[] { typeof(TreeNodeObject), typeof(ObjectCtrlInfo) })]
    public class ObjectCtrlInfo_OnLoadAttach_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(ObjectCtrlInfo __instance, TreeNodeObject _parent, ObjectCtrlInfo _child)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance.guideObject);
        }
    }
    [HarmonyPatch(typeof(OCIChar), "OnDetach")]
    [HarmonyPatch(typeof(OCIFolder), "OnDetach")]
    [HarmonyPatch(typeof(OCIItem), "OnDetach")]
    [HarmonyPatch(typeof(OCILight), "OnDetach")]
    [HarmonyPatch(typeof(OCIPathMove), "OnDetach")]
    public class ObjectCtrlInfo_OnDetach_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(ObjectCtrlInfo __instance)
        {
            GuideObject_LateUpdate_Patches.ScheduleForUpdate(__instance.guideObject);
        }
    }
    [HarmonyPatch(typeof(TreeNodeCtrl), "SetSelectNode", new []{typeof(TreeNodeObject) })]
    public class TreeNodeCtrl_SetSelectNode_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }
        public static void Postfix(TreeNodeCtrl __instance, TreeNodeObject _node)
        {
            ObjectCtrlInfo objectCtrlInfo = TryGetLoop(_node);
            if (objectCtrlInfo != null)
                GuideObject_LateUpdate_Patches.ScheduleForUpdate(objectCtrlInfo.guideObject);
        }

        private static ObjectCtrlInfo TryGetLoop(TreeNodeObject _node)
        {
            if (_node == null)
                return null;
            if (Studio.Studio.Instance.dicInfo.TryGetValue(_node, out ObjectCtrlInfo result))
                return result;
            return TryGetLoop(_node.parent);
        }
    }

    [HarmonyPatch(typeof(WorkspaceCtrl), "Awake")]
    internal static class WorkspaceCtrl_Awake_Patches
    {
        private static List<TreeNodeObject> _treeNodeList;
        internal static InputField _search;
        internal static bool _ignoreSearch = false;

        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static void Postfix(WorkspaceCtrl __instance, TreeNodeCtrl ___treeNodeCtrl)
        {
            RectTransform viewport = __instance.transform.Find("Image Bar/Scroll View").GetComponent<ScrollRect>().viewport;
            _search = UIUtility.CreateInputField("Search", viewport.parent, "Search...");
            _search.transform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(viewport.offsetMin.x, viewport.offsetMin.y - 2f), new Vector2(viewport.offsetMax.x, viewport.offsetMin.y + 18));
            viewport.offsetMin += new Vector2(0f, 18f);
            foreach (Text t in _search.GetComponentsInChildren<Text>())
                t.color = Color.white;
            Image image = _search.GetComponent<Image>();
            image.color = UIUtility.grayColor;
            _search.onValueChanged.AddListener(OnSearchChanged);
            _treeNodeList = (List<TreeNodeObject>)___treeNodeCtrl.GetPrivate("m_TreeNodeObject");
        }

        internal static void OnSearchChanged(string s = "")
        {
            if (_ignoreSearch)
                return;
            foreach (TreeNodeObject o in _treeNodeList)
            {
                if (o.parent == null)
                    PartialSearch(o);
            }
        }

        internal static void PartialSearch(TreeNodeObject o)
        {
            if (_ignoreSearch)
                return;
            string searchText = _search.text;
            RecurseAny(o, t => t.textName.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) != -1,
                       (t, r) => t.gameObject.SetActive(r && t.AllParentsOpen()));

        }

        private static bool RecurseAny(TreeNodeObject obj, Func<TreeNodeObject, bool> func, Action<TreeNodeObject, bool> onResult)
        {
            bool res = func(obj);
            foreach (TreeNodeObject child in obj.child)
            {
                if (RecurseAny(child, func, onResult))
                    res = true;
            }
            onResult(obj, res);
            return res;
        }

        private static bool AllParentsOpen(this TreeNodeObject self)
        {
            if (self.parent == null)
                return true;
            TreeNodeObject parent = self.parent;
            while (parent != null)
            {
                if (parent.treeState == TreeNodeObject.TreeState.Close)
                    return false;
                parent = parent.parent;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickDuplicate")]
    internal static class WorkspaceCtrl_OnClickDuplicate_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickParent")]
    internal static class WorkspaceCtrl_OnClickParent_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "OnParentage", new []{typeof(TreeNodeObject), typeof(TreeNodeObject)})]
    internal static class WorkspaceCtrl_OnParentage_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickDelete")]
    internal static class WorkspaceCtrl_OnClickDelete_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickFolder")]
    internal static class WorkspaceCtrl_OnClickFolder_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "OnClickRemove")]
    internal static class WorkspaceCtrl_OnClickRemove_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "UpdateUI")]
    internal static class WorkspaceCtrl_UpdateUI_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS.self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix()
        {
            WorkspaceCtrl_Awake_Patches.OnSearchChanged();
        }
    }

    [HarmonyPatch(typeof(TreeNodeObject), "SetTreeState", new[] {typeof(TreeNodeObject.TreeState)})]
    [HarmonyPatch(typeof(TreeNodeObject), "set_treeState", new[] {typeof(TreeNodeObject.TreeState)})]
    internal static class TreeNodeObject_SetTreeState_MultiPatch
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static void Postfix(TreeNodeObject __instance)
        {
            if (WorkspaceCtrl_Awake_Patches._search.text == string.Empty)
                return;
            WorkspaceCtrl_Awake_Patches.PartialSearch(__instance);
        }
    }

    [HarmonyPatch(typeof(Studio.Studio), "LoadScene", new[] {typeof(string)})]
    internal static class Studio_LoadScene_MultiPatch
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo;
        }

        private static void Prefix()
        {
            WorkspaceCtrl_Awake_Patches._ignoreSearch = true;
        }
        private static void Postfix()
        {
            WorkspaceCtrl_Awake_Patches._ignoreSearch = false;
        }
    }

    [HarmonyPatch(typeof(TreeNodeCtrl), "AddNode", new[] {typeof(string), typeof(TreeNodeObject)})]
    internal static class TreeNodeCtrl_AddNode_MultiPatch
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._optimizeNeo && HSUS.HSUS._self._translationMethod != null;
        }

        private static void Postfix(TreeNodeObject __result)
        {
            string name = __result.textName;
            HSUS.HSUS._self._translationMethod(ref name);
            __result.textName = name;
        }
    }

    [HarmonyPatch]
    internal static class BoneInfo_Ctor_Patches
    {
        private static ConstructorInfo TargetMethod()
        {
            return typeof(OCIChar.BoneInfo).GetConstructor(new[] { typeof(GuideObject), typeof(OIBoneInfo)});
        }

        private static bool Prepare()
        {
            return HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static void Postfix(GuideObject _guideObject, OIBoneInfo _boneInfo)
        {
            switch (_boneInfo.group)
            {
                case OIBoneInfo.BoneGroup.Body:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkBodyColor;
                    break;
                case (OIBoneInfo.BoneGroup)3:
                case OIBoneInfo.BoneGroup.RightLeg:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkBodyColor;
                    break;
                case (OIBoneInfo.BoneGroup)5:
                case OIBoneInfo.BoneGroup.LeftLeg:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkBodyColor;
                    break;
                case (OIBoneInfo.BoneGroup)9:
                case OIBoneInfo.BoneGroup.RightArm:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkBodyColor;
                    break;
                case (OIBoneInfo.BoneGroup)17:
                case OIBoneInfo.BoneGroup.LeftArm:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkBodyColor;
                    break;
                case OIBoneInfo.BoneGroup.RightHand:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkRightHandColor;
                    break;
                case OIBoneInfo.BoneGroup.LeftHand:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkLeftHandColor;
                    break;
                case OIBoneInfo.BoneGroup.Hair:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkHairColor;
                    break;
                case OIBoneInfo.BoneGroup.Neck:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkNeckColor;
                    break;
                case OIBoneInfo.BoneGroup.Breast:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkChestColor;
                    break;
                case OIBoneInfo.BoneGroup.Skirt:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkSkirtColor;
                    break;
                case (OIBoneInfo.BoneGroup)0:
                    _guideObject.guideSelect.color = HSUS.HSUS._self._fkItemsColor;
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(AddObjectFemale), "Add", typeof(global::CharFemale), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
    internal static class AddObjectFemale_Add_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            bool set2 = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                if (set == false && inst.opcode == OpCodes.Ldnull && instructionsList[i + 1].ToString().Equals("call Studio.TreeNodeObject AddNode(System.String, Studio.TreeNodeObject)"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2); //_parent
                    yield return new CodeInstruction(OpCodes.Ldarg_3); //_parentNode
                    yield return new CodeInstruction(OpCodes.Call, typeof(AddObjectFemale_Add_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));

                    set = true;
                }
                else if (set2 == false && inst.opcode == OpCodes.Ldc_I4_0 && instructionsList[i + 1].ToString().Equals("callvirt Void set_enableChangeParent(Boolean)"))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    set2 = true;
                }
                else
                    yield return inst;
            }
        }

        private static TreeNodeObject Injected(ObjectCtrlInfo _parent, TreeNodeObject _parentNode)
        {
            return _parentNode == null ? (_parent == null ? null : _parent.treeNodeObject) : _parentNode;
        }
    }

    [HarmonyPatch(typeof(AddObjectMale), "Add", typeof(global::CharMale), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
    internal static class AddObjectMale_Add_Patches
    {
        private static bool Prepare()
        {
            return HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            bool set2 = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                if (set == false && inst.opcode == OpCodes.Ldnull && instructionsList[i + 1].ToString().Equals("call Studio.TreeNodeObject AddNode(System.String, Studio.TreeNodeObject)"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2); //_parent
                    yield return new CodeInstruction(OpCodes.Ldarg_3); //_parentNode
                    yield return new CodeInstruction(OpCodes.Call, typeof(AddObjectMale_Add_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));

                    set = true;
                }
                else if (set2 == false && inst.opcode == OpCodes.Ldc_I4_0 && instructionsList[i + 1].ToString().Equals("callvirt Void set_enableChangeParent(Boolean)"))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    set2 = true;
                }
                else
                    yield return inst;
            }
        }

        private static TreeNodeObject Injected(ObjectCtrlInfo _parent, TreeNodeObject _parentNode)
        {
            return _parentNode == null ? (_parent == null ? null : _parent.treeNodeObject) : _parentNode;
        }
    }

    [HarmonyPatch(typeof(OCIChar), "OnDetach")]
    internal static class OCIChar_OnDetach_Patches
    {
        private static void Postfix(OCIChar __instance)
        {
            __instance.parentInfo.OnDetachChild(__instance);
            __instance.guideObject.parent = null;
            Studio.Studio.AddInfo(__instance.objectInfo, __instance);
            __instance.guideObject.transformTarget.SetParent(Scene.Instance.commonSpace.transform);
            __instance.objectInfo.changeAmount.pos = __instance.guideObject.transformTarget.localPosition;
            __instance.objectInfo.changeAmount.rot = __instance.guideObject.transformTarget.localEulerAngles;
            __instance.guideObject.calcMode = GuideObject.Mode.Local;
        }
    }
#endif

}
