﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace HSPE
{
    public class ObjectTreeDebug : MonoBehaviour
    {
        private Transform _target = null;
        private readonly HashSet<GameObject> _openedObjects = new HashSet<GameObject>();
        private Vector2 _scroll;
        private Vector2 _scroll2;
        private readonly LinkedList<string> _lastlogs = new LinkedList<string>();
        private Dictionary<Sprite, Texture2D> tex2D = new Dictionary<Sprite, Texture2D>();
        private bool _debug = false;

        void Awake()
        {
            //foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            //{
            //    try
            //    {
            //        Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
            //        Texture2D newTex = new Texture2D((int)sprite.textureRect.width, (int)sprite.textureRect.height);
            //        newTex.SetPixels(pixels);
            //        newTex.Apply();
            //        this.tex2D.Add(sprite, newTex);

            //    }
            //    catch (Exception)
            //    {
            //        this.tex2D.Add(sprite, sprite.texture);
            //    }
            //}
            //this.StartCoroutine(this.LoadAssetsBundle());
        }

        void OnEnable()
        {
            Application.logMessageReceived += this.HandleLog;
            //foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
            //{
            //    if (shader != null)
            //        UnityEngine.Debug.Log(shader.name);
            //}
        }

        private IEnumerator LoadAssetsBundle()
        {
            StringBuilder bd = new StringBuilder();
            //string[] files = Directory.GetFiles("abdata", "*.unity3d", SearchOption.AllDirectories);
            List<string> files = CommonLib.GetAssetBundleNameListFromPath("studioneo/info/", true);
            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                UnityEngine.Debug.Log(i + "/" + files.Count + " " + (i / (float)files.Count) + " " + file);
                //if (file.StartsWith("abdata/battle") || file.StartsWith("abdata/h"))
                //    continue;
                AssetBundleCreateRequest abcr;
                abcr = AssetBundle.LoadFromFileAsync("abdata/" + file);
                if (abcr != null)
                {
                    yield return abcr;
                    AssetBundleRequest abr = abcr.assetBundle.LoadAllAssetsAsync<Shader>();
                    if (abr != null)
                    {
                        yield return abr;
                        foreach (Shader shader in abr.allAssets)
                        {
                            if (shader != null)
                                bd.Append(shader.name + "\n");
                        }
                    }
                    abcr.assetBundle.Unload(false);
                }
            }
            UnityEngine.Debug.Log("shaders\n" + bd);
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
            if (this._lastlogs.Count == 11)
                this._lastlogs.RemoveFirst();
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
                GUILayout.Label(n);
                foreach (Component c in this._target.GetComponents<Component>())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(c.GetType().FullName);

                    if (c is Image)
                    {
                        Image img = c as Image;
                        if (img.sprite != null && img.sprite.texture != null)
                        {
                            Color[] newImg = img.sprite.texture.GetPixels((int)img.sprite.textureRect.x, (int)img.sprite.textureRect.y, (int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
                            Texture2D tex = new Texture2D((int)img.sprite.textureRect.width, (int)img.sprite.textureRect.height);
                            tex.SetPixels(newImg);
                            tex.Apply();
                            GUILayout.Label(img.sprite.name);
                            GUILayout.Label(tex);
                        }
                    }
                    else if (c is RawImage)
                        GUILayout.Label(((RawImage)c).mainTexture);
                    else if (c is Renderer)
                        GUILayout.Label(((Renderer)c).material != null ? ((Renderer)c).material.shader.name : "");
                    else if (c is Button)
                    {
                        Button b = c as Button;
                        for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                            GUILayout.Label(b.onClick.GetPersistentMethodName(i));
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            foreach (string lastlog in this._lastlogs)
            {
                GUILayout.Label(lastlog);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
