﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
#if HONEYSELECT
using IllusionPlugin;
#elif KOIKATSU
using ExtensibleSaveFormat;
#endif
using Harmony;
using RendererEditor.Targets;
using Studio;
using UnityEngine;
using ToolBox;
using Vectrosity;

namespace RendererEditor
{
    public class MainWindow : MonoBehaviour
    {
        #region Private Types
        private class TextureWrapper
        {
            public class TextureSettings
            {
                public enum TransparentBorderColor
                {
                    Black,
                    White
                }

                public bool bypassSRGBSampling = true;
                public FilterMode filterMode = FilterMode.Bilinear;
                public int anisoLevel = 1;
                public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
                public bool transparentBorder = false;
                public TransparentBorderColor transparentBorderColor = TransparentBorderColor.Black;
                public bool compressed = true;
                //public int maxWidth = 2048;
                //public int maxHeight = 2048;

                public TextureSettings() { }

                public TextureSettings(TextureSettings other)
                {
                    this.bypassSRGBSampling = other.bypassSRGBSampling;
                    this.filterMode = other.filterMode;
                    this.anisoLevel = other.anisoLevel;
                    this.wrapMode = other.wrapMode;
                    this.transparentBorder = other.transparentBorder;
                    this.transparentBorderColor = other.transparentBorderColor;
                    this.compressed = other.compressed;
                    //this.maxWidth = other.maxWidth;
                    //this.maxHeight = other.maxHeight;
                }
                
                public static TextureSettings Load(string texturePath)
                {
                    string settingsFile = texturePath + ".settings.xml";
                    XmlDocument doc = new XmlDocument();
                    doc.Load(settingsFile);
                    return Load(doc.FirstChild);
                }

                public static TextureSettings Load(XmlNode node)
                {
                    TextureSettings settings = new TextureSettings();
                    if (node.Attributes["bypassSRGBSampling"] != null)
                        settings.bypassSRGBSampling = XmlConvert.ToBoolean(node.Attributes["bypassSRGBSampling"].Value);
                    if (node.Attributes["filterMode"] != null)
                        settings.filterMode = (FilterMode)XmlConvert.ToInt32(node.Attributes["filterMode"].Value);
                    if (node.Attributes["anisoLevel"] != null)
                        settings.anisoLevel = XmlConvert.ToInt32(node.Attributes["anisoLevel"].Value);
                    if (node.Attributes["wrapMode"] != null)
                        settings.wrapMode = (TextureWrapMode)XmlConvert.ToInt32(node.Attributes["wrapMode"].Value);
                    if (node.Attributes["transparentBorder"] != null)
                        settings.transparentBorder = XmlConvert.ToBoolean(node.Attributes["transparentBorder"].Value);
                    if (node.Attributes["transparentBorderColor"] != null)
                        settings.transparentBorderColor = (TransparentBorderColor)XmlConvert.ToInt32(node.Attributes["transparentBorderColor"].Value);
                    if (node.Attributes["compressed"] != null)
                        settings.compressed = XmlConvert.ToBoolean(node.Attributes["compressed"].Value);
                    //if (node.Attributes["maxWidth"] != null)
                    //    settings.maxWidth = XmlConvert.ToInt32(node.Attributes["maxWidth"].Value);
                    //if (node.Attributes["maxHeight"] != null)
                    //    settings.maxHeight = XmlConvert.ToInt32(node.Attributes["maxHeight"].Value);
                    return settings;
                }

                public static void Save(string texturePath, TextureSettings settings)
                {
                    string settingsFile = texturePath + ".settings.xml";

                    if (File.Exists(settingsFile))
                        File.SetAttributes(settingsFile, FileAttributes.Normal);

                    using (XmlTextWriter xmlWriter = new XmlTextWriter(settingsFile, Encoding.UTF8))
                    {
                        xmlWriter.WriteStartElement("root");
                        Save(xmlWriter, settings);
                        xmlWriter.WriteEndElement();
                    }
                    File.SetAttributes(settingsFile, FileAttributes.Hidden);
                }

                public static void Save(XmlTextWriter xmlWriter, TextureSettings settings)
                {
                    xmlWriter.WriteAttributeString("bypassSRGBSampling", XmlConvert.ToString(settings.bypassSRGBSampling));
                    xmlWriter.WriteAttributeString("filterMode", XmlConvert.ToString((int)settings.filterMode));
                    xmlWriter.WriteAttributeString("anisoLevel", XmlConvert.ToString(settings.anisoLevel));
                    xmlWriter.WriteAttributeString("wrapMode", XmlConvert.ToString((int)settings.wrapMode));
                    xmlWriter.WriteAttributeString("transparentBorder", XmlConvert.ToString(settings.transparentBorder));
                    xmlWriter.WriteAttributeString("transparentBorderColor", XmlConvert.ToString((int)settings.transparentBorderColor));
                    xmlWriter.WriteAttributeString("compressed", XmlConvert.ToString(settings.compressed));
                    //xmlWriter.WriteAttributeString("maxWidth", XmlConvert.ToString(settings.maxWidth));
                    //xmlWriter.WriteAttributeString("maxHeight", XmlConvert.ToString(settings.maxHeight));
                }
            }

            public string path;
            public Texture2D texture;
            public TextureSettings settings;
        }

        private class ShaderProperty
        {
            public enum Type
            {
                Color,
                Texture,
                Float,
                Boolean,
                Enum,
                Vector4
            }

            public string name;
            public Type type;

            public bool hasFloatRange;
            public Vector2 floatRange = new Vector2(0f, 1f);
            public Dictionary<int, string> enumValues;
            public int enumColumns;
        }

        private class MaterialInfo
        {
            public ITarget target;
            public int index;
        }

        private class TreeNodeData
        {
            public HashSet<ITarget> selectedTargets = new HashSet<ITarget>();
            public Dictionary<Material, MaterialInfo> selectedMaterials = new Dictionary<Material, MaterialInfo>();
            public Vector2 targetsScroll;
            public Vector2 materialsScroll;
            public Vector2 propertiesScroll;
            public bool onlyActive = false;
            public bool onlyDirty = false;

        }
        #endregion

        #region Private Variables
#if HONEYSELECT
        private const string _texturesDir = "Plugins\\RendererEditor\\Textures";
#elif KOIKATSU
        private const string _texturesDir = "BepInEx\\RendererEditor\\Textures";
#endif
        private const string _dumpDir = _texturesDir + "\\Dump\\";
#if KOIKATSU
        private const string _extSaveKey = "rendererEditor";
#endif
        private static MainWindow _self;

        private readonly List<ShaderProperty> _shaderProperties = new List<ShaderProperty>()
        {
            //Color
            new ShaderProperty() {name = "_Color", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color_2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color_3", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color_4", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_BaseColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_DiffuseColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_GlassColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor_2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor_3", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor_4", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_EmissionColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_ReflectionColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecularColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_TintColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_EmisColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_FuzzColor", type = ShaderProperty.Type.Color},
            //Textures
            new ShaderProperty() {name = "_MainTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_Albedo2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_SpecGlossMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_SpecGlossMap2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_Tangent", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_BumpMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_BumpMap2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_OcclusionMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_SecondaryOcclusionMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_BlendNormalMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormal", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap_2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap_3", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap_4", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailMask", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_Colormask", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_EffectMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_OverTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_MetallicGlossMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ParallaxMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_Transmission", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_EmissionMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailAlbedoMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailAlbedoMap_2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailAlbedoMap_3", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailAlbedoMap_4", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ReflectionTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ShoreTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DiffuseMapSpecA", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_NormalMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_Illum", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DecalTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_NoiseTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_FuzzTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ShadowTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_FalloffTex", type = ShaderProperty.Type.Texture},
            //Float
            new ShaderProperty() {name = "_Metallic", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Smoothness", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_OcclusionStrength", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_BlendNormalMapScale", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale_2", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale_3", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale_4", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Cutoff", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Occlusion", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_RimPower", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Refraction", floatRange = new Vector2(0, 9), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_EffectContrast", floatRange = new Vector2(0, 9), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Effect2Power", floatRange = new Vector2(0, 9), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ColorReverse", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SpecReverse", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SmoothTuning", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_LacquerReflection", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_LacquerSmoothness", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Glossiness", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Gloss", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_GlossAniso", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Anisotropy", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_AnisotropyRGContrast", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailIntensity", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_FuzzRange", floatRange = new Vector2(1, 5), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_FuzzBias", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_WrapDiffuse", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_BumpScale", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Parallax", floatRange = new Vector2(0f, 0.08f), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_FresnelScale", floatRange = new Vector2(0.15f, 4), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_GerstnerIntensity", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Shininess", floatRange = new Vector2(2, 500), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_InvFade", floatRange = new Vector2(0.01f, 3.0f), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SpecularIntensity", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_NormalIntensity", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_NormalStrength", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_NormalStrength2", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_TransmissionScale", floatRange = new Vector2(0f, 0.08f), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_AmbientOcclusionScale", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Transparency", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ReflectionEdges", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ReflectionIntensity", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_BlurReflection", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_HeatTime", floatRange = new Vector2(0, 1.5f), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_HeatForce", floatRange = new Vector2(0, 0.1f), hasFloatRange = true, type = ShaderProperty.Type.Float},
            //Bools
            new ShaderProperty() {name = "_HairEffect", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_GlossUseAlpha", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_TuningColor21or3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_TuningSpec21or3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_TuningSmooth21or3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_NspecR", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_NColorR", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_DetailMask2", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_skin_effect", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_rimlight", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_ZWrite", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseAlbedo2", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseOCCLUSION", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseOCCLUSION2", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseSpecular", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseNormal", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseDetail", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_UseFuzz", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_LacquerOcc", type = ShaderProperty.Type.Boolean},
            //Enum
            new ShaderProperty() {name = "_UVSec", enumValues = new Dictionary<int, string>() {{0, "UV0"}, {1, "UV1"}}, type = ShaderProperty.Type.Enum},
            new ShaderProperty() {name = "_Albedo2UVChannel", enumValues = new Dictionary<int, string>() {{0, "UV1"}, {1, "UV2"}}, type = ShaderProperty.Type.Enum},
            new ShaderProperty() {name = "_Mode", enumValues = new Dictionary<int, string>(){{0, "Opaque"}, {1, "Cutout"}, {2, "Fade"}, {3, "Transparent"}}, type = ShaderProperty.Type.Enum},
            new ShaderProperty() {name = "_SrcBlend", enumValues = new Dictionary<int, string>(){{0, "Zero"},{1, "One" },{2, "DstColor" },{3, "SrcColor" },{4, "OneMinusDstColor" },{5, "SrcAlpha" },{6, "OneMinusSrcColor" },{7, "DstAlpha" },{8, "OneMinusDstAlpha" },{9, "SrcAlphaSaturate" },{10, "OneMinusSrcAlpha" }}, enumColumns = 3, type = ShaderProperty.Type.Enum},
            new ShaderProperty() {name = "_DstBlend", enumValues = new Dictionary<int, string>(){{0, "Zero"},{1, "One" },{2, "DstColor" },{3, "SrcColor" },{4, "OneMinusDstColor" },{5, "SrcAlpha" },{6, "OneMinusSrcColor" },{7, "DstAlpha" },{8, "OneMinusDstAlpha" },{9, "SrcAlphaSaturate" },{10, "OneMinusSrcAlpha" }}, enumColumns = 3, type = ShaderProperty.Type.Enum},
            //Vector4
            new ShaderProperty() {name = "_UVScroll", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_DetailNormalConvert", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_DistortParams", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_InvFadeParemeter", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_AnimationTiling", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_AnimationDirection", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_BumpTiling", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_BumpDirection", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_WorldLightDir", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_Foam", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GAmplitude", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GFrequency", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GSteepness", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GSpeed", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GDirectionAB", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GDirectionCD", type = ShaderProperty.Type.Vector4},
#if KOIKATSU
            //Colors
            new ShaderProperty() {name = "_ShadowColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color3", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color1_2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color2_2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color3_2", type = ShaderProperty.Type.Color},
            //Textures
            new ShaderProperty() {name = "_AnotherRamp", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_LineMask", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_PatternMask1", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_PatternMask2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_PatternMask3", type = ShaderProperty.Type.Texture},
            //Floats
            new ShaderProperty() {name = "_SpecularPower", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SpeclarHeight", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_rimpower", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_rimV", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ShadowExtend", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ShadowExtendAnother", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_alpha", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_EmissionPower", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_patternrotator1", floatRange = new Vector2(-1, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_patternrotator2", floatRange = new Vector2(-1, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_patternrotator3", floatRange = new Vector2(-1, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ColorStrength", hasFloatRange = false, type = ShaderProperty.Type.Float},
            //Booleans
            new ShaderProperty() {name = "_AnotherRampFull", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_DetailBLineG", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_DetailRLineR", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_notusetexspecular", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_patternclamp1", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_patternclamp2", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_patternclamp3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_ambientshadowOFF", type = ShaderProperty.Type.Boolean},
            //Vector4
            new ShaderProperty() {name = "_Patternuv1", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_Patternuv2", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_Patternuv3", type = ShaderProperty.Type.Vector4},
#endif
        };
        private string _workingDirectory;
        private string _workingDirectoryParent;
        private float _width = 604;
        private float _height = 670;
        private Rect _windowRect;
        private int _randomId;
        private bool _enabled;
        private bool _mouseInWindow;
        private Dictionary<TreeNodeObject, TreeNodeData> _treeNodeDatas = new Dictionary<TreeNodeObject, TreeNodeData>();
        private TreeNodeData _currentTreeNode;
        private TreeNodeObject _lastSelectedNode;
        private Dictionary<ITarget, ITargetData> _dirtyTargets = new Dictionary<ITarget, ITargetData>();
        private List<ITarget> _currentTreeNodeTargets = null;
        private string _targetFilter = "";
        private readonly Dictionary<Shader, List<ShaderProperty>> _cachedProperties = new Dictionary<Shader, List<ShaderProperty>>();
        private readonly Texture2D _simpleTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private Action<string, Texture> _selectTextureCallback;
        private readonly Dictionary<string, TextureWrapper> _textures = new Dictionary<string, TextureWrapper>();
        private Vector2 _textureScroll;
        private string _textureFilter = "";
        private readonly List<VectorLine> _boundsDebugLines = new List<VectorLine>();
        private Vector2 _keywordsScroll;
        private string _keywordInput = "";
        private bool _loadingTextures = false;
        private string _pwd;
        private string[] _localFiles;
        private string[] _localFolders;
        private string _currentDirectory;
        private GUIStyle _multiLineButton;
        private bool _stylesInitialized;
        private string _tagInput = "";
        private Bounds _selectedBounds = new Bounds();
        private string _materialsFilter = "";
        private bool _targetFilterIncludeMaterials = true;
        private GUIStyle _alignLeftButton;
        private readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };
        private Action<bool> _changeTextureSettingsCallback;
        private TextureWrapper _selectedTextureForUpdateSettings;
        private TextureWrapper.TextureSettings _displayedSettings;
        private string[] _filterModeNames;
        private string[] _wrapModeNames;
        private string[] _transparentBorderColorNames;
        private readonly DateTime _v140Release = new DateTime(2019, 05, 04);
        #endregion

        #region Unity Methods
        void Start()
        {
            _self = this;
#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("rendererEditor", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
            this._width = Mathf.Clamp(ModPrefs.GetFloat("RendererEditor", "windowWidth", this._width, true), this._width, float.MaxValue);
            this._height = Mathf.Clamp(ModPrefs.GetFloat("RendererEditor", "windowHeight", this._height, true), this._height, float.MaxValue);
            this._windowRect = new Rect((Screen.width - this._width) / 2f, (Screen.height - this._height) / 2f, this._width, this._height);
#elif KOIKATSU
            ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += this.OnSceneLoad;
            //ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += this.OnSceneImport;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingSaved += this.OnSceneSave;
#endif
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);
            this._filterModeNames = Enum.GetNames(typeof(FilterMode));
            this._wrapModeNames = Enum.GetNames(typeof(TextureWrapMode));
            this._transparentBorderColorNames = Enum.GetNames(typeof(TextureWrapper.TextureSettings.TransparentBorderColor));

            float size = 0.012f;
            Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * size,
                topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * size,
                bottomLeftForward = ((Vector3.down + Vector3.left + Vector3.forward) * size),
                bottomRightForward = ((Vector3.down + Vector3.right + Vector3.forward) * size),
                topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * size,
                topRightBack = (Vector3.up + Vector3.right + Vector3.back) * size,
                bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * size,
                bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * size;
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topLeftForward, topRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topRightForward, bottomRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomRightForward, bottomLeftForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomLeftForward, topLeftForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topLeftBack, topRightBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topRightBack, bottomRightBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomRightBack, bottomLeftBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomLeftBack, topLeftBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topLeftBack, topLeftForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topRightBack, topRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomRightBack, bottomRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomLeftBack, bottomLeftForward));

            foreach (VectorLine line in this._boundsDebugLines)
            {
                line.lineWidth = 2f;
                line.active = false;
            }
            this.StartCoroutine(this.EndOfFrame());

            this._currentDirectory = Directory.GetCurrentDirectory();
            this._workingDirectory = Path.Combine(this._currentDirectory, _texturesDir);
            this._workingDirectoryParent = Directory.GetParent(this._workingDirectory).FullName;
            this._pwd = this._workingDirectory;
            this.ExploreCurrentFolder();
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                this._enabled = !this._enabled;
#if HONEYSELECT
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                Studio.Studio.Instance.colorPalette.visible = false;
#endif
                this.CloseTextureWindow();
                this.CheckGizmosEnabled();
            }
            if (Studio.Studio.Instance.treeNodeCtrl.selectNode != this._lastSelectedNode)
            {
                //this.ClearSelectedRenderers();
#if HONEYSELECT
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                Studio.Studio.Instance.colorPalette.visible = false;
#endif
                this.CloseTextureWindow();
                this.CheckGizmosEnabled();
            }

            this._lastSelectedNode = Studio.Studio.Instance.treeNodeCtrl.selectNode;

            Dictionary<ITarget, ITargetData> newDic = null;
            foreach (KeyValuePair<ITarget, ITargetData> pair in this._dirtyTargets)
            {
                if (pair.Key.target == null)
                {
                    newDic = new Dictionary<ITarget, ITargetData>();
                    break;
                }
            }
            if (newDic != null)
            {
                foreach (KeyValuePair<ITarget, ITargetData> pair in this._dirtyTargets)
                {
                    if (pair.Key.target != null)
                        newDic.Add(pair.Key, pair.Value);
                }
                this._dirtyTargets = newDic;
                this.CheckGizmosEnabled();
            }

            if (this._lastSelectedNode != null)
            {
                if (this._treeNodeDatas.TryGetValue(this._lastSelectedNode, out this._currentTreeNode) == false)
                {
                    this._currentTreeNode = new TreeNodeData();
                    this._treeNodeDatas.Add(this._lastSelectedNode, this._currentTreeNode);
                }
                ObjectCtrlInfo objectCtrlInfo;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(this._lastSelectedNode, out objectCtrlInfo))
                    this._currentTreeNodeTargets = this.GetAllTargets(objectCtrlInfo);
                else if (this._currentTreeNodeTargets.Count != 0)
                    this._currentTreeNodeTargets.Clear();
            }
            else
                this._currentTreeNode = null;


            Dictionary<TreeNodeObject, TreeNodeData> newDatas = null;
            foreach (KeyValuePair<TreeNodeObject, TreeNodeData> pair in this._treeNodeDatas)
            {
                if (pair.Key == null)
                {
                    if (newDatas == null)
                        newDatas = new Dictionary<TreeNodeObject, TreeNodeData>();
                    continue;
                }
                Dictionary<Material, MaterialInfo> newMaterialDic = null;
                foreach (KeyValuePair<Material, MaterialInfo> material in pair.Value.selectedMaterials)
                {
                    if (material.Key == null || material.Value.target.target == null)
                    {
                        newMaterialDic = new Dictionary<Material, MaterialInfo>();
                        break;
                    }
                }
                if (newMaterialDic != null)
                {
                    foreach (KeyValuePair<Material, MaterialInfo> material in pair.Value.selectedMaterials)
                    {
                        if (material.Value.target.target == null)
                            continue;
                        if (material.Key != null)
                            newMaterialDic.Add(material.Key, material.Value);
                        else
                        {
                            Material newMat = material.Value.target.materials[material.Value.index];
                            if (newMat != null)
                                newMaterialDic.Add(newMat, material.Value);
                        }
                    }
                    pair.Value.selectedMaterials = newMaterialDic;
                    if (pair.Key == this._lastSelectedNode)
                    this.CloseTextureWindow();
                }
                HashSet<ITarget> newTargetsHashset = null;
                foreach (ITarget target in pair.Value.selectedTargets)
                {
                    if (target == null)
                    {
                        newTargetsHashset = new HashSet<ITarget>();
                        break;
                    }
                }
                if (newTargetsHashset != null)
                {
                    foreach (ITarget target in pair.Value.selectedTargets)
                    {
                        if (target != null)
                            newTargetsHashset.Add(target);
                    }
                    pair.Value.selectedTargets = newTargetsHashset;
                    if (pair.Key == this._lastSelectedNode)
                        this.CloseTextureWindow();
                }
            }
            if (newDatas != null)
            {
                foreach (KeyValuePair<TreeNodeObject, TreeNodeData> pair in this._treeNodeDatas)
                {
                    if (pair.Key != null)
                        newDatas.Add(pair.Key, pair.Value);
                }
                this._treeNodeDatas = newDatas;
            }
        }

        private IEnumerator EndOfFrame()
        {
            for(;;)
            {
                yield return new WaitForEndOfFrame();
                this.DrawGizmos();
            }
        }

        void OnGUI()
        {
            if (this._stylesInitialized == false)
            {
                this.InitializeStyles();
                this._stylesInitialized = true;
            }
            if (this._enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null)
            {
                Color c = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                GUI.Box(this._windowRect, "", this._customBoxStyle);
                GUI.backgroundColor = c;
                this._windowRect = GUILayout.Window(this._randomId, this._windowRect, this.WindowFunction, "Renderer Editor " + RendererEditor.versionNum);
                this._mouseInWindow = this._windowRect.Contains(Event.current.mousePosition);
                if (this._selectTextureCallback != null)
                {
                    Rect selectTextureRect = new Rect(this._windowRect.max.x + 4, this._windowRect.max.y - 440, 230, 440);
                    GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                    GUI.Box(selectTextureRect, "", this._customBoxStyle);
                    GUI.backgroundColor = c;
                    selectTextureRect = GUILayout.Window(this._randomId + 1, selectTextureRect, this.SelectTextureWindow, "Select Texture");
                    this._mouseInWindow = this._mouseInWindow || selectTextureRect.Contains(Event.current.mousePosition);
                    if (this._changeTextureSettingsCallback != null)
                    {
                        Rect settingsRect = new Rect(selectTextureRect.max.x + 4, selectTextureRect.max.y - 335, 250, 335);
                        GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                        GUI.Box(settingsRect, "", this._customBoxStyle);
                        GUI.backgroundColor = c;
                        settingsRect = GUILayout.Window(this._randomId + 2, settingsRect, this.ChangeTextureSettingsWindow, "Properties");
                        this._mouseInWindow = this._mouseInWindow || settingsRect.Contains(Event.current.mousePosition);
                    }
                }
                if (this._mouseInWindow)
                    Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => this._mouseInWindow && this._enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null;
            }
        }
        #endregion

        #region Private Methods
        private void InitializeStyles()
        {
            this._multiLineButton = new GUIStyle(GUI.skin.button) {wordWrap = true};
            this._alignLeftButton = new GUIStyle(GUI.skin.button) {alignment = TextAnchor.MiddleLeft};
        }

        private void WindowFunction(int id)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            if (this._currentTreeNode.selectedTargets.Count != 0)
            {
                ITarget firstTarget = this._currentTreeNode.selectedTargets.First();
                ITargetData firstData = null;
                this._dirtyTargets.TryGetValue(firstTarget, out firstData);

                {
                    bool oldEnabled = firstData == null || firstData.currentEnabled;
                    bool newEnabled = GUILayout.Toggle(oldEnabled, "Enabled");
                    if (newEnabled != oldEnabled)
                    {
                        foreach (ITarget target in this._currentTreeNode.selectedTargets)
                        {
                            ITargetData container;
                            this.SetTargetDirty(target, out container);
                            container.currentEnabled = newEnabled;

                            Transform t = target.transform;
                            ObjectCtrlInfo info;
                            while ((info = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == t).Value) == null)
                                t = t.parent;
                            target.enabled = info.treeNodeObject.IsVisible() && newEnabled;
                        }
                    }
                }

                firstTarget.DisplayParams(this._currentTreeNode.selectedTargets, this.SetTargetDirty);

                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reset target"))
                    {
                        foreach (ITarget target in this._currentTreeNode.selectedTargets)
                        {
                            this.ResetTarget(target);
                        }
                    }
                    if (GUILayout.Button("Reset w/ materials"))
                    {
                        foreach (ITarget target in this._currentTreeNode.selectedTargets)
                        {
                            this.ResetTarget(target, true);
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginVertical("Materials", GUI.skin.window);
                GUILayout.BeginHorizontal();
                this._materialsFilter = GUILayout.TextField(this._materialsFilter);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    this._materialsFilter = "";
                if (GUILayout.Button("Select all", GUILayout.ExpandWidth(false)))
                {
                    while (this._currentTreeNode.selectedMaterials.Count != 0)
                    {
                        this.UnselectMaterial(this._currentTreeNode.selectedMaterials.First().Key);
                    }
                    foreach (ITarget selectedTarget in this._currentTreeNode.selectedTargets)
                    {
                        for (int i = 0; i < selectedTarget.sharedMaterials.Length; i++)
                        {
                            Material material = selectedTarget.materials[i];
                            if (material == null || material.name.IndexOf(this._materialsFilter, StringComparison.OrdinalIgnoreCase) == -1)
                                continue;
                            if (!this._currentTreeNode.selectedMaterials.ContainsKey(material))
                                this.SelectMaterial(material, selectedTarget, i);
                        }
                    }
#if HONEYSELECT
                    Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                Studio.Studio.Instance.colorPalette.visible = false;
#endif
                    this.CloseTextureWindow();
                }
                GUILayout.EndHorizontal();

                this._currentTreeNode.materialsScroll = GUILayout.BeginScrollView(this._currentTreeNode.materialsScroll, GUILayout.Height(120));
                if (this._currentTreeNode.selectedTargets.Count != 0)
                {
                    foreach (ITarget selectedTarget in this._currentTreeNode.selectedTargets)
                    {
                        for (int i = 0; i < selectedTarget.sharedMaterials.Length; i++)
                        {
                            Material material = selectedTarget.sharedMaterials[i];
                            if (material == null || material.name.IndexOf(this._materialsFilter, StringComparison.OrdinalIgnoreCase) == -1)
                                continue;
                            Color c = GUI.color;
                            bool isMaterialDirty = this._dirtyTargets.TryGetValue(selectedTarget, out ITargetData targetData) && targetData.dirtyMaterials.ContainsKey(material);
                            if (this._currentTreeNode.selectedMaterials.ContainsKey(material))
                                GUI.color = Color.cyan;
                            else if (isMaterialDirty)
                                GUI.color = Color.magenta;
                            if (GUILayout.Button(material.name + (isMaterialDirty ? "*" : "") + (this._currentTreeNode.selectedTargets.Count > 1 ? "(" + selectedTarget.name + ")" : ""), this._alignLeftButton))
                            {
                                material = selectedTarget.materials[i];
                                if (Input.GetKey(KeyCode.LeftControl) == false)
                                {
                                    this.ClearSelectedMaterials();
                                    this.SelectMaterial(material, selectedTarget, i);
                                }
                                else
                                {
                                    if (this._currentTreeNode.selectedMaterials.ContainsKey(material))
                                        this.UnselectMaterial(material);
                                    else
                                        this.SelectMaterial(material, selectedTarget, i);
                                }
#if HONEYSELECT
                                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                                Studio.Studio.Instance.colorPalette.visible = false;
#endif
                                this.CloseTextureWindow();
                            }
                            GUI.color = c;
                        }
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                if (this._currentTreeNode.selectedMaterials.Count != 0)
                {
                    KeyValuePair<Material, MaterialInfo> firstPair = this._currentTreeNode.selectedMaterials.First();

                    ITargetData linkedTargetData = null;
                    MaterialData firstMaterialData = null;
                    if (this._dirtyTargets.TryGetValue(firstPair.Value.target, out linkedTargetData))
                        linkedTargetData.dirtyMaterials.TryGetValue(firstPair.Key, out firstMaterialData);

                    Material displayedMaterial = firstPair.Key;
                    this._currentTreeNode.propertiesScroll = GUILayout.BeginScrollView(this._currentTreeNode.propertiesScroll);

                    GUILayout.Label("Shader: " + displayedMaterial.shader.name, GUI.skin.box);

                    GUILayout.BeginVertical(GUI.skin.box);
                    this.RenderQueueDrawer(displayedMaterial, firstMaterialData);
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(GUI.skin.box);
                    this.RenderTypeDrawer(displayedMaterial, firstMaterialData);
                    GUILayout.EndVertical();

                    if (this._cachedProperties.TryGetValue(displayedMaterial.shader, out List<ShaderProperty> cachedProperties) == false)
                    {
                        cachedProperties = new List<ShaderProperty>();
                        foreach (ShaderProperty property in this._shaderProperties)
                            if (displayedMaterial.HasProperty(property.name))
                                cachedProperties.Add(property);
                        this._cachedProperties.Add(displayedMaterial.shader, cachedProperties);
                    }

                    foreach (ShaderProperty property in cachedProperties)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        this.ShaderPropertyDrawer(displayedMaterial, firstMaterialData, property);
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginVertical(GUI.skin.box);
                    this.KeywordsDrawer(displayedMaterial, firstMaterialData);
                    GUILayout.EndVertical();

                    GUILayout.EndScrollView();

                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Reset"))
                            foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                            {
                                if (this._dirtyTargets.TryGetValue(material.Value.target, out ITargetData targetData) &&
                                    targetData.dirtyMaterials.TryGetValue(material.Key, out MaterialData data))
                                    this.ResetMaterial(material.Key, data, targetData);
                            }
                        GUILayout.EndHorizontal();
                    }
                }
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("Targets", GUI.skin.window, GUILayout.Width(180f));
            GUILayout.BeginHorizontal();
            this._targetFilter = GUILayout.TextField(this._targetFilter);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                this._targetFilter = "";
            GUILayout.EndHorizontal();
            this._targetFilterIncludeMaterials = GUILayout.Toggle(this._targetFilterIncludeMaterials, "Include materials");
            this._currentTreeNode.targetsScroll = GUILayout.BeginScrollView(this._currentTreeNode.targetsScroll);
            
            foreach (ITarget target in this._currentTreeNodeTargets)
            {
                if (target.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1 && (this._targetFilterIncludeMaterials == false || target.sharedMaterials.All(m => m != null && m.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1)))
                    continue;
                Color c = GUI.color;
                ITargetData targetData = null;
                this._dirtyTargets.TryGetValue(target, out targetData);

                if (this._currentTreeNode.onlyDirty && targetData == null)
                    continue;

                if (this._currentTreeNode.selectedTargets.Contains(target))
                    GUI.color = Color.cyan;
                else if (targetData != null)
                    GUI.color = Color.magenta;

                if (targetData != null && targetData.currentEnabled == false)
                {
                    if (this._currentTreeNode.onlyActive)
                    {
                        GUI.color = c;
                        continue;                        
                    }
                    GUI.color = new Color(GUI.color.r / 2, GUI.color.g / 2, GUI.color.b / 2);
                }

                if (GUILayout.Button(target.name + (targetData != null ? "*" : ""), this._alignLeftButton))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (this._currentTreeNode.selectedTargets.Contains(target))
                            this.UnselectTarget(target);
                        else
                            this.SelectTarget(target);
                    }
                    else if (Input.GetKey(KeyCode.LeftShift) && this._currentTreeNode.selectedTargets.Count > 0)
                    {
                        int firstIndex = this._currentTreeNodeTargets.IndexOf(this._currentTreeNode.selectedTargets.First());
                        int lastIndex = this._currentTreeNodeTargets.IndexOf(target);
                        if (firstIndex != lastIndex)
                        {
                            int inc;
                            if (firstIndex < lastIndex)
                                inc = 1;
                            else
                                inc = -1;
                            for (int i = firstIndex; i != lastIndex; i += inc)
                            {
                                ITarget r = this._currentTreeNodeTargets[i];
                                if (r.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1 && r.sharedMaterials.All(m => m != null && m.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1))
                                    continue;
                                if (this._currentTreeNode.selectedTargets.Contains(r) == false)
                                    this.SelectTarget(r);
                            }
                            if (this._currentTreeNode.selectedTargets.Contains(target) == false)
                                this.SelectTarget(target);
                        }
                    }
                    else
                    {
                        this.ClearSelectedTargets();
                        this.SelectTarget(target);
                    }
#if HONEYSELECT
                    Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                    Studio.Studio.Instance.colorPalette.visible = false;
#endif
                    this.CloseTextureWindow();
                    this.CheckGizmosEnabled();
                }
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            this._currentTreeNode.onlyActive = GUILayout.Toggle(this._currentTreeNode.onlyActive, "Show only active");
            this._currentTreeNode.onlyDirty = GUILayout.Toggle(this._currentTreeNode.onlyDirty, "Show only dirty");

            if (GUILayout.Button("Select all"))
            {
                while (this._currentTreeNode.selectedTargets.Count != 0)
                {
                    this.UnselectTarget(this._currentTreeNode.selectedTargets.First());
                }
                foreach (ITarget target in this._currentTreeNodeTargets)
                {
                    if (target.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1 && (this._targetFilterIncludeMaterials == false || target.sharedMaterials.All(m => m != null && m.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1)))
                        continue;

                    Color c = GUI.color;
                    ITargetData targetData = null;
                    this._dirtyTargets.TryGetValue(target, out targetData);

                    if (targetData == null)
                    {
                        if (this._currentTreeNode.onlyDirty)
                            continue;
                    }
                    else
                    {
                        if (this._currentTreeNode.onlyActive && targetData.currentEnabled == false)
                            continue;
                    }

                    this.SelectTarget(target);
                }
#if HONEYSELECT
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                Studio.Studio.Instance.colorPalette.visible = false;
#endif
            }
            if (GUILayout.Button("Select renderers"))
            {
                while (this._currentTreeNode.selectedTargets.Count != 0)
                {
                    this.UnselectTarget(this._currentTreeNode.selectedTargets.First());
                }
                foreach (ITarget target in this._currentTreeNodeTargets)
                {
                    if (target.targetType != TargetType.Renderer)
                        continue;
                    if (target.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1 && (this._targetFilterIncludeMaterials == false || target.sharedMaterials.All(m => m != null && m.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1)))
                        continue;
                    this.SelectTarget(target);
                }
#if HONEYSELECT
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                Studio.Studio.Instance.colorPalette.visible = false;
#endif
            }
            if (GUILayout.Button("Select projectors"))
            {
                while (this._currentTreeNode.selectedTargets.Count != 0)
                {
                    this.UnselectTarget(this._currentTreeNode.selectedTargets.First());
                }
                foreach (ITarget t in this._currentTreeNodeTargets)
                {
                    if (t.targetType != TargetType.Projector)
                        continue;
                    if (t.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1 && (this._targetFilterIncludeMaterials == false || t.sharedMaterials.All(m => m != null && m.name.IndexOf(this._targetFilter, StringComparison.OrdinalIgnoreCase) == -1)))
                        continue;
                    this.SelectTarget(t);
                }
#if HONEYSELECT
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
#elif KOIKATSU
                Studio.Studio.Instance.colorPalette.visible = false;
#endif
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void SelectTextureWindow(int id)
        {
            GUILayout.BeginHorizontal();
            this._textureFilter = GUILayout.TextField(this._textureFilter);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                this._textureFilter = "";
            GUILayout.EndHorizontal();

            GUILayout.Label(this._pwd.Substring(this._workingDirectoryParent.Length));
            if (this._loadingTextures)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Loading" + new String('.', (int)(Time.time * 2 % 4)));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                if (GUILayout.Button("No Texture"))
                {
                    this._selectTextureCallback("", null);
                    this.CloseTextureWindow();
                }
                this._textureScroll = GUILayout.BeginScrollView(this._textureScroll);
                Color c = GUI.color;
                GUI.color = Color.green;
                if (this._pwd.Equals(this._workingDirectory, StringComparison.OrdinalIgnoreCase) == false && GUILayout.Button(".. (Parent folder)"))
                {
                    this._pwd = Directory.GetParent(this._pwd).FullName;
                    this.ExploreCurrentFolder();
                    this._changeTextureSettingsCallback = null;
                }
                foreach (string folder in this._localFolders)
                {
                    string directoryName = folder.Substring(this._pwd.Length);
                    if (directoryName.IndexOf(this._textureFilter, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(directoryName, this._multiLineButton, GUILayout.Width(178)))
                        {
                            this._pwd = folder;
                            this.ExploreCurrentFolder();
                            this._changeTextureSettingsCallback = null;
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                }
                GUI.color = c;
                foreach (string file in this._localFiles)
                {
                    string localFileName = Path.GetFileName(file);
                    if (localFileName.IndexOf(this._textureFilter, StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    if (RendererEditor.previewTextures)
                    {
                        TextureWrapper texture = this.GetTexture(file);
                        if (texture != null)
                        {
                            GUILayout.BeginVertical(GUI.skin.box);
                            GUILayout.Label(localFileName);
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(GUIContent.none, GUILayout.Width(178), GUILayout.Height(178)))
                            {
                                this._selectTextureCallback(file, texture.texture);
                                this.CloseTextureWindow();
                            }
                            Rect layoutRectangle = GUILayoutUtility.GetLastRect();
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            layoutRectangle.xMin += 2;
                            layoutRectangle.xMax -= 2;
                            layoutRectangle.yMin += 2;
                            layoutRectangle.yMax -= 2;
                            GUI.DrawTexture(layoutRectangle, texture.texture, ScaleMode.StretchToFill, true);
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Properties", GUILayout.ExpandWidth(false)))
                            {
                                this._selectedTextureForUpdateSettings = texture;
                                this._displayedSettings = new TextureWrapper.TextureSettings(texture.settings);
                                this._changeTextureSettingsCallback = (saved) =>
                                {
                                    if (saved)
                                    {
                                        texture.settings = new TextureWrapper.TextureSettings(this._displayedSettings);
                                        TextureWrapper.TextureSettings.Save(texture.path, texture.settings);
                                        this.RefreshSingleTexture(texture);
                                    }
                                };
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                        }
                    }
                    else
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(localFileName, this._multiLineButton, GUILayout.Width(178)))
                        {
                            this._selectTextureCallback(file, this.GetTexture(file)?.texture);
                            this.CloseTextureWindow();
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Properties", GUILayout.ExpandWidth(false)))
                        {
                            TextureWrapper texture = this.GetTexture(file);

                            this._selectedTextureForUpdateSettings = texture;
                            this._displayedSettings = new TextureWrapper.TextureSettings(texture.settings);
                            this._changeTextureSettingsCallback = (saved) =>
                            {
                                if (saved)
                                {
                                    texture.settings = new TextureWrapper.TextureSettings(this._displayedSettings);
                                    TextureWrapper.TextureSettings.Save(texture.path, texture.settings);
                                    this.RefreshSingleTexture(texture);
                                }
                            };
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh") && this._loadingTextures == false)
                this.RefreshTextures();
            if (GUILayout.Button("Close"))
                this.CloseTextureWindow();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open folder"))
                System.Diagnostics.Process.Start(this._pwd);
            if (GUILayout.Button("Unload unused"))
            {
                HashSet<string> usedTextures = new HashSet<string>();
                foreach (KeyValuePair<ITarget, ITargetData> pair in this._dirtyTargets)
                {
                    foreach (KeyValuePair<Material, MaterialData> pair2 in pair.Value.dirtyMaterials)
                    {
                        foreach (KeyValuePair<string, MaterialData.TextureData> pair3 in pair2.Value.dirtyTextureProperties)
                        {
                            if (usedTextures.Contains(pair3.Value.currentTexturePath) == false)
                            {
                                usedTextures.Add(pair3.Value.currentTexturePath);
                            }
                        }
                    }
                }
                foreach (KeyValuePair<string, TextureWrapper> pair in new Dictionary<string, TextureWrapper>(this._textures))
                {
                    if (usedTextures.Contains(pair.Key) == false)
                    {
                        this._textures.Remove(pair.Key);
                        Destroy(pair.Value.texture);
                    }
                }
                Resources.UnloadUnusedAssets();
                GC.Collect();
            }
            GUILayout.EndHorizontal();
        }

        private void ChangeTextureSettingsWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label(Path.GetFileName(this._selectedTextureForUpdateSettings.path));
                GUILayout.Label("Size: " + this._selectedTextureForUpdateSettings.texture.width + "x" + this._selectedTextureForUpdateSettings.texture.height);
                this._displayedSettings.bypassSRGBSampling = GUILayout.Toggle(this._displayedSettings.bypassSRGBSampling, "Bypass sRGB sampling");
                GUILayout.Label("Filter Mode");
                this._displayedSettings.filterMode = (FilterMode)GUILayout.SelectionGrid((int)this._displayedSettings.filterMode, this._filterModeNames, 3);
                GUILayout.Label("Aniso Level", GUILayout.ExpandWidth(false));
                GUILayout.BeginHorizontal();
                this._displayedSettings.anisoLevel = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._displayedSettings.anisoLevel, 0, 16));
                string s = GUILayout.TextField(this._displayedSettings.anisoLevel.ToString(), GUILayout.Width(40));
                int res;
                if (int.TryParse(s, out res))
                {
                    res = Mathf.Clamp(res, 0, 16);
                    this._displayedSettings.anisoLevel = res;                    
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Wrap Mode");
                this._displayedSettings.wrapMode = (TextureWrapMode)GUILayout.SelectionGrid((int)this._displayedSettings.wrapMode, this._wrapModeNames, 2);
                GUILayout.BeginHorizontal();
                this._displayedSettings.transparentBorder = GUILayout.Toggle(this._displayedSettings.transparentBorder, "Transparent border", GUILayout.ExpandWidth(false));
                this._displayedSettings.transparentBorderColor = (TextureWrapper.TextureSettings.TransparentBorderColor)GUILayout.SelectionGrid((int)this._displayedSettings.transparentBorderColor, this._transparentBorderColorNames, 2);
                GUILayout.EndHorizontal();
                this._displayedSettings.compressed = GUILayout.Toggle(this._displayedSettings.compressed, "Compressed");
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save"))
                {
                    this._changeTextureSettingsCallback(true);
                    this._changeTextureSettingsCallback = null;
                }
                if (GUILayout.Button("Cancel"))
                {
                    this._changeTextureSettingsCallback(false);
                    this._changeTextureSettingsCallback = null;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void RefreshTextures()
        {
            this.StartCoroutine(this.RefreshTextures_Routine());
        }

        private IEnumerator RefreshTextures_Routine()
        {
            this._loadingTextures = true;
            this.ExploreCurrentFolder();
            if (RendererEditor.previewTextures)
            {
                foreach (string file in this._localFiles)
                {
                    yield return null;
                    this.RefreshSingleTexture(file);
                }
            }
            this._loadingTextures = false;
        }

        private void RefreshSingleTexture(string path)
        {
            TextureWrapper texture;
            if (this._textures.TryGetValue(path, out texture))
                this.RefreshSingleTexture(texture);
        }

        private void RefreshSingleTexture(TextureWrapper texture)
        {
            Destroy(texture.texture);
            this._textures.Remove(texture.path);
            this.LoadSingleTexture(texture.path);
        }

        private TextureWrapper LoadSingleTexture(string path, TextureWrapper.TextureSettings settings = null)
        {
            string settingsFile = path + ".settings.xml";
            if (File.Exists(settingsFile))
                settings = TextureWrapper.TextureSettings.Load(path);
            else
            {
                if (settings == null)
                {
                    settings = new TextureWrapper.TextureSettings();
                    if (File.GetLastWriteTime(path) < this._v140Release)
                        settings.bypassSRGBSampling = false;
                }
                TextureWrapper.TextureSettings.Save(path, settings);
            }

            Texture2D texture = new Texture2D(1024, 1024, TextureFormat.ARGB32, true, settings.bypassSRGBSampling);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
                return null;

            switch (settings.filterMode)
            {
                case FilterMode.Point:
                    TextureScale.Point(texture, Mathf.Clamp(Mathf.NextPowerOfTwo(texture.width), 32, 8192), Mathf.Clamp(Mathf.NextPowerOfTwo(texture.height), 32, 8192));
                    break;
                case FilterMode.Bilinear:
                case FilterMode.Trilinear:
                    TextureScale.Bilinear(texture, Mathf.Clamp(Mathf.NextPowerOfTwo(texture.width), 32, 8192), Mathf.Clamp(Mathf.NextPowerOfTwo(texture.height), 32, 8192));
                    break;
            }

            texture.filterMode = settings.filterMode;
            texture.anisoLevel = settings.anisoLevel;
            texture.wrapMode = settings.wrapMode;
            texture.Apply(true);
            if (settings.transparentBorder)
            {
                int width = texture.width;
                int height = texture.height;
                Color32 c = new Color32(0, 0, 0, 0);
                switch (settings.transparentBorderColor)
                {
                    case TextureWrapper.TextureSettings.TransparentBorderColor.Black:
                        c = new Color32(0, 0, 0, 0);
                        break;
                    case TextureWrapper.TextureSettings.TransparentBorderColor.White:
                        c = new Color32(255, 255, 255, 0);
                        break;
                }
                for (int i = 0; i < texture.mipmapCount; i++)
                {
                    Color32[] pixels = texture.GetPixels32(i);
                    int lastLine = width * (height - 1);
                    for (int j = 0; j < width; j++)
                    {
                        pixels[j] = c;
                        pixels[lastLine + j] = c;
                    }
                    for (int j = 0; j < height; j++)
                    {
                        pixels[j * width] = c;
                        pixels[j * width + width - 1] = c;
                    }
                    width /= 2;
                    height /= 2;
                    texture.SetPixels32(pixels, i);
                }
                texture.Apply(false);
            }
            if (settings.compressed)
                texture.Compress(true);

            TextureWrapper textureWrapper = new TextureWrapper
            {
                path = path,
                texture = texture,
                settings = settings
            };

            if (this._textures.ContainsKey(path) == false)
                this._textures.Add(path, textureWrapper);
            else
            {
                Destroy(this._textures[path].texture);
                this._textures[path] = textureWrapper;
            }
            foreach (KeyValuePair<ITarget, ITargetData> pair in this._dirtyTargets)
            {
                foreach (KeyValuePair<Material, MaterialData> pair2 in pair.Value.dirtyMaterials)
                {
                    foreach (KeyValuePair<string, MaterialData.TextureData> pair3 in pair2.Value.dirtyTextureProperties)
                    {
                        if (pair3.Value.currentTexturePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                        {
                            pair2.Key.SetTexture(pair3.Key, texture);
                        }
                    }
                }
            }
            return textureWrapper;
        }

        private void ExploreCurrentFolder()
        {
            if (Directory.Exists(this._pwd) == false)
                this._pwd = _texturesDir;
            if (Directory.Exists(this._pwd) == false)
                Directory.CreateDirectory(_texturesDir);

            this._localFiles = Directory.GetFiles(this._pwd, "*.*").Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToArray();
            this._localFolders = Directory.GetDirectories(this._pwd);
        }

        private TextureWrapper GetTexture(string path, TextureWrapper.TextureSettings settings = null)
        {
            TextureWrapper t;
            if (this._textures.TryGetValue(path, out t))
                return t;
            return this.LoadSingleTexture(path, settings);
        }

        private void CloseTextureWindow()
        {
            this._selectTextureCallback = null;
            this._changeTextureSettingsCallback = null;
        }

        private void SelectMaterial(Material material, ITarget target, int index)
        {
            this._currentTreeNode.selectedMaterials.Add(material, new MaterialInfo{index = index, target = target});
            this.CloseTextureWindow();
        }

        private void UnselectMaterial(Material material)
        {
            this._currentTreeNode.selectedMaterials.Remove(material);
            this.CloseTextureWindow();
        }

        private void ClearSelectedMaterials()
        {
            this._currentTreeNode.selectedMaterials.Clear();
            this.CloseTextureWindow();
        }

        private void SelectTarget(ITarget target)
        {
            this._currentTreeNode.selectedTargets.Add(target);
            this.CloseTextureWindow();
        }

        private void UnselectTarget(ITarget target)
        {
            this._currentTreeNode.selectedTargets.Remove(target);
            foreach (KeyValuePair<Material, MaterialInfo> pair in new Dictionary<Material, MaterialInfo>(this._currentTreeNode.selectedMaterials))
            {
                if (pair.Value.target == target)
                    this.UnselectMaterial(pair.Key);
            }
            this.CloseTextureWindow();
        }

        private void ClearSelectedTargets()
        {
            this._currentTreeNode.selectedTargets.Clear();
            this.ClearSelectedMaterials();
        }

        private void UpdateSelectedBounds()
        {
            Vector3 finalMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 finalMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (ITarget selectedTarget in this._currentTreeNode.selectedTargets)
            {
                if (selectedTarget.hasBounds == false)
                    continue;
                Bounds bounds = selectedTarget.bounds;
                if (bounds.min.x < finalMin.x)
                    finalMin.x = bounds.min.x;
                if (bounds.min.y < finalMin.y)
                    finalMin.y = bounds.min.y;
                if (bounds.min.z < finalMin.z)
                    finalMin.z = bounds.min.z;

                if (bounds.max.x > finalMax.x)
                    finalMax.x = bounds.max.x;
                if (bounds.max.y > finalMax.y)
                    finalMax.y = bounds.max.y;
                if (bounds.max.z > finalMax.z)
                    finalMax.z = bounds.max.z;
            }
            this._selectedBounds.SetMinMax(finalMin, finalMax);
        }

        private bool SetMaterialDirty(Material mat, out MaterialData data, ITargetData targetData = null)
        {
            if (targetData == null)
                this.SetTargetDirty(this._currentTreeNode.selectedMaterials[mat].target, out targetData);
            if (targetData.dirtyMaterials.TryGetValue(mat, out data) == false)
            {
                data = new MaterialData();
                targetData.dirtyMaterials.Add(mat, data);
                return true;
            }
            return false;
        }

        private void TryResetMaterial(Material mat, MaterialData materialData, ITargetData targetData)
        {
            if (mat == null)
                return;
            if (materialData.hasRenderQueue == false && materialData.hasRenderType == false && materialData.dirtyColorProperties.Count == 0 && materialData.dirtyBooleanProperties.Count == 0 && materialData.dirtyEnumProperties.Count == 0 && materialData.dirtyFloatProperties.Count == 0 && materialData.dirtyVector4Properties.Count == 0 && materialData.dirtyTextureOffsetProperties.Count == 0 && materialData.dirtyTextureScaleProperties.Count == 0 && materialData.dirtyTextureProperties.Count == 0 && materialData.enabledKeywords.Count == 0 && materialData.disabledKeywords.Count == 0)
            {
                this.ResetMaterial(mat, materialData, targetData);
            }
        }

        private void ResetMaterial(Material mat, MaterialData materialData, ITargetData targetData)
        {
            if (mat == null)
                return;
            if (materialData.hasRenderQueue)
            {
                mat.renderQueue = materialData.originalRenderQueue;
                materialData.hasRenderQueue = false;
            }
            if (materialData.hasRenderType)
            {
                mat.SetOverrideTag("RenderType", materialData.originalRenderType);
                materialData.hasRenderType = false;
            }
            foreach (KeyValuePair<string, Color> pair in materialData.dirtyColorProperties)
                mat.SetColor(pair.Key, pair.Value);
            foreach (KeyValuePair<string, bool> pair in materialData.dirtyBooleanProperties)
                mat.SetFloat(pair.Key, pair.Value ? 1f : 0f);
            foreach (KeyValuePair<string, int> pair in materialData.dirtyEnumProperties)
                mat.SetFloat(pair.Key, pair.Value);
            foreach (KeyValuePair<string, float> pair in materialData.dirtyFloatProperties)
                mat.SetFloat(pair.Key, pair.Value);
            foreach (KeyValuePair<string, Vector4> pair in materialData.dirtyVector4Properties)
                mat.SetVector(pair.Key, pair.Value);
            foreach (KeyValuePair<string, Vector2> pair in materialData.dirtyTextureOffsetProperties)
                mat.SetTextureOffset(pair.Key, pair.Value);
            foreach (KeyValuePair<string, Vector2> pair in materialData.dirtyTextureScaleProperties)
                mat.SetTextureScale(pair.Key, pair.Value);
            foreach (KeyValuePair<string, MaterialData.TextureData> pair in materialData.dirtyTextureProperties)
                mat.SetTexture(pair.Key, pair.Value.originalTexture);
            foreach (string enabledKeyword in materialData.enabledKeywords)
                mat.DisableKeyword(enabledKeyword);
            materialData.enabledKeywords.Clear();
            foreach (string disabledKeyword in materialData.disabledKeywords)
                mat.EnableKeyword(disabledKeyword);
            materialData.disabledKeywords.Clear();

            targetData.dirtyMaterials.Remove(mat);
        }

        private bool SetTargetDirty(ITarget target, out ITargetData data)
        {
            if (this._dirtyTargets.TryGetValue(target, out data) == false)
            {
                data = target.GetNewData();
                this._dirtyTargets.Add(target, data);
                return true;
            }
            return false;
        }

        private void ResetTarget(ITarget target, bool withMaterials = false)
        {
            ITargetData container;
            if (this._dirtyTargets.TryGetValue(target, out container))
            {
                Transform t = target.transform;
                ObjectCtrlInfo info;
                while ((info = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == t).Value) == null)
                    t = t.parent;

                target.enabled = info.treeNodeObject.IsVisible();
                target.ResetData(container);
                if (withMaterials)
                    foreach (KeyValuePair<Material, MaterialData> pair in new Dictionary<Material, MaterialData>(container.dirtyMaterials))
                        this.ResetMaterial(pair.Key, pair.Value, container);
                if (container.dirtyMaterials.Count == 0)
                    this._dirtyTargets.Remove(target);
            }
        }

        private void DrawGizmos()
        {
            if (!this._enabled || Studio.Studio.Instance.treeNodeCtrl.selectNode == null || this._currentTreeNode == null || this._currentTreeNode.selectedTargets.Count == 0)
                return;
            this.UpdateSelectedBounds();
            Vector3 topLeftForward = new Vector3(this._selectedBounds.min.x, this._selectedBounds.max.y, this._selectedBounds.max.z),
                topRightForward = this._selectedBounds.max,
                bottomLeftForward = new Vector3(this._selectedBounds.min.x, this._selectedBounds.min.y, this._selectedBounds.max.z),
                bottomRightForward = new Vector3(this._selectedBounds.max.x, this._selectedBounds.min.y, this._selectedBounds.max.z),
                topLeftBack = new Vector3(this._selectedBounds.min.x, this._selectedBounds.max.y, this._selectedBounds.min.z) ,
                topRightBack = new Vector3(this._selectedBounds.max.x, this._selectedBounds.max.y, this._selectedBounds.min.z) ,
                bottomLeftBack = this._selectedBounds.min,
                bottomRightBack = new Vector3(this._selectedBounds.max.x, this._selectedBounds.min.y, this._selectedBounds.min.z);
            int i = 0;
            this._boundsDebugLines[i++].SetPoints(topLeftForward, topRightForward);
            this._boundsDebugLines[i++].SetPoints(topRightForward, bottomRightForward);
            this._boundsDebugLines[i++].SetPoints(bottomRightForward, bottomLeftForward);
            this._boundsDebugLines[i++].SetPoints(bottomLeftForward, topLeftForward);
            this._boundsDebugLines[i++].SetPoints(topLeftBack, topRightBack);
            this._boundsDebugLines[i++].SetPoints(topRightBack, bottomRightBack);
            this._boundsDebugLines[i++].SetPoints(bottomRightBack, bottomLeftBack);
            this._boundsDebugLines[i++].SetPoints(bottomLeftBack, topLeftBack);
            this._boundsDebugLines[i++].SetPoints(topLeftBack, topLeftForward);
            this._boundsDebugLines[i++].SetPoints(topRightBack, topRightForward);
            this._boundsDebugLines[i++].SetPoints(bottomRightBack, bottomRightForward);
            this._boundsDebugLines[i++].SetPoints(bottomLeftBack, bottomLeftForward);

            foreach (VectorLine line in this._boundsDebugLines)
                line.Draw();

        }

        private void CheckGizmosEnabled()
        {
            bool a = this._enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null && this._currentTreeNode != null && this._currentTreeNode.selectedTargets.Count != 0;
            foreach (VectorLine line in this._boundsDebugLines)
                line.active = a;
        }

        private void OnDuplicate(ObjectCtrlInfo source, ObjectCtrlInfo destination)
        {
            this.ExecuteDelayed(() =>
            {
                
                List<ITarget> sourceTargets = this.GetAllTargets(source);
                sourceTargets.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.Ordinal));
                List<ITarget> destinationTargets = this.GetAllTargets(destination);
                destinationTargets.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.Ordinal));
                for (int i = 0; i < sourceTargets.Count && i < destinationTargets.Count; i++)
                {
                    ITarget sourceTarget = sourceTargets[i];
                    ITarget destinationTarget = destinationTargets[i];
                    ITargetData sourceData;
                    ITargetData destinationData;
                    if (this._dirtyTargets.TryGetValue(sourceTarget, out sourceData) == false)
                        continue;
                    if (this._dirtyTargets.ContainsKey(destinationTarget))
                        continue;
                    this.SetTargetDirty(destinationTarget, out destinationData);

                    destinationData.currentEnabled = sourceData.currentEnabled;
                    destinationTarget.enabled = sourceTarget.enabled;
                    destinationTarget.CopyFrom(sourceTarget);

                    List<Material> materials = sourceTarget.sharedMaterials.ToList();
                    foreach (KeyValuePair<Material, MaterialData> materialPair in sourceData.dirtyMaterials)
                    {
                        if (materialPair.Key == null)
                            continue;
                        int index = materials.IndexOf(materialPair.Key);
                        Material destinationMaterial = destinationTarget.materials[index];
                        MaterialData destinationMaterialData;
                        this.SetMaterialDirty(destinationMaterial, out destinationMaterialData, destinationData);

                        if (materialPair.Value.hasRenderQueue)
                        {
                            destinationMaterialData.originalRenderQueue = destinationMaterial.renderQueue;
                            destinationMaterialData.hasRenderQueue = true;
                            destinationMaterial.renderQueue = materialPair.Key.renderQueue;
                        }
                        if (materialPair.Value.hasRenderType)
                        {
                            destinationMaterialData.originalRenderType = destinationMaterial.GetTag("RenderType", false);
                            destinationMaterialData.hasRenderType = true;
                            destinationMaterial.SetOverrideTag("RenderType", materialPair.Key.GetTag("RenderType", false));
                        }

                        foreach (KeyValuePair<string, Color> pair in materialPair.Value.dirtyColorProperties)
                        {
                            destinationMaterialData.dirtyColorProperties.Add(pair.Key, destinationMaterial.GetColor(pair.Key));
                            destinationMaterial.SetColor(pair.Key, materialPair.Key.GetColor(pair.Key));
                        }
                        foreach (KeyValuePair<string, MaterialData.TextureData> pair in materialPair.Value.dirtyTextureProperties)
                        {
                            destinationMaterialData.dirtyTextureProperties.Add(pair.Key, new MaterialData.TextureData()
                            {
                                currentTexturePath = pair.Value.currentTexturePath,
                                originalTexture = destinationMaterial.GetTexture(pair.Key)
                            });
                            Texture t = null;
                            if (string.IsNullOrEmpty(pair.Value.currentTexturePath) == false)
                                t = this.GetTexture(pair.Value.currentTexturePath)?.texture;
                            destinationMaterial.SetTexture(pair.Key, t);
                        }
                        foreach (KeyValuePair<string, Vector2> pair in materialPair.Value.dirtyTextureOffsetProperties)
                        {
                            destinationMaterialData.dirtyTextureOffsetProperties.Add(pair.Key, destinationMaterial.GetTextureOffset(pair.Key));
                            destinationMaterial.SetTextureOffset(pair.Key, materialPair.Key.GetTextureOffset(pair.Key));
                        }
                        foreach (KeyValuePair<string, Vector2> pair in materialPair.Value.dirtyTextureScaleProperties)
                        {
                            destinationMaterialData.dirtyTextureScaleProperties.Add(pair.Key, destinationMaterial.GetTextureScale(pair.Key));
                            destinationMaterial.SetTextureScale(pair.Key, materialPair.Key.GetTextureScale(pair.Key));
                        }
                        foreach (KeyValuePair<string, float> pair in materialPair.Value.dirtyFloatProperties)
                        {
                            destinationMaterialData.dirtyFloatProperties.Add(pair.Key, destinationMaterial.GetFloat(pair.Key));
                            destinationMaterial.SetFloat(pair.Key, materialPair.Key.GetFloat(pair.Key));
                        }

                        foreach (KeyValuePair<string, bool> pair in materialPair.Value.dirtyBooleanProperties)
                        {
                            destinationMaterialData.dirtyBooleanProperties.Add(pair.Key, Mathf.RoundToInt(destinationMaterial.GetFloat(pair.Key)) == 1);
                            destinationMaterial.SetFloat(pair.Key, materialPair.Key.GetFloat(pair.Key));
                        }
                        foreach (KeyValuePair<string, int> pair in materialPair.Value.dirtyEnumProperties)
                        {
                            destinationMaterialData.dirtyEnumProperties.Add(pair.Key, Mathf.RoundToInt(destinationMaterial.GetFloat(pair.Key)));
                            destinationMaterial.SetFloat(pair.Key, materialPair.Key.GetFloat(pair.Key));
                        }
                        foreach (KeyValuePair<string, Vector4> pair in materialPair.Value.dirtyVector4Properties)
                        {
                            destinationMaterialData.dirtyVector4Properties.Add(pair.Key, destinationMaterial.GetVector(pair.Key));
                            destinationMaterial.SetVector(pair.Key, materialPair.Key.GetVector(pair.Key));
                        }
                        foreach (string keyword in materialPair.Value.enabledKeywords)
                        {
                            destinationMaterialData.enabledKeywords.Add(keyword);
                            destinationMaterial.EnableKeyword(keyword);
                        }
                        foreach (string keyword in materialPair.Value.disabledKeywords)
                        {
                            destinationMaterialData.disabledKeywords.Add(keyword);
                            destinationMaterial.DisableKeyword(keyword);
                        }
                    }
                }
            }, 3);
        }

        private string PathReconstruction(string originalPath, long size)
        {
            string fileName = Path.GetFileName(originalPath);
            foreach (string file in Directory.GetFiles(_texturesDir, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            {
                if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase) && (size == -1 || new FileInfo(file).Length == size))
                    return file;
            }
            return originalPath;
        }

        private List<ITarget> GetAllTargets(ObjectCtrlInfo objectCtrlInfo)
        {
            return objectCtrlInfo.guideObject.transformTarget.GetComponentsInChildren<Renderer>(true).Select(r => (ITarget)(RendererTarget)r).Concat(objectCtrlInfo.guideObject.transformTarget.GetComponentsInChildren<Projector>(true).Select(p => (ITarget)(ProjectorTarget)p)).ToList();
        }
        #endregion

        #region Drawers
        private void RenderQueueDrawer(Material displayedMaterial, MaterialData displayedMaterialData)
        {
            GUILayout.BeginHorizontal();
            Color c = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.hasRenderQueue)
                GUI.color = Color.magenta;
            GUILayout.Label("Render Queue", GUILayout.ExpandWidth(false));
            GUI.color = c;

            int newRenderQueue = (int)GUILayout.HorizontalSlider(displayedMaterial.renderQueue, -1, 5000);
            if (newRenderQueue != displayedMaterial.renderQueue)
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                {
                    MaterialData data;
                    this.SetMaterialDirty(material.Key, out data);
                    if (data.hasRenderQueue == false)
                    {
                        data.originalRenderQueue = material.Key.renderQueue;
                        data.hasRenderQueue = true;
                    }
                    material.Key.renderQueue = newRenderQueue;
                }
            }
            string res = GUILayout.TextField(newRenderQueue.ToString(), GUILayout.Width(40));
            if (int.TryParse(res, out newRenderQueue) == false)
                newRenderQueue = displayedMaterial.renderQueue;
            if (newRenderQueue != displayedMaterial.renderQueue)
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                {
                    MaterialData data;
                    this.SetMaterialDirty(material.Key, out data);
                    if (data.hasRenderQueue == false)
                    {
                        data.originalRenderQueue = material.Key.renderQueue;
                        data.hasRenderQueue = true;
                    }
                    material.Key.renderQueue = newRenderQueue;
                }
            }
            if (GUILayout.Button("-1000", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                {
                    MaterialData data;
                    this.SetMaterialDirty(material.Key, out data);
                    if (data.hasRenderQueue == false)
                    {
                        data.originalRenderQueue = material.Key.renderQueue;
                        data.hasRenderQueue = true;
                    }
                    int value = material.Key.renderQueue - 1000;
                    if (value < -1)
                        value = -1;
                    material.Key.renderQueue = value;
                }
            }
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                {
                    if (this._dirtyTargets.TryGetValue(material.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(material.Key, out MaterialData data) &&
                        data.hasRenderQueue)
                    {
                        material.Key.renderQueue = data.originalRenderQueue;
                        data.hasRenderQueue = false;
                        this.TryResetMaterial(material.Key, data, targetData);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void RenderTypeDrawer(Material displayedMaterial, MaterialData displayedMaterialData)
        {
            GUILayout.BeginHorizontal();
            Color c = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.hasRenderType)
                GUI.color = Color.magenta;
            GUILayout.Label("RenderType: " + displayedMaterial.GetTag("RenderType", false));
            GUI.color = c;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                {
                    if (this._dirtyTargets.TryGetValue(material.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(material.Key, out MaterialData data) &&
                        data.hasRenderType)
                    {
                        material.Key.SetOverrideTag("RenderType", data.originalRenderType);
                        data.hasRenderType = false;
                        this.TryResetMaterial(material.Key, data, targetData);
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            this._tagInput = GUILayout.TextField(this._tagInput);
            if (GUILayout.Button("Set RenderType", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._currentTreeNode.selectedMaterials)
                {
                    MaterialData data;
                    this.SetMaterialDirty(material.Key, out data);
                    if (data.hasRenderType == false)
                    {
                        data.originalRenderType = displayedMaterial.GetTag("RenderType", false);
                        data.hasRenderType = true;
                    }
                    material.Key.SetOverrideTag("RenderType", this._tagInput);
                }
                this._tagInput = "";
            }

            GUILayout.EndHorizontal();
        }

        private void ShaderPropertyDrawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            switch (property.type)
            {
                case ShaderProperty.Type.Color:
                    this.ColorDrawer(displayedMaterial, displayedMaterialData, property);
                    break;
                case ShaderProperty.Type.Texture:
                    this.TextureDrawer(displayedMaterial, displayedMaterialData, property);
                    break;
                case ShaderProperty.Type.Float:
                    this.FloatDrawer(displayedMaterial, displayedMaterialData, property);
                    break;
                case ShaderProperty.Type.Boolean:
                    this.BooleanDrawer(displayedMaterial, displayedMaterialData, property);
                    break;
                case ShaderProperty.Type.Enum:
                    this.EnumDrawer(displayedMaterial, displayedMaterialData, property);
                    break;
                case ShaderProperty.Type.Vector4:
                    this.Vector4Drawer(displayedMaterial, displayedMaterialData, property);
                    break;
            }
        }

        private void ColorDrawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            Color c = displayedMaterial.GetColor(property.name);
            GUILayout.BeginHorizontal();
            Color guiColor = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.dirtyColorProperties.ContainsKey(property.name))
                GUI.color = Color.magenta;
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            GUI.color = guiColor;

            if (GUILayout.Button("Hit me senpai <3", GUILayout.ExpandWidth(true)))
            {
#if HONEYSELECT
                Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
                if (Studio.Studio.Instance.colorPaletteCtrl.visible)
                {
                    Studio.Studio.Instance.colorMenu.updateColorFunc = col =>
                    {
                        foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                        {
                            this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                            if (materialData.dirtyColorProperties.ContainsKey(property.name) == false)
                                materialData.dirtyColorProperties.Add(property.name, selectedMaterial.Key.GetColor(property.name));
                            selectedMaterial.Key.SetColor(property.name, col);
                        }
                    };
                    try
                    {
                        Studio.Studio.Instance.colorMenu.SetColor(c, UI_ColorInfo.ControlType.PresetsSample);
                    }
                    catch (Exception)
                    {
                        UnityEngine.Debug.LogError("RendererEditor: Color is HDR, couldn't assign it properly.");
                    }
                }
#elif KOIKATSU
                if (Studio.Studio.Instance.colorPalette.visible)
                    
                    Studio.Studio.Instance.colorPalette.visible = false;
                else
                {
                    try
                    {
                        Studio.Studio.Instance.colorPalette.Setup(property.name, c, (col) =>
                        {
                            foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTarget.selectedMaterials)
                            {
                                this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                                if (materialData.dirtyColorProperties.ContainsKey(property.name) == false)
                                    materialData.dirtyColorProperties.Add(property.name, selectedMaterial.Key.GetColor(property.name));
                                selectedMaterial.Key.SetColor(property.name, col);
                            }
                        }, true);
                    }
                    catch (Exception)
                    {
                        UnityEngine.Debug.LogError("RendererEditor: Color is HDR, couldn't assign it properly.");
                    }
                }
#endif
            }

            Rect layoutRectangle = GUILayoutUtility.GetLastRect();
            layoutRectangle.xMin += 2;
            layoutRectangle.xMax -= 2;
            layoutRectangle.yMin += 2;
            layoutRectangle.yMax -= 2;
            this._simpleTexture.SetPixel(0, 0, c);
            this._simpleTexture.Apply(false);
            GUI.DrawTexture(layoutRectangle, this._simpleTexture, ScaleMode.StretchToFill, true);

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                {
                    if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                        materialData.dirtyColorProperties.ContainsKey(property.name))
                    {
                        selectedMaterial.Key.SetColor(property.name, materialData.dirtyColorProperties[property.name]);
                        materialData.dirtyColorProperties.Remove(property.name);
                        this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void TextureDrawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            Texture texture = displayedMaterial.GetTexture(property.name);
            Vector2 offset = displayedMaterial.GetTextureOffset(property.name);
            Vector2 scale = displayedMaterial.GetTextureScale(property.name);
            GUILayout.BeginHorizontal();
            {
                Color guiColor = GUI.color;
                if (displayedMaterialData != null && displayedMaterialData.dirtyTextureProperties.ContainsKey(property.name))
                    GUI.color = Color.magenta;
                GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
                GUI.color = guiColor;
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(texture == null ? "NULL" : "", GUILayout.Width(90f), GUILayout.Height(90f)))
                {
                    if (this._selectTextureCallback != null)
                        this.CloseTextureWindow();
                    else
                        this._selectTextureCallback = (path, newTexture) =>
                        {
                            foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                            {
                                this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                                MaterialData.TextureData textureData;
                                if (materialData.dirtyTextureProperties.TryGetValue(property.name, out textureData) == false)
                                {
                                    textureData = new MaterialData.TextureData();
                                    textureData.originalTexture = selectedMaterial.Key.GetTexture(property.name);
                                    materialData.dirtyTextureProperties.Add(property.name, textureData);
                                }
                                textureData.currentTexturePath = path;
                                selectedMaterial.Key.SetTexture(property.name, newTexture);
                            }
                        };
                }
                Rect r = GUILayoutUtility.GetLastRect();
                r.xMin += 2;
                r.xMax -= 2;
                r.yMin += 2;
                r.yMax -= 2;
                if (texture != null)
                    GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, true);
                GUILayout.BeginVertical();
                {
                    if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
                    {
                        foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                            if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                                targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                                materialData.dirtyTextureProperties.ContainsKey(property.name))
                            {
                                selectedMaterial.Key.SetTexture(property.name, materialData.dirtyTextureProperties[property.name].originalTexture);
                                materialData.dirtyTextureProperties.Remove(property.name);
                                this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                            }
                    }
                    if (this._currentTreeNode.selectedMaterials.Count == 1 && GUILayout.Button("Dump", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)) && texture != null)
                    {
                        if (Directory.Exists(_dumpDir) == false)
                            Directory.CreateDirectory(_dumpDir);
                        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 24, RenderTextureFormat.ARGB32);
                        RenderTexture cachedActive = RenderTexture.active;
                        RenderTexture.active = rt;
                        Graphics.Blit(texture, rt);
                        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, true);
                        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                        RenderTexture.active = cachedActive;

                        byte[] bytes = tex.EncodeToPNG();
                        KeyValuePair<Material, MaterialInfo> mat = this._currentTreeNode.selectedMaterials.First();
                        string fileName = Path.Combine(_dumpDir, $"{mat.Value.target.name}_{mat.Key.name}_{mat.Value.index}_{texture.name}.png");
                        File.WriteAllBytes(fileName, bytes);

                        this.LoadSingleTexture(fileName);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                Vector2 newOffset = offset;
                Color guiColor = GUI.color;
                if (displayedMaterialData != null && displayedMaterialData.dirtyTextureOffsetProperties.ContainsKey(property.name))
                    GUI.color = Color.magenta;
                GUILayout.Label("Offset", GUILayout.ExpandWidth(false));
                GUI.color = guiColor;

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("X", GUILayout.ExpandWidth(false));
                        newOffset.x = GUILayout.HorizontalSlider(newOffset.x, -1f, 1f);
                        string stringValue = newOffset.x.ToString("0.000");
                        string stringNewValue = GUILayout.TextField(stringValue, GUILayout.Width(40f));
                        if (stringNewValue != stringValue)
                        {
                            float parsedValue;
                            if (float.TryParse(stringNewValue, out parsedValue))
                                newOffset.x = parsedValue;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                        newOffset.y = GUILayout.HorizontalSlider(newOffset.y, -1f, 1f);
                        string stringValue = newOffset.y.ToString("0.000");
                        string stringNewValue = GUILayout.TextField(stringValue, GUILayout.Width(40f));
                        if (stringNewValue != stringValue)
                        {
                            float parsedValue;
                            if (float.TryParse(stringNewValue, out parsedValue))
                                newOffset.y = parsedValue;
                        }
                
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                if (offset != newOffset)
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    {
                        this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                        if (materialData.dirtyTextureOffsetProperties.ContainsKey(property.name) == false)
                            materialData.dirtyTextureOffsetProperties.Add(property.name, selectedMaterial.Key.GetTextureOffset(property.name));
                        selectedMaterial.Key.SetTextureOffset(property.name, newOffset);
                    }
                }
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                        if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                            targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                            materialData.dirtyTextureOffsetProperties.ContainsKey(property.name))
                        {
                            selectedMaterial.Key.SetTextureOffset(property.name, materialData.dirtyTextureOffsetProperties[property.name]);
                            materialData.dirtyTextureOffsetProperties.Remove(property.name);
                            this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                        }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                Vector2 newScale = scale;
                Color guiColor = GUI.color;
                if (displayedMaterialData != null && displayedMaterialData.dirtyTextureScaleProperties.ContainsKey(property.name))
                    GUI.color = Color.magenta;
                GUILayout.Label("Scale", GUILayout.ExpandWidth(false));
                GUI.color = guiColor;
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("X", GUILayout.ExpandWidth(false));
                        newScale.x = GUILayout.HorizontalSlider(newScale.x, 0f, 10f);
                        string stringValue = newScale.x.ToString("0.00");
                        string stringNewValue = GUILayout.TextField(stringValue, GUILayout.Width(40f));
                        if (stringNewValue != stringValue)
                        {
                            float parsedValue;
                            if (float.TryParse(stringNewValue, out parsedValue))
                                newScale.x = parsedValue;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                        newScale.y = GUILayout.HorizontalSlider(newScale.y, 0f, 10f);
                        string stringValue = newScale.y.ToString("0.00");
                        string stringNewValue = GUILayout.TextField(stringValue, GUILayout.Width(40f));
                        if (stringNewValue != stringValue)
                        {
                            float parsedValue;
                            if (float.TryParse(stringNewValue, out parsedValue))
                                newScale.y = parsedValue;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                if (scale != newScale)
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    {
                        this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                        if (materialData.dirtyTextureScaleProperties.ContainsKey(property.name) == false)
                            materialData.dirtyTextureScaleProperties.Add(property.name, selectedMaterial.Key.GetTextureScale(property.name));
                        selectedMaterial.Key.SetTextureScale(property.name, newScale);
                    }
                }
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                        if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                            targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                            materialData.dirtyTextureScaleProperties.ContainsKey(property.name))
                        {
                            selectedMaterial.Key.SetTextureScale(property.name, materialData.dirtyTextureScaleProperties[property.name]);
                            materialData.dirtyTextureScaleProperties.Remove(property.name);
                            this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                        }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void FloatDrawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            float value = displayedMaterial.GetFloat(property.name);
            GUILayout.BeginHorizontal();
            Color guiColor = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.dirtyFloatProperties.ContainsKey(property.name))
                GUI.color = Color.magenta;
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            GUI.color = guiColor;
            Vector2 range;
            if (property.hasFloatRange)
                range = property.floatRange;
            else
                range = new Vector2(0f, 4f);

            float newValue = GUILayout.HorizontalSlider(value, range.x, range.y);

            string valueString = value.ToString("0.000");
            string newValueString = GUILayout.TextField(valueString, 5, GUILayout.Width(50f));

            if (newValueString != valueString)
            {
                float parseResult;
                if (float.TryParse(newValueString, out parseResult))
                    newValue = parseResult;
            }
            if (Mathf.Approximately(value, newValue) == false)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                    if (materialData.dirtyFloatProperties.ContainsKey(property.name) == false)
                        materialData.dirtyFloatProperties.Add(property.name, selectedMaterial.Key.GetFloat(property.name));
                    selectedMaterial.Key.SetFloat(property.name, newValue);
                }
            }

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                    materialData.dirtyFloatProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetFloat(property.name, materialData.dirtyFloatProperties[property.name]);
                    materialData.dirtyFloatProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void BooleanDrawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            bool value = Mathf.Approximately(displayedMaterial.GetFloat(property.name), 1f);
            
            GUILayout.BeginHorizontal();
            Color guiColor = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.dirtyBooleanProperties.ContainsKey(property.name))
                GUI.color = Color.magenta;
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            GUI.color = guiColor;
            bool newValue = GUILayout.Toggle(value, GUIContent.none, GUILayout.ExpandWidth(false));

            if (value != newValue)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                    if (materialData.dirtyBooleanProperties.ContainsKey(property.name) == false)
                        materialData.dirtyBooleanProperties.Add(property.name, Mathf.Approximately(selectedMaterial.Key.GetFloat(property.name), 1f));
                    selectedMaterial.Key.SetFloat(property.name, newValue ? 1f : 0f);
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                    materialData.dirtyBooleanProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetFloat(property.name, materialData.dirtyBooleanProperties[property.name] ? 1f : 0f);
                    materialData.dirtyBooleanProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void EnumDrawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            int key = (displayedMaterial.GetInt(property.name));

            GUILayout.BeginHorizontal();
            Color guiColor = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.dirtyEnumProperties.ContainsKey(property.name))
                GUI.color = Color.magenta;
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            GUI.color = guiColor;
            int newKey = key;
            int i = 0;
            foreach (KeyValuePair<int, string> pair in property.enumValues)
            {
                if (property.enumColumns != 0 && i != 0 && i % property.enumColumns == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Toggle(pair.Key == newKey, pair.Value))
                    newKey = pair.Key;
                ++i;
            }
            if (newKey != key)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                    if (materialData.dirtyEnumProperties.ContainsKey(property.name) == false)
                        materialData.dirtyEnumProperties.Add(property.name, Mathf.RoundToInt(selectedMaterial.Key.GetFloat(property.name)));
                    selectedMaterial.Key.SetInt(property.name, newKey);
                }
            }

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                    materialData.dirtyEnumProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetInt(property.name, materialData.dirtyEnumProperties[property.name]);
                    materialData.dirtyEnumProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void Vector4Drawer(Material displayedMaterial, MaterialData displayedMaterialData, ShaderProperty property)
        {
            Vector4 value = displayedMaterial.GetVector(property.name);

            GUILayout.BeginHorizontal();
            Color guiColor = GUI.color;
            if (displayedMaterialData != null && displayedMaterialData.dirtyVector4Properties.ContainsKey(property.name))
                GUI.color = Color.magenta;
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            GUI.color = guiColor;
            Vector4 newValue = value;

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("W", GUILayout.ExpandWidth(false));
            string stringValue = value.w.ToString("0.00");
            string stringNewValue = GUILayout.TextField(stringValue, GUILayout.MaxWidth(60));
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.w = parsedValue;
            }

            GUILayout.Label("X", GUILayout.ExpandWidth(false));
            stringValue = value.x.ToString("0.00");
            stringNewValue = GUILayout.TextField(stringValue, GUILayout.MaxWidth(60));
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.x = parsedValue;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Y", GUILayout.ExpandWidth(false));
            stringValue = value.y.ToString("0.00");
            stringNewValue = GUILayout.TextField(stringValue, GUILayout.MaxWidth(60));
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.y = parsedValue;
            }

            GUILayout.Label("Z", GUILayout.ExpandWidth(false));
            stringValue = value.z.ToString("0.00");
            stringNewValue = GUILayout.TextField(stringValue, GUILayout.MaxWidth(60));
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.z = parsedValue;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (value != newValue)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                    if (materialData.dirtyVector4Properties.ContainsKey(property.name) == false)
                        materialData.dirtyVector4Properties.Add(property.name, selectedMaterial.Key.GetVector(property.name));
                    selectedMaterial.Key.SetVector(property.name, newValue);
                }
            }

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData) &&
                        materialData.dirtyVector4Properties.ContainsKey(property.name))
                    {
                        selectedMaterial.Key.SetVector(property.name, materialData.dirtyVector4Properties[property.name]);
                        materialData.dirtyVector4Properties.Remove(property.name);
                        this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                    }
            }
            GUILayout.EndHorizontal();
        }

        private void KeywordsDrawer(Material displayedMaterial, MaterialData displayedMaterialData)
        {
            Color guiColor = GUI.color;
            if (displayedMaterialData != null && (displayedMaterialData.enabledKeywords.Count != 0 || displayedMaterialData.disabledKeywords.Count != 0))
                GUI.color = Color.magenta;
            GUILayout.Label("Shader Keywords");
            GUI.color = guiColor;
            GUILayout.BeginHorizontal();
            this._keywordsScroll = GUILayout.BeginScrollView(this._keywordsScroll, GUI.skin.box, GUILayout.Height(60f));
            foreach (string keyword in displayedMaterial.shaderKeywords)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(keyword);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    {
                        if (selectedMaterial.Key.IsKeywordEnabled(keyword))
                        {
                            this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                            bool inEnabled = materialData.enabledKeywords.Contains(keyword);
                            if (inEnabled) //Keyword was added artificially
                                materialData.enabledKeywords.Remove(keyword);
                            else //Keyword here in the first place
                                materialData.disabledKeywords.Add(keyword);
                            selectedMaterial.Key.DisableKeyword(keyword);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                    if (this._dirtyTargets.TryGetValue(selectedMaterial.Value.target, out ITargetData targetData) &&
                        targetData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out MaterialData materialData))
                    {
                        foreach (string enabledKeyword in materialData.enabledKeywords)
                            selectedMaterial.Key.DisableKeyword(enabledKeyword);
                        materialData.enabledKeywords.Clear();
                        foreach (string disabledKeyword in materialData.disabledKeywords)
                            selectedMaterial.Key.EnableKeyword(disabledKeyword);
                        materialData.disabledKeywords.Clear();
                        this.TryResetMaterial(selectedMaterial.Key, materialData, targetData);
                    }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            this._keywordInput = GUILayout.TextField(this._keywordInput);
            if (GUILayout.Button("Add Keyword", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._currentTreeNode.selectedMaterials)
                {
                    if (selectedMaterial.Key.IsKeywordEnabled(this._keywordInput) == false)
                    {
                        this.SetMaterialDirty(selectedMaterial.Key, out MaterialData materialData);
                        if (materialData.disabledKeywords.Contains(this._keywordInput)) //Keyword was here in the first place
                            materialData.disabledKeywords.Remove(this._keywordInput);
                        else //Keyword is added artificially
                            materialData.enabledKeywords.Add(this._keywordInput);
                        selectedMaterial.Key.EnableKeyword(this._keywordInput);
                    }
                }
                this._keywordInput = "";
            }

            GUILayout.EndHorizontal();
        }
        #endregion

        #region Saves
#if KOIKATSU
        private void OnSceneLoad(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["xml"]);
            this.OnSceneLoad(path, doc.FirstChild);
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.WriteStartElement("root");

                xmlWriter.WriteAttributeString("version", RendererEditor.versionNum);

                this.OnSceneSave(path, xmlWriter);

                xmlWriter.WriteEndElement();

                PluginData data = new PluginData();
                data.version = RendererEditor.saveVersion;
                data.data.Add("xml", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }
        }
#endif

        private void OnSceneLoad(string path, XmlNode node)
        {
            this.ExecuteDelayed(() =>
            {
                this.OnSceneLoadGeneric(node, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
            }, 16);
        }

        private void OnSceneImport(string path, XmlNode node)
        {
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            this.ExecuteDelayed(() =>
            {
                this.OnSceneLoadGeneric(node, Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList());
            }, 16);
        }

        private void OnSceneLoadGeneric(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            if (node == null)
                return;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                try
                {
                    switch (childNode.Name)
                    {
                        case "textureSettings":
                            string texturePath = childNode.Attributes["path"].Value;
                            if (!string.IsNullOrEmpty(texturePath))
                            {
                                texturePath = Path.GetFullPath(texturePath);
                                if (File.Exists(texturePath) == false)
                                    continue;
                                this.GetTexture(texturePath, TextureWrapper.TextureSettings.Load(childNode));
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Exception happened while loading item " + childNode.OuterXml + "\n" + e);
                }
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                TargetType type;
                switch (childNode.Name)
                {
                    case "renderer":
                        type = TargetType.Renderer;
                        break;
                    case "projector":
                        type = TargetType.Projector;
                        break;
                    default:
                        continue;
                }
                try
                {
                    int objectIndex = XmlConvert.ToInt32(childNode.Attributes["objectIndex"].Value);
                    if (objectIndex >= dic.Count)
                        continue;
                    ObjectCtrlInfo info = dic[objectIndex].Value;
                    Transform t = info.guideObject.transformTarget;
                    string targetPath = "";
                    ITarget target = null;
                    switch (type)
                    {
                        case TargetType.Renderer:
                            targetPath = childNode.Attributes["rendererPath"].Value;
                            Transform child;
                            child = string.IsNullOrEmpty(targetPath) ? t : t.Find(targetPath);
                            if (child == null)
                                continue;
                            target = (RendererTarget)child.GetComponent<Renderer>();
                            break;
                        case TargetType.Projector:
                            targetPath = childNode.Attributes["projectorPath"].Value;
                            child = string.IsNullOrEmpty(targetPath) ? t : t.Find(targetPath);
                            if (child == null)
                                continue;
                            target = (ProjectorTarget)child.GetComponent<Projector>();
                            break;
                    }
                    if (target.target != null && this.SetTargetDirty(target, out ITargetData targetData))
                    {
                        switch (type)
                        {
                            case TargetType.Renderer:
                                if (childNode.Attributes["rendererEnabled"] != null)
                                {
                                    targetData.currentEnabled = XmlConvert.ToBoolean(childNode.Attributes["rendererEnabled"].Value);
                                    target.enabled = info.treeNodeObject.IsVisible() && targetData.currentEnabled;
                                }
                                else if (childNode.Attributes["enabled"] != null)
                                {
                                    targetData.currentEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    target.enabled = info.treeNodeObject.IsVisible() && targetData.currentEnabled;
                                }
                                else
                                {
                                    targetData.currentEnabled = true;
                                    target.enabled = info.treeNodeObject.IsVisible();
                                }
                                break;
                            case TargetType.Projector:
                                targetData.currentEnabled = XmlConvert.ToBoolean(childNode.Attributes["projectorEnabled"].Value);
                                target.enabled = info.treeNodeObject.IsVisible() && targetData.currentEnabled;
                                break;
                        }

                        target.LoadXml(childNode);

                        foreach (XmlNode grandChildNode in childNode.ChildNodes)
                        {
                            switch (grandChildNode.Name)
                            {
                                case "material":
                                    int index = XmlConvert.ToInt32(grandChildNode.Attributes["index"].Value);
                                    Material mat = target.materials[index];
                                    this.SetMaterialDirty(mat, out MaterialData materialData, targetData);
                                    if (grandChildNode.Attributes["renderQueue"] != null)
                                    {
                                        materialData.originalRenderQueue = mat.renderQueue;
                                        materialData.hasRenderQueue = true;
                                        mat.renderQueue = XmlConvert.ToInt32(grandChildNode.Attributes["renderQueue"].Value);
                                    }

                                    if (grandChildNode.Attributes["renderType"] != null)
                                    {
                                        materialData.originalRenderType = mat.GetTag("RenderType", false);
                                        materialData.hasRenderType = true;
                                        mat.SetOverrideTag("RenderType", grandChildNode.Attributes["renderType"].Value);
                                    }

                                    foreach (XmlNode propertyGroupNode in grandChildNode.ChildNodes)
                                    {
                                        switch (propertyGroupNode.Name)
                                        {
                                            case "colors":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    Color c = Color.black;
                                                    c.r = XmlConvert.ToSingle(property.Attributes["r"].Value);
                                                    c.g = XmlConvert.ToSingle(property.Attributes["g"].Value);
                                                    c.b = XmlConvert.ToSingle(property.Attributes["b"].Value);
                                                    c.a = XmlConvert.ToSingle(property.Attributes["a"].Value);
                                                    materialData.dirtyColorProperties.Add(key, mat.GetColor(key));
                                                    mat.SetColor(key, c);
                                                }
                                                break;
                                            case "textures":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    string texturePath = property.Attributes["path"].Value;
                                                    Texture2D texture = null;
                                                    if (!string.IsNullOrEmpty(texturePath))
                                                    {
                                                        int i = texturePath.IndexOf(_texturesDir, StringComparison.OrdinalIgnoreCase);
                                                        if (i > 0) //Doing this because I fucked up an older version
                                                            texturePath = texturePath.Substring(i);
                                                        texturePath = Path.GetFullPath(texturePath);
                                                        if (File.Exists(texturePath) == false)
                                                        {
                                                            long size = -1;
                                                            if (property.Attributes["size"] != null)
                                                                size = XmlConvert.ToInt64(property.Attributes["size"].Value);
                                                            texturePath = this.PathReconstruction(texturePath, size);
                                                            if (File.Exists(texturePath) == false)
                                                                continue;
                                                        }
                                                        texture = this.GetTexture(texturePath, TextureWrapper.TextureSettings.Load(property))?.texture;
                                                        if (texture == null)
                                                            continue;
                                                        Resources.UnloadUnusedAssets();
                                                        GC.Collect();
                                                    }
                                                    materialData.dirtyTextureProperties.Add(key, new MaterialData.TextureData()
                                                    {
                                                        currentTexturePath = texturePath,
                                                        originalTexture = mat.GetTexture(key)
                                                    });
                                                    mat.SetTexture(key, texture);
                                                }
                                                break;
                                            case "textureOffsets":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    Vector2 offset;
                                                    offset.x = XmlConvert.ToSingle(property.Attributes["x"].Value);
                                                    offset.y = XmlConvert.ToSingle(property.Attributes["y"].Value);
                                                    materialData.dirtyTextureOffsetProperties.Add(key, mat.GetTextureOffset(key));
                                                    mat.SetTextureOffset(key, offset);
                                                }
                                                break;
                                            case "textureScales":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    Vector2 scale;
                                                    scale.x = XmlConvert.ToSingle(property.Attributes["x"].Value);
                                                    scale.y = XmlConvert.ToSingle(property.Attributes["y"].Value);
                                                    materialData.dirtyTextureScaleProperties.Add(key, mat.GetTextureScale(key));
                                                    mat.SetTextureScale(key, scale);
                                                }
                                                break;
                                            case "floats":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    float value = XmlConvert.ToSingle(property.Attributes["value"].Value);
                                                    materialData.dirtyFloatProperties.Add(key, mat.GetFloat(key));
                                                    mat.SetFloat(key, value);
                                                }
                                                break;
                                            case "booleans":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    bool value = XmlConvert.ToBoolean(property.Attributes["value"].Value);
                                                    materialData.dirtyBooleanProperties.Add(key, Mathf.RoundToInt(mat.GetFloat(key)) == 1);
                                                    mat.SetFloat(key, value ? 1 : 0);
                                                }
                                                break;
                                            case "enums":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    int value = XmlConvert.ToInt32(property.Attributes["value"].Value);
                                                    materialData.dirtyEnumProperties.Add(key, Mathf.RoundToInt(mat.GetFloat(key)));
                                                    mat.SetFloat(key, value);
                                                }
                                                break;
                                            case "vector4s":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string key = property.Attributes["key"].Value;
                                                    Vector4 scale;
                                                    scale.w = XmlConvert.ToSingle(property.Attributes["w"].Value);
                                                    scale.x = XmlConvert.ToSingle(property.Attributes["x"].Value);
                                                    scale.y = XmlConvert.ToSingle(property.Attributes["y"].Value);
                                                    scale.z = XmlConvert.ToSingle(property.Attributes["z"].Value);
                                                    materialData.dirtyVector4Properties.Add(key, mat.GetVector(key));
                                                    mat.SetVector(key, scale);
                                                }
                                                break;
                                            case "enabledKeywords":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string keyword = property.Attributes["value"].Value;
                                                    materialData.enabledKeywords.Add(keyword);
                                                    mat.EnableKeyword(keyword);
                                                }
                                                break;
                                            case "disabledKeywords":
                                                foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                {
                                                    string keyword = property.Attributes["value"].Value;
                                                    materialData.disabledKeywords.Add(keyword);
                                                    mat.DisableKeyword(keyword);
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Exception happened while loading item " + childNode.OuterXml + "\n" + e);
                }
            }
        }

        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            HashSet<string> usedTextures = new HashSet<string>();
            foreach (KeyValuePair<ITarget, ITargetData> pair in this._dirtyTargets)
            {
                foreach (KeyValuePair<Material, MaterialData> pair2 in pair.Value.dirtyMaterials)
                {
                    foreach (KeyValuePair<string, MaterialData.TextureData> pair3 in pair2.Value.dirtyTextureProperties)
                    {
                        if (string.IsNullOrEmpty(pair3.Value.currentTexturePath) == false && usedTextures.Contains(pair3.Value.currentTexturePath) == false)
                        {
                            usedTextures.Add(pair3.Value.currentTexturePath);
                            TextureWrapper textureWrapper;
                            if (this._textures.TryGetValue(pair3.Value.currentTexturePath, out textureWrapper))
                            {
                                writer.WriteStartElement("textureSettings");
                                writer.WriteAttributeString("path", string.IsNullOrEmpty(pair3.Value.currentTexturePath) ? pair3.Value.currentTexturePath : pair3.Value.currentTexturePath.Substring(this._currentDirectory.Length + 1));
                                TextureWrapper.TextureSettings.Save(writer, textureWrapper.settings);
                                writer.WriteEndElement();
                            }
                        }
                    }
                }
            }

            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            foreach (KeyValuePair<ITarget, ITargetData> targetPair in this._dirtyTargets)
            {
                int objectIndex = -1;
                try
                {
                    Transform t = targetPair.Key.transform;
                    while ((objectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == t)) == -1)
                        t = t.parent;

                    switch (targetPair.Key.targetType)
                    {
                        case TargetType.Renderer:
                            writer.WriteStartElement("renderer");
                            writer.WriteAttributeString("rendererPath", targetPair.Key.transform.GetPathFrom(t));
                            //writer.WriteAttributeString("enabled", XmlConvert.ToString(rendererPair.Key.enabled));
                            writer.WriteAttributeString("rendererEnabled", XmlConvert.ToString(targetPair.Value.currentEnabled));
                            break;
                        case TargetType.Projector:
                            writer.WriteStartElement("projector");
                            writer.WriteAttributeString("projectorPath", targetPair.Key.transform.GetPathFrom(t));
                            writer.WriteAttributeString("projectorEnabled", XmlConvert.ToString(targetPair.Value.currentEnabled));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    writer.WriteAttributeString("objectIndex", XmlConvert.ToString(objectIndex));

                    targetPair.Key.SaveXml(writer);

                    List<Material> materials = targetPair.Key.sharedMaterials.ToList();
                    foreach (KeyValuePair<Material, MaterialData> materialPair in targetPair.Value.dirtyMaterials)
                    {
                        if (materialPair.Key == null)
                            continue;
                        writer.WriteStartElement("material");
                        writer.WriteAttributeString("index", XmlConvert.ToString(materials.IndexOf(materialPair.Key)));
                        if (materialPair.Value.hasRenderQueue)
                            writer.WriteAttributeString("renderQueue", XmlConvert.ToString(materialPair.Key.renderQueue));

                        if (materialPair.Value.hasRenderType)
                            writer.WriteAttributeString("renderType", materialPair.Key.GetTag("RenderType", false));

                        writer.WriteStartElement("colors");
                        foreach (KeyValuePair<string, Color> pair in materialPair.Value.dirtyColorProperties)
                        {
                            writer.WriteStartElement("color");
                            writer.WriteAttributeString("key", pair.Key);
                            Color c = materialPair.Key.GetColor(pair.Key);
                            writer.WriteAttributeString("r", XmlConvert.ToString(c.r));
                            writer.WriteAttributeString("g", XmlConvert.ToString(c.g));
                            writer.WriteAttributeString("b", XmlConvert.ToString(c.b));
                            writer.WriteAttributeString("a", XmlConvert.ToString(c.a));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("textures");
                        foreach (KeyValuePair<string, MaterialData.TextureData> pair in materialPair.Value.dirtyTextureProperties)
                        {
                            writer.WriteStartElement("texture");
                            writer.WriteAttributeString("key", pair.Key);
                            string p = string.IsNullOrEmpty(pair.Value.currentTexturePath) ? pair.Value.currentTexturePath : pair.Value.currentTexturePath.Substring(this._currentDirectory.Length + 1);
                            writer.WriteAttributeString("path", p);
                            if (string.IsNullOrEmpty(p) == false)
                                writer.WriteAttributeString("size", XmlConvert.ToString(new FileInfo(p).Length));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("textureOffsets");
                        foreach (KeyValuePair<string, Vector2> pair in materialPair.Value.dirtyTextureOffsetProperties)
                        {
                            writer.WriteStartElement("textureOffset");
                            writer.WriteAttributeString("key", pair.Key);
                            Vector2 offset = materialPair.Key.GetTextureOffset(pair.Key);
                            writer.WriteAttributeString("x", XmlConvert.ToString(offset.x));
                            writer.WriteAttributeString("y", XmlConvert.ToString(offset.y));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("textureScales");
                        foreach (KeyValuePair<string, Vector2> pair in materialPair.Value.dirtyTextureScaleProperties)
                        {
                            writer.WriteStartElement("textureScale");
                            writer.WriteAttributeString("key", pair.Key);
                            Vector2 scale = materialPair.Key.GetTextureScale(pair.Key);
                            writer.WriteAttributeString("x", XmlConvert.ToString(scale.x));
                            writer.WriteAttributeString("y", XmlConvert.ToString(scale.y));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("floats");
                        foreach (KeyValuePair<string, float> pair in materialPair.Value.dirtyFloatProperties)
                        {
                            writer.WriteStartElement("float");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("value", XmlConvert.ToString(materialPair.Key.GetFloat(pair.Key)));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("booleans");
                        foreach (KeyValuePair<string, bool> pair in materialPair.Value.dirtyBooleanProperties)
                        {
                            writer.WriteStartElement("boolean");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("value", XmlConvert.ToString(Mathf.RoundToInt(materialPair.Key.GetFloat(pair.Key)) == 1));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("enums");
                        foreach (KeyValuePair<string, int> pair in materialPair.Value.dirtyEnumProperties)
                        {
                            writer.WriteStartElement("enum");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("value", XmlConvert.ToString(Mathf.RoundToInt(materialPair.Key.GetFloat(pair.Key))));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("vector4s");
                        foreach (KeyValuePair<string, Vector4> pair in materialPair.Value.dirtyVector4Properties)
                        {
                            writer.WriteStartElement("vector4");
                            writer.WriteAttributeString("key", pair.Key);
                            Vector4 value = materialPair.Key.GetVector(pair.Key);
                            writer.WriteAttributeString("w", XmlConvert.ToString(value.w));
                            writer.WriteAttributeString("x", XmlConvert.ToString(value.x));
                            writer.WriteAttributeString("y", XmlConvert.ToString(value.y));
                            writer.WriteAttributeString("z", XmlConvert.ToString(value.z));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("enabledKeywords");
                        foreach (string keyword in materialPair.Value.enabledKeywords)
                        {
                            writer.WriteStartElement("keyword");
                            writer.WriteAttributeString("value", keyword);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("disabledKeywords");
                        foreach (string keyword in materialPair.Value.disabledKeywords)
                        {
                            writer.WriteStartElement("keyword");
                            writer.WriteAttributeString("value", keyword);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Exception happened during save with item " + targetPair.Key.transform + " index " + objectIndex + "\n" + e);
                }
            }
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        private class Studio_Duplicate_Patches
        {
            private static void Postfix(Studio.Studio __instance)
            {
                foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                {
                    ObjectCtrlInfo source;
                    if (__instance.dicObjectCtrl.TryGetValue(pair.Value, out source) == false)
                        continue;
                    ObjectCtrlInfo destination;
                    if (__instance.dicObjectCtrl.TryGetValue(pair.Key, out destination) == false)
                        continue;
                    _self.OnDuplicate(source, destination);
                }
            }
        }

        [HarmonyPatch(typeof(SceneInfo), "Import", new [] { typeof(BinaryReader), typeof(Version) } )]
        private static class SceneInfo_Import_Patches //This is here because I fucked up the save format making it impossible to import scenes correctly
        {
            internal static readonly Dictionary<int, int> _newToOldKeys = new Dictionary<int, int>();

            private static void Prefix()
            {
                _newToOldKeys.Clear();
            }
        }

        [HarmonyPatch(typeof(ObjectInfo), "Load", new[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
        private static class ObjectInfo_Load_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (set == false && instructionsList[i + 1].opcode == OpCodes.Pop)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        set = true;
                    }
                }
            }

            private static int Injected(int originalIndex, ObjectInfo __instance)
            {
                SceneInfo_Import_Patches._newToOldKeys.Add(__instance.dicKey, originalIndex);
                return originalIndex; //Doing this so other transpilers can use this value if they want
            }
        }

        [HarmonyPatch(typeof(TreeNodeObject), "SetVisible", new []{ typeof(bool) } )]
        private static class TreeNodeObject_SetVisible_Patches
        {
            private static void Postfix(TreeNodeObject __instance)
            {
                ObjectCtrlInfo info;
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(__instance, out info))
                    return;
                foreach (ITarget target in _self.GetAllTargets(info))
                {
                    if (!_self._dirtyTargets.TryGetValue(target, out ITargetData data))
                        continue;
                    Transform t = target.transform;
                    ObjectCtrlInfo info2;
                    while ((info2 = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == t).Value) == null)
                        t = t.parent;
                    target.enabled = info2.treeNodeObject.IsVisible() && data.currentEnabled;
                }
            }
        }
        #endregion
    }
}