using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolBox;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class UIScale
    {
        private class CanvasData
        {
            public float scaleFactor;
            public float scaleFactor2;
            public Vector2 referenceResolution;
        }

        private static Dictionary<Canvas, CanvasData> _scaledCanvases = new Dictionary<Canvas, CanvasData>();

        public static void Init()
        {
#if HONEYSELECT
            HSUS._self._routines.ExecuteDelayed(() =>
#elif KOIKATSU
            HSUS._self.ExecuteDelayed(() =>
#endif
                                      {
                                          switch (HSUS._self._binary)
                                          {
                                              case HSUS.Binary.Game:
#if HONEYSELECT
                                                            GameObject go = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSubMenu/W_SubMenu");
                                                            if (go != null)
                                                            {
                                                                RectTransform rt = (RectTransform)go.transform;
                                                                Vector3 cachedPosition = rt.position;
                                                                rt.anchorMax = Vector2.one;
                                                                rt.anchorMin = Vector2.one;
                                                                rt.position = cachedPosition;
                                                            }
                                                            go = GameObject.Find("CustomScene/CustomControl/CustomUI/CustomSystem/W_System");
                                                            if (go != null)
                                                            {
                                                                RectTransform rt = (RectTransform)go.transform;
                                                                Vector3 cachedPosition = rt.position;
                                                                rt.anchorMax = Vector2.zero;
                                                                rt.anchorMin = Vector2.zero;
                                                                rt.position = cachedPosition;
                                                            }
                                                            go = GameObject.Find("CustomScene/CustomControl/CustomUI/ColorMenu/BasePanel");
                                                            if (go != null)
                                                            {
                                                                RectTransform rt = (RectTransform)go.transform;
                                                                Vector3 cachedPosition = rt.position;
                                                                rt.anchorMax = new Vector2(1, 0);
                                                                rt.anchorMin = new Vector2(1, 0);
                                                                rt.position = cachedPosition;
                                                            }
#endif
                                                  break;
                                              case HSUS.Binary.Neo:
#if KOIKATSU
                                                  GameObject go;
#endif
                                                  go = GameObject.Find("StudioScene/Canvas Object List/Image Bar");
                                                  if (go != null)
                                                  {
                                                      RectTransform rt = (RectTransform)go.transform;
                                                      Vector3 cachedPosition = rt.position;
                                                      rt.anchorMax = Vector2.one;
                                                      rt.anchorMin = Vector2.one;
                                                      rt.position = cachedPosition;
                                                  }
                                                  break;
                                          }

                                          foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
                                          {
                                              if (_scaledCanvases.ContainsKey(c) == false && ShouldScaleUI(c))
                                              {
                                                  CanvasScaler cs = c.GetComponent<CanvasScaler>();
                                                  if (cs != null)
                                                  {
                                                      switch (cs.uiScaleMode)
                                                      {
                                                          case CanvasScaler.ScaleMode.ConstantPixelSize:
                                                              _scaledCanvases.Add(c, new CanvasData() {scaleFactor = c.scaleFactor, scaleFactor2 = cs.scaleFactor});
                                                              break;
                                                          case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                                              _scaledCanvases.Add(c, new CanvasData() {scaleFactor = c.scaleFactor, referenceResolution = cs.referenceResolution});
                                                              break;
                                                      }
                                                  }
                                                  else
                                                  {
                                                      _scaledCanvases.Add(c, new CanvasData() {scaleFactor = c.scaleFactor});
                                                  }
                                              }
                                          }
                                          Dictionary<Canvas, CanvasData> newScaledCanvases = new Dictionary<Canvas, CanvasData>();
                                          foreach (KeyValuePair<Canvas, CanvasData> pair in _scaledCanvases)
                                          {
                                              if (pair.Key != null)
                                                  newScaledCanvases.Add(pair.Key, pair.Value);
                                          }
                                          _scaledCanvases = newScaledCanvases;
                                          Do();
                                      }, 10);
        }

        public static void Do()
        {
            float usedScale = HSUS._self._binary == HSUS.Binary.Game ? HSUS._self._gameUIScale : HSUS._self._neoUIScale;
            if (usedScale != 1f) //Fuck you shortcutshsparty
            {
                Type t = Type.GetType("ShortcutsHSParty.DefaultMenuController,ShortcutsHSParty");
                if (t != null)
                {
                    MonoBehaviour component = (MonoBehaviour)UnityEngine.Object.FindObjectOfType(t);
                    if (component != null)
                        component.enabled = false;
                }
            }
            foreach (KeyValuePair<Canvas, CanvasData> pair in _scaledCanvases)
            {
                if (pair.Key != null && ShouldScaleUI(pair.Key))
                {
                    CanvasScaler cs = pair.Key.GetComponent<CanvasScaler>();
                    if (cs != null)
                    {
                        switch (cs.uiScaleMode)
                        {
                            case CanvasScaler.ScaleMode.ConstantPixelSize:
                                cs.scaleFactor = pair.Value.scaleFactor2 * usedScale;
                                break;
                            case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                cs.referenceResolution = pair.Value.referenceResolution / usedScale;
                                break;
                        }
                    }
                    else
                    {
                        pair.Key.scaleFactor = pair.Value.scaleFactor * usedScale;
                    }
                }
            }
        }

        private static bool ShouldScaleUI(Canvas c)
        {
            bool ok = true;
            string path = c.transform.GetPathFrom((Transform)null);
            if (HSUS._self._binary == HSUS.Binary.Neo)
            {
                switch (path)
                {
#if HONEYSELECT
                    case "StartScene/Canvas":
                    case "VectorCanvas":
                    case "New Game Object"://AdjustMod/SkintexMod
#elif KOIKATSU
                    case "SceneLoadScene/Canvas Load":
                    case "SceneLoadScene/Canvas Load Work":
                    case "ExitScene/Canvas":
                    case "NotificationScene/Canvas":
                    case "CheckScene/Canvas":
#endif
                        ok = false;
                        break;
                }
            }
            else
            {
                switch (path)
                {
#if HONEYSELECT
                    case "LogoScene/Canvas":
                    case "LogoScene/Canvas (1)":
                    case "CustomScene/CustomControl/CustomUI/BackGround":
                    case "CustomScene/CustomControl/CustomUI/Fusion":
                    case "GameScene/Canvas":
                    case "MapSelectScene/Canvas":
                    case "SubtitleUserInterface":
                    case "ADVScene/Canvas":
#elif KOIKATSU
                    case "CustomScene/CustomRoot/BackUIGroup/CvsBackground":
                    case "CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCharaName":
                    case "AssetBundleManager/scenemanager/Canvas":
                    case "FreeHScene/Canvas":
                    case "ExitScene":
                    case "CustomScene/CustomRoot/SaveFrame/BackSpCanvas":
                    case "CustomScene/CustomRoot/SaveFrame/FrontSpCanvas":
                    case "CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront":
#endif
                    case "TitleScene/Canvas":
                        ok = false;
                        break;
                }
            }
            Canvas parent = c.GetComponentInParent<Canvas>();
            return ok && c.isRootCanvas && (parent == null || parent == c);
        }
    }
}
