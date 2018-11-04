using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using ToolBox;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HSUS
{
    public class ObjectTreeDebug : MonoBehaviour
    {
        private Transform _target;
        private readonly HashSet<GameObject> _openedObjects = new HashSet<GameObject>();
        private Vector2 _scroll;
        private Vector2 _scroll2;
        private static Vector2 _scroll3;
        private static readonly LinkedList<KeyValuePair<LogType, string>> _lastlogs = new LinkedList<KeyValuePair<LogType, string>>();
        private static bool _debug;
        private Rect _rect = new Rect(Screen.width / 4f, Screen.height / 4f, Screen.width / 2f, Screen.height / 2f);
        private int _randomId;
        private static readonly Process _process;
        private static readonly byte _bits;
        private readonly HashSet<Component> _openedComponents = new HashSet<Component>();
        private static string _goSearch = "";

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
            if (Input.GetKeyDown(HSUS.self.debugShortcut))
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
                        if (GUILayout.Toggle(this._openedObjects.Contains(go), "", GUILayout.ExpandWidth(false)))
                        {
                            if (this._openedObjects.Contains(go) == false)
                                this._openedObjects.Add(go);
                        }
                        else
                        {
                            if (this._openedObjects.Contains(go))
                                this._openedObjects.Remove(go);
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
                            if (this._openedObjects.Contains(t.gameObject) == false)
                                this._openedObjects.Add(t.gameObject);
                            t = t.parent;
                        }
                    }
                }
                GUI.color = c;
                go.SetActive(GUILayout.Toggle(go.activeSelf, "", GUILayout.ExpandWidth(false)));
                GUILayout.EndHorizontal();
            }
            if (_goSearch.Length != 0 || this._openedObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        void OnGUI()
        {
            if (_debug == false)
                return;
            GUI.Box(this._rect, "", GUI.skin.window);
            GUI.Box(this._rect, "", GUI.skin.window);
            GUI.Box(this._rect, "", GUI.skin.window);
            this._rect = GUILayout.Window(this._randomId, this._rect, this.WindowFunc, "Debug Console: " + _process.ProcessName + " | " + _bits + "bits"
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

                    if (GUILayout.Toggle(this._openedComponents.Contains(c), ""))
                    {
                        if (this._openedComponents.Contains(c) == false)
                            this._openedComponents.Add(c);
                    }
                    else
                    {
                        if (this._openedComponents.Contains(c))
                            this._openedComponents.Remove(c);
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

                    if (this._openedComponents.Contains(c))
                    {
                        GUILayout.BeginVertical();
                        FieldInfo[] fields = c.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        foreach (FieldInfo field in fields)
                        {
                            object o = null;
                            try
                            {
                                o = field.GetValue(c);
                            }
                            catch (Exception)
                            {
                            }
                            if (o != null)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUILayout.Label(field.Name + ": " + field.GetValue(c));
                                GUILayout.EndHorizontal();
                            }
                        }
                        PropertyInfo[] properties = c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        foreach (PropertyInfo property in properties)
                        {
                            object o = null;
                            try
                            {
                                o = property.GetValue(c, null);
                            }
                            catch (Exception)
                            {
                            }
                            if (o != null)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(20);
                                GUILayout.Label(property.Name + ": " + o);
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                }
            }
            GUILayout.EndScrollView();
            _scroll3 = GUILayout.BeginScrollView(_scroll3, GUI.skin.box, GUILayout.Height(Screen.height / 4f));
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
                System.Diagnostics.Process.Start(System.IO.Path.Combine(Application.dataPath, "output_log.txt"));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
    }
}

