using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using ToolBox;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HSUS
{
    public class ObjectTreeDebug : MonoBehaviour
    {
        private struct ObjectPair
        {
            public readonly object parent;
            public readonly object child;
            private readonly int _hashCode;

            public ObjectPair(object inParent, object inChild)
            {
                this.parent = inParent;
                this.child = inChild;
                this._hashCode = -157375006;
                this._hashCode = this._hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(this.parent);
                this._hashCode = this._hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(this.child);
            }

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }

        private Transform _target;
        private readonly HashSet<GameObject> _openedGameObjects = new HashSet<GameObject>();
        private Vector2 _scroll;
        private Vector2 _scroll2;
        private static Vector2 _scroll3;
        private static readonly LinkedList<KeyValuePair<LogType, string>> _lastlogs = new LinkedList<KeyValuePair<LogType, string>>();
        private static bool _debug;
        private Rect _rect = new Rect(Screen.width / 4f, Screen.height / 4f, Screen.width / 2f, Screen.height / 2f);
        private int _randomId;
        private static readonly Process _process;
        private static readonly byte _bits;
        private readonly HashSet<ObjectPair> _openedObjects = new HashSet<ObjectPair>();
        private static string _goSearch = "";
        private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };


#if HONEYSELECT
        private static readonly bool _has630Patch;
#endif

        static ObjectTreeDebug()
        {
            Application.logMessageReceived += HandleLog;
            _process = Process.GetCurrentProcess();
            if (IntPtr.Size == 4)
                _bits = 32;
            else if (IntPtr.Size == 8)
                _bits = 64;
#if HONEYSELECT
            _has630Patch = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "HoneySelect_" + _bits + "_Data\\Managed\\Vectrosity.dll"));
#endif
        }

        void Awake()
        {
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);
        }

        void Update()
        {
            if (HSUS._self._debugEnabled && Input.GetKeyDown(HSUS._self.debugShortcut))
                _debug = !_debug;
        }

        void OnDestroy()
        {
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            _lastlogs.AddLast(new KeyValuePair<LogType, string>(type, type + " " + condition));
            if (_lastlogs.Count == 1001)
                _lastlogs.RemoveFirst();
            _scroll3.y += 999999;
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            if (_goSearch.Length == 0 || go.name.IndexOf(_goSearch, StringComparison.OrdinalIgnoreCase) != -1)
            {
                Color c = GUI.color;
                if (this._target == go.transform)
                    GUI.color = Color.cyan;
                GUILayout.BeginHorizontal();

                if (_goSearch.Length == 0)
                {
                    GUILayout.Space(indent * 20f);
                    if (go.transform.childCount != 0)
                    {
                        if (GUILayout.Toggle(this._openedGameObjects.Contains(go), "", GUILayout.ExpandWidth(false)))
                        {
                            if (this._openedGameObjects.Contains(go) == false)
                                this._openedGameObjects.Add(go);
                        }
                        else
                        {
                            if (this._openedGameObjects.Contains(go))
                                this._openedGameObjects.Remove(go);
                        }
                    }
                    else
                        GUILayout.Space(20f);
                }
                if (GUILayout.Button(go.name, GUILayout.ExpandWidth(false)))
                {
                    this._target = go.transform;
                    if (_goSearch.Length != 0)
                    {
                        Transform t = this._target.parent;
                        while (t != null)
                        {
                            if (this._openedGameObjects.Contains(t.gameObject) == false)
                                this._openedGameObjects.Add(t.gameObject);
                            t = t.parent;
                        }
                    }
                }
                GUI.color = c;
                go.SetActive(GUILayout.Toggle(go.activeSelf, "", GUILayout.ExpandWidth(false)));
                GUILayout.EndHorizontal();
            }
            if (_goSearch.Length != 0 || this._openedGameObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        void OnGUI()
        {
            if (_debug == false)
                return;
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.6f);
            GUI.Box(this._rect, "", _customBoxStyle);
            GUI.backgroundColor = c;
            this._rect = GUILayout.Window(this._randomId, this._rect, this.WindowFunc, "Debug Console " + HSUS._version + ": " + _process.ProcessName + " | " + _bits + "bits"
#if HONEYSELECT
                                                                                       + " | 630 patch: " + (_has630Patch ? "Yes" : "No")
#endif
                                         );
        }

        private void WindowFunc(int id)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            _goSearch = GUILayout.TextField(_goSearch);
            this._scroll = GUILayout.BeginScrollView(this._scroll, GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.MinWidth(300));
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.parent == null)
                    this.DisplayObjectTree(t.gameObject, 0);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            this._scroll2 = GUILayout.BeginScrollView(this._scroll2, GUI.skin.box);
            if (this._target != null)
            {
                Transform t = this._target.parent;
                string n = this._target.name;
                while (t != null)
                {
                    n = t.name + "/" + n;
                    t = t.parent;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(n);
                if (GUILayout.Button("Copy to clipboard", GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = n;
                GUILayout.EndHorizontal();
                GUILayout.Label("Layer: " + LayerMask.LayerToName(this._target.gameObject.layer) + " " + this._target.gameObject.layer);
                GUILayout.Label("Tag: " + this._target.gameObject.tag);
                foreach (Component c in this._target.GetComponents<Component>())
                {
                    if (c == null)
                        continue;
                    GUILayout.BeginHorizontal();
                    MonoBehaviour m = c as MonoBehaviour;
                    if (m != null)
                        m.enabled = GUILayout.Toggle(m.enabled, c.GetType().FullName, GUILayout.ExpandWidth(false));
                    else if (c is Animator)
                    {
                        Animator an = (Animator)c;
                        an.enabled = GUILayout.Toggle(an.enabled, c.GetType().FullName, GUILayout.ExpandWidth(false));
                    }
                    else
                        GUILayout.Label(c.GetType().FullName, GUILayout.ExpandWidth(false));

                    ObjectPair pair = new ObjectPair(this._target, c);
                    if (GUILayout.Toggle(this._openedObjects.Contains(pair), ""))
                    {
                        if (this._openedObjects.Contains(pair) == false)
                            this._openedObjects.Add(pair);
                    }
                    else
                    {
                        if (this._openedObjects.Contains(pair))
                            this._openedObjects.Remove(pair);
                    }

                    if (c is Image)
                    {
                        Image img = c as Image;
                        if (img.sprite != null && img.sprite.texture != null)
                        {
                            GUILayout.Label(img.sprite.name);
                            GUILayout.Label(img.color.ToString());
                            try
                            {
                                Color[] newImg = img.sprite.texture.GetPixels((int)img.sprite.textureRect.x, (int)img.sprite.textureRect.y, (int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
                                Texture2D tex = new Texture2D((int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
                                tex.SetPixels(newImg);
                                tex.Apply();
                                GUILayout.Label(tex);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    else if (c is Slider)
                    {
                        Slider b = c as Slider;
                        for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                    }
                    else if (c is Text)
                    {
                        Text text = c as Text;
                        GUILayout.Label(text.text + " " + text.font + " " + text.fontStyle + " " + text.fontSize + " " + text.alignment + " " + text.alignByGeometry + " " + text.resizeTextForBestFit + " " + text.color);
                    }
                    else if (c is RawImage)
                    {
                        GUILayout.Label(((RawImage)c).mainTexture.name);
                        GUILayout.Label(((RawImage)c).color.ToString());
                        GUILayout.Label(((RawImage)c).mainTexture);
                    }
                    else if (c is Renderer)
                        GUILayout.Label(((Renderer)c).material != null ? ((Renderer)c).material.shader.name : "");
                    else if (c is Button)
                    {
                        Button b = c as Button;
                        for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onClick.GetPersistentTarget(i).GetType().FullName + "." + b.onClick.GetPersistentMethodName(i));
                        IList calls = b.onClick.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                        for (int i = 0; i < calls.Count; ++i)
                        {
                            UnityAction unityAction = ((UnityAction)calls[i].GetPrivate("Delegate"));
                            GUILayout.Label(unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                        }
                    }
                    else if (c is Toggle)
                    {
                        Toggle b = c as Toggle;
                        for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                        IList calls = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                        for (int i = 0; i < calls.Count; ++i)
                        {
                            UnityAction<bool> unityAction = ((UnityAction<bool>)calls[i].GetPrivate("Delegate"));
                            GUILayout.Label(unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                        }
                    }
                    else if (c is InputField)
                    {
                        InputField b = c as InputField;
                        if (b.onValueChanged != null)
                        {
                            for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                                GUILayout.Label("OnValueChanged " + b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                            IList calls = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                            for (int i = 0; i < calls.Count; ++i)
                            {
                                UnityAction<string> unityAction = ((UnityAction<string>)calls[i].GetPrivate("Delegate"));
                                GUILayout.Label("OnValueChanged " + unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                            }
                        }
                        if (b.onEndEdit != null)
                        {
                            for (int i = 0; i < b.onEndEdit.GetPersistentEventCount(); ++i)
                                GUILayout.Label("OnEndEdit " + b.onEndEdit.GetPersistentTarget(i).GetType().FullName + "." + b.onEndEdit.GetPersistentMethodName(i));
                            IList calls = b.onEndEdit.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                            for (int i = 0; i < calls.Count; ++i)
                            {
                                UnityAction<string> unityAction = ((UnityAction<string>)calls[i].GetPrivate("Delegate"));
                                GUILayout.Label("OnEndEdit " + unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                            }
                        }
                        if (b.onValidateInput != null)
                            GUILayout.Label("OnValidateInput " + b.onValidateInput.Target.GetType().FullName + "." + b.onValidateInput.Method.Name);
                    }
                    else if (c is RectTransform)
                    {
                        RectTransform rt = c as RectTransform;
                        GUILayout.Label("anchorMin " + rt.anchorMin);
                        GUILayout.Label("anchorMax " + rt.anchorMax);
                        GUILayout.Label("offsetMin " + rt.offsetMin);
                        GUILayout.Label("offsetMax " + rt.offsetMax);
                        GUILayout.Label("rect " + rt.rect);
                        GUILayout.Label("localRotation " + rt.localEulerAngles);
                        GUILayout.Label("localScale " + rt.localScale);
                    }
                    else if (c is Transform)
                    {
                        Transform tr = c as Transform;
                        GUILayout.Label("localPosition " + tr.localPosition);
                        GUILayout.Label("localRotation " + tr.localEulerAngles);
                        GUILayout.Label("localScale " + tr.localScale);
                    }
                    else if (c is UI_OnEnableEvent)
                    {
                        UI_OnEnableEvent e = c as UI_OnEnableEvent;
                        if (e._event != null)
                        {
                            for (int i = 0; i < e._event.GetPersistentEventCount(); ++i)
                                GUILayout.Label("_event " + e._event.GetPersistentTarget(i).GetType().FullName + "." + e._event.GetPersistentMethodName(i));
                            IList calls = e._event.GetPrivateExplicit<UnityEventBase>("m_Calls").GetPrivate("m_RuntimeCalls") as IList;
                            for (int i = 0; i < calls.Count; ++i)
                            {
                                UnityAction<string> unityAction = ((UnityAction<string>)calls[i].GetPrivate("Delegate"));
                                GUILayout.Label("_event " + unityAction.Target.GetType().FullName + "." + unityAction.Method.Name);
                            }

                        }
                    }
                    GUILayout.EndHorizontal();
                    this.RecurseObjects(pair, 1);
                }
            }
            GUILayout.EndScrollView();
            _scroll3 = GUILayout.BeginScrollView(_scroll3, GUI.skin.box, GUILayout.Height(Screen.height / 5f));
            foreach (KeyValuePair<LogType, string> lastlog in _lastlogs)
            {
                Color c = GUI.color;
                switch (lastlog.Key)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        GUI.color = Color.red;
                        break;
                    case LogType.Warning:
                        GUI.color = Color.yellow;
                        break;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label(lastlog.Value);
                GUI.color = c;
                if (GUILayout.Button("Copy to clipboard", GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = lastlog.Value;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear AssetBundle Cache"))
            {
                foreach (KeyValuePair<string, AssetBundleManager.BundlePack> pair in AssetBundleManager.ManifestBundlePack)
                {
                    foreach (KeyValuePair<string, LoadedAssetBundle> bundle in new Dictionary<string, LoadedAssetBundle>(pair.Value.LoadedAssetBundles))
                    {
                        AssetBundleManager.UnloadAssetBundle(bundle.Key, true, pair.Key);
                    }
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear logs", GUILayout.ExpandWidth(false)))
                _lastlogs.Clear();
            if (GUILayout.Button("Open log file", GUILayout.ExpandWidth(false)))
                Process.Start(Path.Combine(Application.dataPath, "output_log.txt"));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void RecurseObjects(ObjectPair obj, int indent)
        {
            if (this._openedObjects.Contains(obj))
            {
                Color c = GUI.backgroundColor;
                GUI.backgroundColor = indent % 2 != 0 ? new Color(0f, 0f, 0f, 0.7f) : new Color(0.35f, 0.35f, 0.35f, 0.7f);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical(_customBoxStyle);
                GUI.backgroundColor = c;
                Type t = obj.child.GetType();
                if (t.GetInterface("IEnumerable") != null)
                {
                    IEnumerable array = obj.child as IEnumerable;
                    int i = 0;
                    if (array != null)
                    {
                        foreach (object o in array)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            GUILayout.Label(i + ": " + (o == null ? "null" : o), GUILayout.ExpandWidth(false));

                            ObjectPair pair = new ObjectPair(obj.child, o);
                            if (o != null)
                            {
                                Type oType = o.GetType();
                                if (oType.IsValueType == false && (oType.BaseType == null || oType.BaseType.IsValueType == false))
                                {
                                    if (GUILayout.Toggle(this._openedObjects.Contains(pair), ""))
                                    {
                                        if (this._openedObjects.Contains(pair) == false)
                                            this._openedObjects.Add(pair);
                                    }
                                    else
                                    {
                                        if (this._openedObjects.Contains(pair))
                                            this._openedObjects.Remove(pair);
                                    }
                                }
                            }

                            GUILayout.EndHorizontal();
                            this.RecurseObjects(pair, indent + 1);
                            ++i;
                        }
                        if (i == 0)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            GUILayout.Label("empty", GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label("null", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    FieldInfo[] fields = t.GetFields(AccessTools.all);
                    foreach (FieldInfo field in fields)
                    {
                        object o = null;
                        bool exception = false;
                        try
                        {
                            o = field.GetValue(obj.child);
                        }
                        catch (Exception)
                        {
                            exception = true;
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        if (o != null)
                            GUILayout.Label(field.Name + ": " + o, GUILayout.ExpandWidth(false));
                        else
                            GUILayout.Label(field.Name + ": " + (exception ? "Exception caught while getting value" : "null"), GUILayout.ExpandWidth(false));

                        ObjectPair pair = new ObjectPair(obj.child, o);
                        if (o != null)
                        {
                            Type oType = o.GetType();
                            if (oType.IsValueType == false && (oType.BaseType == null || oType.BaseType.IsValueType == false))
                            {
                                if (GUILayout.Toggle(this._openedObjects.Contains(pair), ""))
                                {
                                    if (this._openedObjects.Contains(pair) == false)
                                        this._openedObjects.Add(pair);
                                }
                                else
                                {
                                    if (this._openedObjects.Contains(pair))
                                        this._openedObjects.Remove(pair);
                                }
                            }
                        }

                        GUILayout.EndHorizontal();
                        this.RecurseObjects(pair, indent + 1);
                    }
                    PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    Type compilerGeneratedAttribute = typeof(CompilerGeneratedAttribute);
                    foreach (PropertyInfo property in properties)
                    {
                        if ((property.GetGetMethod() ?? property.GetSetMethod()).IsDefined(compilerGeneratedAttribute, false))
                            continue;
                        object o = null;
                        bool exception = false;
                        try
                        {
                            o = property.GetValue(obj.child, null);
                        }
                        catch (Exception)
                        {
                            exception = true;
                        }
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        if (o != null)
                            GUILayout.Label(property.Name + ": " + o, GUILayout.ExpandWidth(false));
                        else
                            GUILayout.Label(property.Name + ": " + (exception ? "Exception caught while getting value" : "null"), GUILayout.ExpandWidth(false));

                        ObjectPair pair = new ObjectPair(obj.child, o);
                        if (o != null)
                        {
                            Type oType = o.GetType();
                            if (oType.IsValueType == false && (oType.BaseType == null || oType.BaseType.IsValueType == false))
                            {
                                if (GUILayout.Toggle(this._openedObjects.Contains(pair), ""))
                                {
                                    if (this._openedObjects.Contains(pair) == false)
                                        this._openedObjects.Add(pair);
                                }
                                else
                                {
                                    if (this._openedObjects.Contains(pair))
                                        this._openedObjects.Remove(pair);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();

                        this.RecurseObjects(pair, indent + 1);
                    }
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }
    }
}

