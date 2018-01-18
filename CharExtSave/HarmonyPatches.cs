using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Harmony;

namespace CharExtSave
{
    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("SaveWithoutPNG")]
    [HarmonyPatch(new Type[] {typeof(BinaryWriter)})]
    public class CharFile_SaveWithoutPNG_Patches
    {
        public static bool Prepare()
        {
            return CharExtSave._binary == CharExtSave.Binary.Game;
        }

        public static void Postfix(CharFile __instance, BinaryWriter writer)
        {

            UnityEngine.Debug.Log(CharExtSave.logPrefix + "Saving extended data for character...");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(writer.BaseStream, System.Text.Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.None;

                xmlWriter.WriteStartElement("charExtData");
                foreach (KeyValuePair<string, CharExtSave.HandlerPair> kvp in CharExtSave._handlers)
                {
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        using (XmlTextWriter xmlWriter2 = new XmlTextWriter(stringWriter))
                        {
                            try
                            {
                                xmlWriter2.WriteStartElement(kvp.Key);
                                kvp.Value.onWrite(__instance, xmlWriter2);
                                xmlWriter2.WriteEndElement();

                                xmlWriter.WriteRaw(stringWriter.ToString());
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError(CharExtSave.logPrefix + "Exception happened in handler \"" + kvp.Key + "\" during character saving. The exception was: " + e);
                            }
                        }

                    }
                }
                xmlWriter.WriteEndElement();
            }
            UnityEngine.Debug.Log(CharExtSave.logPrefix + "Saving done.");
        }
    }

    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("Load")]
    [HarmonyPatch(new[] {typeof(BinaryReader), typeof(Boolean), typeof(Boolean)})]
    public class CharFile_Load_Patches
    {
        public static void Postfix(CharFile __instance, BinaryReader reader, Boolean noSetPNG, Boolean noLoadStatus)
        {
            UnityEngine.Debug.Log(CharExtSave.logPrefix + "Loading extended data for character...");
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
                                    CharExtSave.HandlerPair pair;
                                    if (CharExtSave._handlers.TryGetValue(child.Name, out pair))
                                        pair.onRead(__instance, child);
                                }
                                catch (Exception e)
                                {
                                    UnityEngine.Debug.LogError(CharExtSave.logPrefix + "Exception happened in handler \"" + child.Name + "\" during character loading. The exception was: " + e);
                                }
                            }
                            break;
                    }
                }
            }
            catch (XmlException)
            {
                UnityEngine.Debug.Log(CharExtSave.logPrefix + "No ext data in reader.");
                foreach (KeyValuePair<string, CharExtSave.HandlerPair> kvp in CharExtSave._handlers)
                    kvp.Value.onRead(__instance, null);
                reader.BaseStream.Seek(cachedPosition, SeekOrigin.Begin);
            }
        }
    }
}