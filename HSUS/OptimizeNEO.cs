using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using Manager;
using Studio;
using ToolBox;
using UILib;
using UnityEngine;
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

    internal class CostumeInfo_Init_Patches
    {
        internal static void ManualPatch(HarmonyInstance harmony)
        {
            if (HSUS.HSUS._self._optimizeNeo)
            {
                harmony.Patch(typeof(MPCharCtrl).GetNestedType("CostumeInfo", BindingFlags.NonPublic).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy), null, new HarmonyMethod(typeof(CostumeInfo_Init_Patches), nameof(Postfix)));
            }
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
#endif

#if HONEYSELECT
    [HarmonyPatch(typeof(BackgroundList), "InitList")]
    public class BackgroundList_InitList_Patches
    {
        private static InputField _searchBar;
        private static RectTransform _parent;

        public static bool Prepare()
        {
            return HSUS.HSUS.self._optimizeNeo;
        }

        public static void Postfix(object __instance)
        {
            _parent = (RectTransform)((RectTransform)__instance.GetPrivate("transformRoot")).parent.parent;

            _searchBar = UIUtility.CreateInputField("Search Bar", _parent, "Search...");
            Image image = _searchBar.GetComponent<Image>();
            image.color = UIUtility.grayColor;
            //image.sprite = null;
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
    [HarmonyPatch(typeof(OICharInfo), new []{typeof(CharFile), typeof(int)})]
#elif KOIKATSU
    [HarmonyPatch(typeof(OICharInfo), new []{typeof(ChaFileControl), typeof(int)})]
#endif
    public class OICharInfo_Ctor_Patches
    {
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
    [HarmonyPatch(typeof(CharFileInfoStatus))]
#elif KOIKATSU
    [HarmonyPatch(typeof(ChaFileStatus))]
#endif
    public class CharFileInfoStatus_Ctor_Patches
    {
#if HONEYSELECT
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

    public class ABMStudioNEOSaveLoadHandler_OnLoad_Patches
    {
        public static void ManualPatch(HarmonyInstance harmony)
        {
            Type t = Type.GetType("AdditionalBoneModifier.ABMStudioNEOSaveLoadHandler,AdditionalBoneModifierStudioNEO");
            if (t != null)
            {
                harmony.Patch(t.GetMethod("OnLoad", BindingFlags.Public | BindingFlags.Instance), null, new HarmonyMethod(typeof(ABMStudioNEOSaveLoadHandler_OnLoad_Patches).GetMethod(nameof(Postfix), BindingFlags.NonPublic | BindingFlags.Static)));
            }
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
            return HSUS.HSUS._self._optimizeNeo;
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
#endif

}
