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
                MainWindow.self.OnDuplicate(source, destination);
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
}
