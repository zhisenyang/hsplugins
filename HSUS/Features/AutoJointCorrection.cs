﻿#if HONEYSELECT
using Harmony;
#elif KOIKATSU || AISHOUJO
using HarmonyLib;
#endif
#if AISHOUJO
using AIChara;
#endif
using System.Reflection;
using System.Xml;
using Studio;
using ToolBox;
using ToolBox.Extensions;

namespace HSUS.Features
{
    public class AutoJointCorrection : IFeature
    {
        private static bool _autoJointCorrection = true;

        public void Awake()
        {
        }

        public void LoadParams(XmlNode node)
        {
            node = node.FindChildNode("autoJointCorrection");
            if (node == null)
                return;
            if (node.Attributes["enabled"] != null)
                _autoJointCorrection = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
        }

        public void SaveParams(XmlTextWriter writer)
        {
            writer.WriteStartElement("autoJointCorrection");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(_autoJointCorrection));
            writer.WriteEndElement();
        }

        public void LevelLoaded()
        {
        }

        [HarmonyPatch]
        public class OICharInfo_Ctor_Patches
        {
            internal static MethodBase TargetMethod()
            {
#if HONEYSELECT
                return typeof(OICharInfo).GetConstructor(new[] { typeof(CharFile), typeof(int) });
#elif KOIKATSU || AISHOUJO
                return typeof(OICharInfo).GetConstructor(new[] { typeof(ChaFileControl), typeof(int) });
#endif
            }

            public static bool Prepare()
            {
                return _autoJointCorrection && HSUS._self._binary == Binary.Studio;
            }

            public static void Postfix(OICharInfo __instance)
            {
                for (int i = 0; i < __instance.expression.Length; i++)
                    __instance.expression[i] = true;
            }
        }

    }
}
