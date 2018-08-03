#if HONEYSELECT
using System;
using System.Collections.Generic;
using IllusionPlugin;
using UnityEngine;
#elif KOIKATSU
using BepInEx;
using UnityEngine.SceneManagement;
#endif
using Harmony;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Instrumentation
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.lightingeditor", Name: "Instrumentation", Version: Instrumentation.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
//  [BepInProcess("CharaStudio")]
#endif
    public class Instrumentation :
#if HONEYSELECT
        IEnhancedPlugin
#elif KOIKATSU
    BaseUnityPlugin
#endif
    {
        public static Dictionary<int, Dictionary<Type, ulong>> times = new Dictionary<int, Dictionary<Type, ulong>>();
        public static Dictionary<int, Dictionary<Type, ulong>> times2 = new Dictionary<int, Dictionary<Type, ulong>>();
        public static Dictionary<int, Dictionary<Type, ulong>> times3 = new Dictionary<int, Dictionary<Type, ulong>>();
        public static Dictionary<int, Dictionary<Type, ulong>> times4 = new Dictionary<int, Dictionary<Type, ulong>>();
        public static Dictionary<Type, Stopwatch> _stopwatches = new Dictionary<Type, Stopwatch>();

        public const string versionNum = "1.0.0";

#if HONEYSELECT
        public string Name { get { return "Instrumentation"; } }
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
#elif KOIKATSU
        void Awake()
        {
            SceneManager.sceneLoaded += this.SceneLoaded;
            this.OnApplicationStart();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            this.OnLevelWasLoaded(scene.buildIndex);
        }

        void Update()
        {
            this.OnUpdate();
        }
#endif
        public void OnApplicationStart()
        {

        }

        public void OnApplicationQuit()
        {

        }

        public void OnLevelWasLoaded(int level)
        {
            //if (level == 3)
            //{
            //    HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.instrumentation");
            //    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            //    {
            //        try
            //        {
            //            foreach (Type type in assembly.GetTypes())
            //            {
            //                MethodInfo info;
            //                try
            //                {

            //                    if ((info = type.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) != null && info.GetParameters().Length == 0 && info.ReturnType == typeof(void))
            //                    {
            //                        harmony.Patch(
            //                                      info,
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.UpdatePrefixes), BindingFlags.Public | BindingFlags.Static)),
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.UpdatePostfixes), BindingFlags.Public | BindingFlags.Static))
            //                                     );
            //                    }
            //                }
            //                catch (Exception e)
            //                {
            //                    UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
            //                }
            //                try
            //                {
            //                    if ((info = type.GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) != null && info.GetParameters().Length == 0 && info.ReturnType == typeof(void))
            //                        harmony.Patch(
            //                                      info,
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.UpdatePrefixes), BindingFlags.Public | BindingFlags.Static)),
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.LateUpdatePostfixes), BindingFlags.Public | BindingFlags.Static))
            //                                     );
            //                }
            //                catch (Exception e)
            //                {
            //                    UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
            //                }
            //                try
            //                {
            //                    if ((info = type.GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) != null && info.GetParameters().Length == 0 && info.ReturnType == typeof(void))
            //                        harmony.Patch(
            //                                      info,
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.UpdatePrefixes), BindingFlags.Public | BindingFlags.Static)),
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.FixedUpdatePostfixes), BindingFlags.Public | BindingFlags.Static))
            //                                     );
            //                }
            //                catch (Exception e)
            //                {
            //                    UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
            //                }
            //                try
            //                {
            //                    if ((info = type.GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) != null && info.GetParameters().Length == 0 && info.ReturnType == typeof(void))
            //                        harmony.Patch(
            //                                      info,
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.UpdatePrefixes), BindingFlags.Public | BindingFlags.Static)),
            //                                      new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.OnGUIPostfixes), BindingFlags.Public | BindingFlags.Static))
            //                                     );
            //                }
            //                catch (Exception e)
            //                {
            //                    UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
            //                }
            //            }

            //        }
            //        catch (Exception e)
            //        {
            //            UnityEngine.Debug.LogError("Instrumentation: Exception occured when patching: " + e.ToString());
            //        }
            //    }
            //}
        }

        public void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                UnityEngine.Debug.Log((Time.frameCount - 1) + " " + (Time.frameCount - 60));
                Dictionary<Type, ulong> avgDic = new Dictionary<Type, ulong>();
                Dictionary<Type, ulong> avgDic2 = new Dictionary<Type, ulong>();
                Dictionary<Type, ulong> avgDic3 = new Dictionary<Type, ulong>();
                Dictionary<Type, ulong> avgDic4 = new Dictionary<Type, ulong>();
                for (int i = 1; i < 60; i++)
                {
                    Dictionary<Type, ulong> dic;
                    if (times.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                        {
                            if (avgDic.ContainsKey(pair.Key) == false)
                                avgDic.Add(pair.Key, 0);
                            avgDic[pair.Key] += pair.Value;
                        }
                    }
                    if (times2.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                        {
                            if (avgDic2.ContainsKey(pair.Key) == false)
                                avgDic2.Add(pair.Key, 0);
                            avgDic2[pair.Key] += pair.Value;
                        }
                    }
                    if (times3.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                        {
                            if (avgDic3.ContainsKey(pair.Key) == false)
                                avgDic3.Add(pair.Key, 0);
                            avgDic3[pair.Key] += pair.Value;
                        }
                    }
                    if (times4.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                        {
                            if (avgDic4.ContainsKey(pair.Key) == false)
                                avgDic4.Add(pair.Key, 0);
                            avgDic4[pair.Key] += pair.Value;
                        }
                    }
                }
                StringBuilder sb = new StringBuilder();
                List<KeyValuePair<Type, ulong>> list = avgDic.ToList();
                list.Sort((a, b) => b.Value.CompareTo(a.Value));
                foreach (KeyValuePair<Type, ulong> pair in list)
                {
                    sb.AppendLine(pair.Key.Name + ".Update," + (pair.Value / 59));
                }
                list = avgDic2.ToList();
                list.Sort((a, b) => b.Value.CompareTo(a.Value));
                foreach (KeyValuePair<Type, ulong> pair in list)
                {
                    sb.AppendLine(pair.Key.Name + ".LateUpdate," + (pair.Value / 59));
                }
                list = avgDic3.ToList();
                list.Sort((a, b) => b.Value.CompareTo(a.Value));
                foreach (KeyValuePair<Type, ulong> pair in list)
                {
                    sb.AppendLine(pair.Key.Name + ".FixedUpdate," + (pair.Value / 59));
                }
                list = avgDic4.ToList();
                list.Sort((a, b) => b.Value.CompareTo(a.Value));
                foreach (KeyValuePair<Type, ulong> pair in list)
                {
                    sb.AppendLine(pair.Key.Name + ".OnGUI," + (pair.Value / 59));
                }
                UnityEngine.Debug.Log(sb.ToString());
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < 300; i++)
                {
                    sb.AppendLine((Time.frameCount - i).ToString());
                    Dictionary<Type, ulong> dic;
                    if (times.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                            sb.AppendLine(pair.Key.Name + ".Update," + pair.Value);
                    }
                    if (times2.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                            sb.AppendLine(pair.Key.Name + ".LateUpdate," + pair.Value);
                    }
                    if (times3.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                            sb.AppendLine(pair.Key.Name + ".FixedUpdate," + pair.Value);
                    }
                    if (times4.TryGetValue(Time.frameCount - i, out dic))
                    {
                        foreach (KeyValuePair<Type, ulong> pair in dic)
                            sb.AppendLine(pair.Key.Name + ".OnGUI," + pair.Value);
                    }
                }
                System.IO.File.WriteAllText("FrameSample.txt", sb.ToString());
            }
            if (times.ContainsKey(Time.frameCount - 301))
                times.Remove(Time.frameCount - 301);
            if (times2.ContainsKey(Time.frameCount - 301))
                times2.Remove(Time.frameCount - 301);
            if (times3.ContainsKey(Time.frameCount - 301))
                times3.Remove(Time.frameCount - 301);
            if (times4.ContainsKey(Time.frameCount - 301))
                times4.Remove(Time.frameCount - 301);
        }
    }


    public static class Patches
    {
        public static void UpdatePrefixes(object __instance)
        {
            Type t = __instance.GetType();
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            watch.Reset();
            watch.Start();
        }


        public static void UpdatePostfixes(object __instance)
        {
            Type t = __instance.GetType();
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            Dictionary<Type, ulong> frameDic;
            if (Instrumentation.times.TryGetValue(Time.frameCount, out frameDic) == false)
            {
                frameDic = new Dictionary<Type, ulong>();
                Instrumentation.times.Add(Time.frameCount, frameDic);
            }
            if (frameDic.ContainsKey(t) == false)
                frameDic.Add(t, 0);
            frameDic[t] += (ulong)watch.ElapsedTicks;
        }


        public static void LateUpdatePostfixes(object __instance)
        {
            Type t = __instance.GetType();
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            Dictionary<Type, ulong> frameDic;
            if (Instrumentation.times2.TryGetValue(Time.frameCount, out frameDic) == false)
            {
                frameDic = new Dictionary<Type, ulong>();
                Instrumentation.times2.Add(Time.frameCount, frameDic);
            }
            if (frameDic.ContainsKey(t) == false)
                frameDic.Add(t, 0);
            frameDic[t] += (ulong)watch.ElapsedTicks;
        }


        public static void FixedUpdatePostfixes(object __instance)
        {
            Type t = __instance.GetType();
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            Dictionary<Type, ulong> frameDic;
            if (Instrumentation.times3.TryGetValue(Time.frameCount, out frameDic) == false)
            {
                frameDic = new Dictionary<Type, ulong>();
                Instrumentation.times3.Add(Time.frameCount, frameDic);
            }
            if (frameDic.ContainsKey(t) == false)
                frameDic.Add(t, 0);
            frameDic[t] += (ulong)watch.ElapsedTicks;
        }

        public static void OnGUIPostfixes(object __instance)
        {
            Type t = __instance.GetType();
            if (Instrumentation._stopwatches.TryGetValue(t, out Stopwatch watch) == false)
            {
                watch = new Stopwatch();
                Instrumentation._stopwatches.Add(t, watch);
            }
            Dictionary<Type, ulong> frameDic;
            if (Instrumentation.times4.TryGetValue(Time.frameCount, out frameDic) == false)
            {
                frameDic = new Dictionary<Type, ulong>();
                Instrumentation.times4.Add(Time.frameCount, frameDic);
            }
            if (frameDic.ContainsKey(t) == false)
                frameDic.Add(t, 0);
            frameDic[t] += (ulong)watch.ElapsedTicks;
        }
    }
}
