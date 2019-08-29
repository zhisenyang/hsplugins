using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ChaCustom;
using UnityEngine;

namespace MoreAccessoriesKOI
{
    internal static class BackwardCompatibility
    {
        private static object _customHistory_Instance = null;
        private static MethodInfo _customHistory_Add1 = null;
        private static MethodInfo _customHistory_Add2 = null;
        private static MethodInfo _customHistory_Add3 = null;
        private static MethodInfo _customHistory_Add5 = null;

        private static void CheckInstance()
        {
            if (_customHistory_Instance == null)
            {
                Type t = Type.GetType("ChaCustom.CustomHistory,Assembly-CSharp");
                _customHistory_Instance = t.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            }
        }

        public static void CustomHistory_Instance_Add1(ChaControl instanceChaCtrl, Func<bool> updateAccessoryMoveAllFromInfo)
        {
            CheckInstance();
            if (_customHistory_Add1 == null)
                _customHistory_Add1 = _customHistory_Instance.GetType().GetMethod("Add1", BindingFlags.Public | BindingFlags.Instance);
            _customHistory_Add1.Invoke(_customHistory_Instance, new object[] { instanceChaCtrl, updateAccessoryMoveAllFromInfo });
        }

        public static void CustomHistory_Instance_Add2(ChaControl instanceChaCtrl, Func<bool, bool> funcUpdateAcsColor, bool b)
        {
            CheckInstance();
            if (_customHistory_Add2 == null)
                _customHistory_Add2 = _customHistory_Instance.GetType().GetMethod("Add2", BindingFlags.Public | BindingFlags.Instance);
            _customHistory_Add2.Invoke(_customHistory_Instance, new object[] { instanceChaCtrl, funcUpdateAcsColor, b });
        }

        internal static void CustomHistory_Instance_Add3(ChaControl instanceChaCtrl, Func<bool, bool, bool> funcUpdateAccessory, bool b, bool b1)
        {
            CheckInstance();
            if (_customHistory_Add3 == null)
                _customHistory_Add3 = _customHistory_Instance.GetType().GetMethod("Add3", BindingFlags.Public | BindingFlags.Instance);
            _customHistory_Add3.Invoke(_customHistory_Instance, new object[] {instanceChaCtrl, funcUpdateAccessory, b, b1});
        }

        internal static void CustomHistory_Instance_Add5(ChaControl chaCtrl, Func<bool, bool, bool, bool, bool> reload, bool v1, bool v2, bool v3, bool v4)
        {
            CheckInstance();
            if (_customHistory_Add5 == null)
                _customHistory_Add5 = _customHistory_Instance.GetType().GetMethod("Add5", BindingFlags.Public | BindingFlags.Instance);
            _customHistory_Add5.Invoke(_customHistory_Instance, new object[] { chaCtrl, reload, v1, v2, v3, v4 });
        }

        private static MethodInfo _cvsColor_Setup;

        internal static void Setup(this CvsColor self, string winTitle, CvsColor.ConnectColorKind kind, Color color, Action<Color> _actUpdateColor, Action _actUpdateHistory, bool _useAlpha)
        {
            if (_cvsColor_Setup == null)
                _cvsColor_Setup = self.GetType().GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance);
            if (MoreAccessories._self._hasDarkness)
                _cvsColor_Setup.Invoke(self, new object[] {winTitle, kind, color, _actUpdateColor, _useAlpha});
            else
                _cvsColor_Setup.Invoke(self, new object[] {winTitle, kind, color, _actUpdateColor, _actUpdateHistory, _useAlpha});
        }

        internal static Action UpdateAcsColorHistory(this CvsAccessory __instance)
        {
            if (MoreAccessories._self._hasDarkness)
                return null;
            MethodInfo methodInfo = __instance.GetType().GetMethod("UpdateAcsColorHistory", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
                return (Action)Delegate.CreateDelegate(typeof(Action), __instance, methodInfo);
            return null;
        }

        private static MethodInfo _cvsAccessory_UpdateAcsMoveHistory;
        internal static void UpdateAcsMoveHistory(this CvsAccessory self)
        {
            if (_cvsAccessory_UpdateAcsMoveHistory == null)
                _cvsAccessory_UpdateAcsMoveHistory = self.GetType().GetMethod("UpdateAcsMoveHistory", BindingFlags.Public | BindingFlags.Instance);
            _cvsAccessory_UpdateAcsMoveHistory.Invoke(self, null);
        }
    }
}
