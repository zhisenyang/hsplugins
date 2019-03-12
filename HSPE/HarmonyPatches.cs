using System;
using System.Collections.Generic;
using System.IO;
using Harmony;
using Studio;
using ToolBox;
using UnityEngine;

namespace HSPE
{
    [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
    public class Studio_Duplicate_Patches
    {
        internal static readonly List<ObjectInfo> _sources = new List<ObjectInfo>();
        internal static readonly List<ObjectInfo> _destinations = new List<ObjectInfo>();
        internal static bool _duplicateCalled = false;

        public static void Prefix()
        {
            _duplicateCalled = true;
        }

        public static void Postfix(Studio.Studio __instance)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                ObjectCtrlInfo source = __instance.dicObjectCtrl[_sources[i].dicKey];
                ObjectCtrlInfo destination = __instance.dicObjectCtrl[_destinations[i].dicKey];
                MainWindow._self.OnDuplicate(source, destination);
            }
            _sources.Clear();
            _destinations.Clear();
            _duplicateCalled = false;
        }
    }

    [HarmonyPatch(typeof(ObjectInfo), "Save", new []{typeof(BinaryWriter), typeof(Version)})]
    internal static class ObjectInfo_Save_Patches
    {
        private static void Postfix(ObjectInfo __instance)
        {
            if (Studio_Duplicate_Patches._duplicateCalled && (__instance is OICharInfo || __instance is OIItemInfo))
                Studio_Duplicate_Patches._sources.Add(__instance);
        }
    }

    [HarmonyPatch(typeof(ObjectInfo), "Load", new []{typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool)})]
    internal static class ObjectInfo_Load_Patches
    {
        private static void Postfix(ObjectInfo __instance)
        {
            if (Studio_Duplicate_Patches._duplicateCalled && (__instance is OICharInfo || __instance is OIItemInfo))
                Studio_Duplicate_Patches._destinations.Add(__instance);
        }
    }


    [HarmonyPatch(typeof(OCIChar), "LoadClothesFile", new[] { typeof(string) })]
    internal static class OCIChar_LoadClothesFile_Patches
    {
        public static event Action<OCIChar> onLoadClothesFile;
        public static void Postfix(OCIChar __instance, string _path)
        {
            if (onLoadClothesFile != null)
                onLoadClothesFile(__instance);
        }
    }

    [HarmonyPatch(typeof(OCIChar), "ChangeChara", new[] { typeof(string) })]
    internal static class OCIChar_ChangeChara_Patches
    {
        public static event Action<OCIChar> onChangeChara;
        public static void Postfix(OCIChar __instance, string _path)
        {
            if (onChangeChara != null)
                onChangeChara(__instance);
        }
    }

#if HONEYSELECT
    [HarmonyPatch(typeof(OCIChar), "SetCoordinateInfo", new[] { typeof(CharDefine.CoordinateType), typeof(bool) })]
#elif KOIKATSU
        [HarmonyPatch(typeof(OCIChar), "SetCoordinateInfo", new[] {typeof(ChaFileDefine.CoordinateType), typeof(bool) })]        
#endif
    internal static class OCIChar_SetCoordinateInfo_Patches
    {
#if HONEYSELECT
        public static event Action<OCIChar, CharDefine.CoordinateType, bool> onSetCoordinateInfo;
        public static void Postfix(OCIChar __instance, CharDefine.CoordinateType _type, bool _force)
#elif KOIKATSU
            public static event Action<OCIChar, ChaFileDefine.CoordinateType, bool> onSetCoordinateInfo;
            public static void Postfix(OCIChar __instance, ChaFileDefine.CoordinateType _type, bool _force)
#endif
        {
            if (onSetCoordinateInfo != null)
                onSetCoordinateInfo(__instance, _type, _force);
        }
    }

}
