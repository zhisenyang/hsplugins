using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public class ObjectTreeDebug : MonoBehaviour
    {
        private Transform _target;
        private readonly HashSet<GameObject> _openedObjects = new HashSet<GameObject>();
        private Vector2 _scroll;
        private Vector2 _scroll2;
        private Vector2 _scroll3;
        private readonly LinkedList<string> _lastlogs = new LinkedList<string>();
        private bool _debug;

        void OnEnable()
        {
            Application.logMessageReceived += this.HandleLog;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightControl))
                this._debug = !this._debug;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= this.HandleLog;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            this._lastlogs.AddLast(type + " " + condition);
            if (this._lastlogs.Count == 101)
                this._lastlogs.RemoveFirst();
            this._scroll3.y += 999999;
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            Color c = GUI.color;
            if (this._target == go.transform)
                GUI.color = Color.cyan;
            GUILayout.BeginHorizontal();
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
            if (GUILayout.Button(go.name, GUILayout.ExpandWidth(false)))
            {
                this._target = go.transform;
            }
            GUI.color = c;
            go.SetActive(GUILayout.Toggle(go.activeSelf, "", GUILayout.ExpandWidth(false)));
            GUILayout.EndHorizontal();
            if (this._openedObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        void OnGUI()
        {
            if (this._debug == false)
                return;
            GUILayout.BeginArea(new Rect(Screen.width / 4f, Screen.height / 4f, Screen.width / 2f, Screen.height / 2f));
            GUILayout.BeginHorizontal();
            this._scroll = GUILayout.BeginScrollView(this._scroll, GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.MinWidth(300));
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.parent == null)
                    this.DisplayObjectTree(t.gameObject, 0);
            GUILayout.EndScrollView();
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
                        GUILayout.Label(c.GetType().FullName);

                    if (c is Image)
                    {
                        Image img = c as Image;
                        if (img.sprite != null && img.sprite.texture != null)
                        {
                            try
                            {
                                GUILayout.Label(img.sprite.name);
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
                        GUILayout.Label(((RawImage)c).mainTexture);
                    else if (c is Renderer)
                        GUILayout.Label(((Renderer)c).material != null ? ((Renderer)c).material.shader.name : "");
                    else if (c is Button)
                    {
                        Button b = c as Button;
                        for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onClick.GetPersistentTarget(i).GetType().FullName + "." + b.onClick.GetPersistentMethodName(i));
                    }
                    else if (c is Toggle)
                    {
                        Toggle b = c as Toggle;
                        for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onValueChanged.GetPersistentTarget(i).GetType().FullName + "." + b.onValueChanged.GetPersistentMethodName(i));
                    }
                    else if (c is RectTransform)
                    {
                        RectTransform rt = c as RectTransform;
                        GUILayout.Label("anchorMin " + rt.anchorMin);
                        GUILayout.Label("anchorMax " + rt.anchorMax);
                        GUILayout.Label("offsetMin " + rt.offsetMin);
                        GUILayout.Label("offsetMax " + rt.offsetMax);
                        GUILayout.Label("sizeDelta " + rt.sizeDelta);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            this._scroll3 = GUILayout.BeginScrollView(this._scroll3, GUI.skin.box, GUILayout.Height(Screen.height / 4f));
            foreach (string lastlog in this._lastlogs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(lastlog);
                if (GUILayout.Button("Copy to clipboard", GUILayout.ExpandWidth(false)))
                    GUIUtility.systemCopyBuffer = lastlog;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear logs", GUILayout.ExpandWidth(false)))
                this._lastlogs.Clear();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open log file", GUILayout.ExpandWidth(false)))
                System.Diagnostics.Process.Start(System.IO.Path.Combine(Application.dataPath, "output_log.txt"));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}

