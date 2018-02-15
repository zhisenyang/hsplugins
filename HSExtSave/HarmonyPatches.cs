﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Harmony;
using Studio;

namespace HSExtSave
{
    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("SaveWithoutPNG")]
    [HarmonyPatch(new[] {typeof(BinaryWriter)})]
    public class CharFile_SaveWithoutPNG_Patches
    {
        public static bool Prepare()
        {
            return HSExtSave._binary == HSExtSave.Binary.Game;
        }

        public static void Postfix(CharFile __instance, BinaryWriter writer)
        {

            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Saving extended data for character...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(writer.BaseStream, System.Text.Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("charExtData");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                {
                    if (kvp.Value.onCharWrite == null)
                        continue;
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onCharWrite(__instance, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during character saving. The exception was: " + e);
                            }
                        }
                    }
                }
                xmlWriter.WriteEndElement();
            }
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Saving done.");
        }
    }

    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("Load")]
    [HarmonyPatch(new[] {typeof(BinaryReader), typeof(Boolean), typeof(Boolean)})]
    public class CharFile_Load_Patches
    {
        public static void Postfix(CharFile __instance, BinaryReader reader, Boolean noSetPNG, Boolean noLoadStatus)
        {
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Loading extended data for character...");
            long cachedPosition = reader.BaseStream.Position;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader.BaseStream);

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "charExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onCharRead != null)
                                        group.onCharRead(__instance, child);
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + child.Name + "\" during character loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(HSExtSave.logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onCharRead != null)
                        kvp.Value.onCharRead(__instance, null);
                reader.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }
    }

    public class SceneInfo_Load_Patches
    {
        public static void ManualPatch(HarmonyInstance instance)
        {
            foreach (MethodInfo methodInfo in typeof(SceneInfo).GetMethods())
            {
                if (methodInfo.Name.Equals("Load") && methodInfo.GetParameters().Length == 2)
                {
                    instance.Patch(methodInfo, null, null, new HarmonyMethod(typeof(SceneInfo_Load_Patches).GetMethod(nameof(MyTranspiler))));
                    break;
                }
            }
        }

        public static IEnumerable<CodeInstruction> MyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Stind_Ref && instructionsList[i + 1].opcode == OpCodes.Leave)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(SceneInfo_Load_Patches).GetMethod(nameof(Injected)));
                    set = true;
                }
            }
        }

        public static void Injected(string path, FileStream stream)
        {
            stream.Read(new byte[12], 0, 12);
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Loading extended data for scene...");
            long cachedPosition = stream.Position;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "sceneExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onSceneReadLoad != null)
                                        group.onSceneReadLoad(path, child);
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + child.Name + "\" during scene loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(HSExtSave.logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onSceneReadLoad != null)
                        kvp.Value.onSceneReadLoad(path, null);
                stream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }
    }

    [HarmonyPatch(typeof(SceneInfo), "Import", new []{typeof(string)})]
    public class SceneInfo_Import_Patches
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Call && instructionsList[i + 1].opcode == OpCodes.Leave)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(SceneInfo_Import_Patches).GetMethod(nameof(Injected)));
                    set = true;
                }
            }
        }

        public static void Injected(string path, FileStream stream)
        {
            stream.Read(new byte[12], 0, 12);
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Loading extended data for scene...");
            long cachedPosition = stream.Position;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "sceneExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onSceneReadImport != null)
                                        group.onSceneReadImport(path, child);
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + child.Name + "\" during scene import. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(HSExtSave.logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onSceneReadImport != null)
                        kvp.Value.onSceneReadImport(path, null);
                stream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }
    }

    [HarmonyPatch(typeof(SceneInfo), "Save", new[] { typeof(string) })]
    public class SceneInfo_Save_Patches
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Callvirt && instructionsList[i + 1].opcode == OpCodes.Leave)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(SceneInfo_Save_Patches).GetMethod(nameof(Injected)));
                    set = true;
                }
            }
        }

        public static void Injected(string path, FileStream stream)
        {

            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Saving extended data for scene...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stream, System.Text.Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("sceneExtData");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                {
                    if (kvp.Value.onSceneWrite == null)
                        continue;
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onSceneWrite(path, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during scene saving. The exception was: " + e);
                            }
                        }
                    }
                }
                xmlWriter.WriteEndElement();
            }
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Saving done.");
        }
    }


    [HarmonyPatch(typeof(CharFileInfoClothesFemale), "LoadSub", new[] { typeof(BinaryReader), typeof(int), typeof(int) })]
    public class CharFileInfoClothesFemale_LoadSub_Patches
    {
        public static void Postfix(CharFileInfoClothesFemale __instance, BinaryReader br, int clothesVer, int colorVer)
        {
            CharFileInfoClothes_Extensions.Load(__instance, br);
        }
    }


    [HarmonyPatch(typeof(CharFileInfoClothesMale), "LoadSub", new[] { typeof(BinaryReader), typeof(int), typeof(int) })]
    public class CharFileInfoClothesMale_LoadSub_Patches
    {
        public static void Postfix(CharFileInfoClothesMale __instance, BinaryReader br, int clothesVer, int colorVer)
        {
            CharFileInfoClothes_Extensions.Load(__instance, br);
        }
    }


    [HarmonyPatch(typeof(CharFileInfoClothes), "Save", new[] { typeof(string) })]
    public class CharFileInfoClothesMale_Save_Patches
    {
        public static bool Prepare()
        {
            return HSExtSave._binary == HSExtSave.Binary.Game;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.opcode == OpCodes.Call && instructionsList[i + 1].opcode == OpCodes.Stloc_S)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, typeof(CharFileInfoClothesMale_Save_Patches).GetMethod(nameof(Injected)));
                    set = true;
                }
            }
        }

        public static void Injected(CharFileInfoClothes __instance, BinaryWriter bw)
        {
            CharFileInfoClothes_Extensions.Save(__instance, bw);
        }
    }

    public class CharFileInfoClothes_Extensions
    {
        public static void Load(CharFileInfoClothes __instance, BinaryReader br)
        {
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Loading extended data for coordinate...");
            long cachedPosition = br.BaseStream.Position;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(br.BaseStream);

                foreach (XmlNode node in doc.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "clothesExtData":
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try
                                {
                                    HSExtSave.HandlerGroup group;
                                    if (HSExtSave._handlers.TryGetValue(child.Name, out group) && group.onClothesRead != null)
                                        group.onClothesRead(__instance, child);
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + child.Name + "\" during coordinate loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(HSExtSave.logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                    if (kvp.Value.onClothesRead != null)
                        kvp.Value.onClothesRead(__instance, null);
                br.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }

        public static void Save(CharFileInfoClothes __instance, BinaryWriter bw)
        {
            StackTrace stackTrace = new StackTrace();

            // Get calling method name
            Console.WriteLine("mabite " + stackTrace.GetFrame(1).GetMethod().Name);

            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Saving extended data for coordinates...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(bw.BaseStream, System.Text.Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("clothesExtData");
                foreach (KeyValuePair<string, HSExtSave.HandlerGroup> kvp in HSExtSave._handlers)
                {
                    if (kvp.Value.onClothesWrite == null)
                        continue;
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onClothesWrite(__instance, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError(HSExtSave.logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during coordinates saving. The exception was: " + e);
                            }
                        }
                    }
                }
                xmlWriter.WriteEndElement();
            }
            UnityEngine.Debug.Log(HSExtSave.logPrefix + "Saving done.");
        }
    }

}