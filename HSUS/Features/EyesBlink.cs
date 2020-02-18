#if HONEYSELECT
using Harmony;
#elif KOIKATSU || AISHOUJO
using HarmonyLib;
#endif
#if AISHOUJO
using AIChara;
#endif
using System;
using System.Reflection;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;

namespace HSUS.Features
{
    public class EyesBlink : IFeature
    {
        private static bool _eyesBlink = false;
        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
            node = node.FindChildNode("eyesBlink");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _eyesBlink = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
        }

        public void SaveParams(XmlTextWriter writer)
        {
            writer.WriteStartElement("eyesBlink");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_eyesBlink));
            writer.WriteEndElement();
        }

        public void LevelLoaded()
        {
        }

        [HarmonyPatch]
        public static class CharFileInfoStatus_Ctor_Patches
        {
            private static bool Prepare()
            {
                return HSUS._self._binary == Binary.Studio;
            }

            private static MethodBase TargetMethod()
            {
#if HONEYSELECT
                return typeof(CharFileInfoStatus).GetConstructor(new Type[] { });
#elif AISHOUJO || KOIKATSU
                return typeof(ChaFileStatus).GetConstructor(new Type[] { });
#endif
            }

#if HONEYSELECT
            private static void Postfix(CharFileInfoStatus __instance)
#elif KOIKATSU || AISHOUJO
            private static void Postfix(ChaFileStatus __instance)
#endif
            {
                __instance.eyesBlink = _eyesBlink;
            }
        }

    }
}