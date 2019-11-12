using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
#if HONEYSELECT
using Harmony;
using IllusionInjector;
using IllusionPlugin;
using Studio;
#elif PLAYHOME
using Harmony;
using IllusionInjector;
using IllusionPlugin;
using Studio;
#elif KOIKATSU
using HarmonyLib;
using Studio;
#elif AISHOUJO
using HarmonyLib;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToolBox.Extensions
{
    public delegate void Action<T1, T2, T3, T4, T5>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

    internal static class HarmonyExtensions
    {
#if HONEYSELECT || PLAYHOME
        public static HarmonyInstance PatchAll(string guid)
#elif KOIKATSU || AISHOUJO
        public static Harmony PatchAll(string guid)
#endif
        {
#if HONEYSELECT || PLAYHOME
            HarmonyInstance harmony = HarmonyInstance.Create(guid);
#elif AISHOUJO || KOIKATSU
            Harmony harmony = new Harmony(guid);
#endif
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            return harmony;
        }

#if HONEYSELECT || PLAYHOME
        public static HarmonyInstance PatchAllSafe(string guid)
#elif KOIKATSU || AISHOUJO
        public static Harmony PatchAllSafe(string guid)
#endif
        {
#if HONEYSELECT || PLAYHOME
            HarmonyInstance harmony = HarmonyInstance.Create(guid);
#elif AISHOUJO || KOIKATSU
            Harmony harmony = new Harmony(guid);
#endif
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                try
                {
#if HONEYSELECT || PLAYHOME
                    List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
#elif AISHOUJO || KOIKATSU
                    List<HarmonyMethod> harmonyMethods = HarmonyMethodExtensions.GetFromType(type);
#endif
                    if (harmonyMethods == null || harmonyMethods.Count <= 0)
                        continue;
                    HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
                    new PatchProcessor(harmony, type, attributes).Patch();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(assembly.FullName + ": Exception occured when patching: " + e);
                }
            }
            return harmony;
        }



        public class Replacement
        {
            public CodeInstruction[] pattern = null;
            public CodeInstruction[] replacer = null;
        }

        public static IEnumerable<CodeInstruction> ReplaceCodePattern(IEnumerable<CodeInstruction> instructions, IList<Replacement> replacements)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList();
            foreach (Replacement replacement in replacements)
            {
                for (int i = 0; i < codeInstructions.Count; i++)
                {
                    int j = 0;
                    while (j < replacement.pattern.Length && i + j < codeInstructions.Count &&
                           CompareCodeInstructions(codeInstructions[i + j], replacement.pattern[j]))
                        ++j;
                    if (j == replacement.pattern.Length)
                    {
                        for (int k = 0; k < replacement.replacer.Length; k++)
                        {
                            int finalIndex = i + k;
                            codeInstructions[finalIndex] = new CodeInstruction(replacement.replacer[k]) { labels = new List<Label>(codeInstructions[finalIndex].labels) };
                        }
                        i += replacement.replacer.Length;
                    }
                }
            }
            return codeInstructions;
        }

        private static bool CompareCodeInstructions(CodeInstruction first, CodeInstruction second)
        {
            return first.opcode == second.opcode && first.operand == second.operand;
        }
    }

    internal static class IMGUIExtensions
    {
        public static void SetGlobalFontSize(int size)
        {
            foreach (GUIStyle style in GUI.skin)
            {
                style.fontSize = size;
            }
            GUI.skin = GUI.skin;
        }

        public static void ResetFontSize()
        {
            SetGlobalFontSize(0);
        }

        public static void HorizontalSliderWithValue(string label, float value, float left, float right, string valueFormat = "", Action<float> onChanged = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            float newValue = GUILayout.HorizontalSlider(value, left, right);
            string valueString = newValue.ToString(valueFormat);
            string newValueString = GUILayout.TextField(valueString, 5, GUILayout.Width(50f));

            if (newValueString != valueString)
            {
                float parseResult;
                if (float.TryParse(newValueString, out parseResult))
                    newValue = parseResult;
            }
            GUILayout.EndHorizontal();

            if (onChanged != null && !Mathf.Approximately(value, newValue))
                onChanged(newValue);
        }
    }

    internal static class MonoBehaviourExtensions
    {
#if HONEYSELECT || PLAYHOME
        private static PluginComponent _pluginComponent;
        private static void CheckPluginComponent()
        {
            if (_pluginComponent == null)
                _pluginComponent = UnityEngine.Object.FindObjectOfType<PluginComponent>();
        }
        public static Coroutine ExecuteDelayed(this IPlugin self, Action action, int framecount = 1)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayed(action, framecount);
        }
        public static Coroutine ExecuteDelayed(this IPlugin self, Action action, float delay, bool timeScaled = true)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayed(action, delay, timeScaled);
        }
        public static Coroutine ExecuteDelayedFixed(this IPlugin self, Action action, int waitCount = 1)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayedFixed(action, waitCount);
        }
        public static Coroutine ExecuteDelayed(this IPlugin self, Func<bool> waitUntil, Action action)
        {
            CheckPluginComponent();
            return _pluginComponent.ExecuteDelayed(waitUntil, action);
        }

        public static Coroutine StartCoroutine(this IPlugin self, IEnumerator routine)
        {
            CheckPluginComponent();
            return _pluginComponent.StartCoroutine(routine);
        }
        public static Coroutine StartCoroutine(this IPlugin self, string methodName)
        {
            CheckPluginComponent();
            return _pluginComponent.StartCoroutine(methodName);
        }
        public static Coroutine StartCoroutine(this IPlugin self, string methodName, object value)
        {
            CheckPluginComponent();
            return _pluginComponent.StartCoroutine(methodName, value);
        }

        public static void StopCoroutine(this IPlugin self, Coroutine routine)
        {
            CheckPluginComponent();
            _pluginComponent.StopCoroutine(routine);
        }
        public static void StopCoroutine(this IPlugin self, IEnumerator routine)
        {
            CheckPluginComponent();
            _pluginComponent.StopCoroutine(routine);
        }
        public static void StopCoroutine(this IPlugin self, string methodName)
        {
            CheckPluginComponent();
            _pluginComponent.StopCoroutine(methodName);
        }
        public static void StopAllCouroutines(this IPlugin self)
        {
            CheckPluginComponent();
            _pluginComponent.StopAllCoroutines();
        }
#endif

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, int frameCount = 1)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(action));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, int frameCount = 1)
        {
            for (int i = 0; i < frameCount; i++)
                yield return null;
            action();
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Action action, float delay, bool timeScaled = true)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(action, delay, timeScaled));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, float delay, bool timeScaled)
        {
            if (timeScaled)
                yield return new WaitForSeconds(delay);
            else
                yield return new WaitForSecondsRealtime(delay);
            action();
        }

        public static Coroutine ExecuteDelayedFixed(this MonoBehaviour self, Action action, int waitCount = 1)
        {
            return self.StartCoroutine(ExecuteDelayedFixed_Routine(action, waitCount));
        }

        private static IEnumerator ExecuteDelayedFixed_Routine(Action action, int waitCount)
        {
            for (int i = 0; i < waitCount; i++)
                yield return new WaitForFixedUpdate();
            action();
        }

        public static Coroutine ExecuteDelayed(this MonoBehaviour self, Func<bool> waitUntil, Action action)
        {
            return self.StartCoroutine(ExecuteDelayed_Routine(waitUntil, action));
        }

        private static IEnumerator ExecuteDelayed_Routine(Func<bool> waitUntil, Action action)
        {
            yield return new WaitUntil(waitUntil);
            action();
        }
    }

    internal static class ReflectionExtensions
    {

        private struct MemberKey
        {
            public readonly Type type;
            public readonly string name;
            private readonly int _hashCode;

            public MemberKey(Type inType, string inName)
            {
                this.type = inType;
                this.name = inName;
                this._hashCode = this.type.GetHashCode() ^ this.name.GetHashCode();
            }

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }

        private static readonly Dictionary<MemberKey, FieldInfo> _fieldCache = new Dictionary<MemberKey, FieldInfo>();
        private static readonly Dictionary<MemberKey, PropertyInfo> _propertyCache = new Dictionary<MemberKey, PropertyInfo>();

        public static void SetPrivateExplicit<T>(this T self, string name, object value)
        {
            MemberKey key = new MemberKey(typeof(T), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            info.SetValue(self, value);
        }
        public static void SetPrivate(this object self, string name, object value)
        {
            MemberKey key = new MemberKey(self.GetType(), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            info.SetValue(self, value);
        }
        public static object GetPrivateExplicit<T>(this T self, string name)
        {
            MemberKey key = new MemberKey(typeof(T), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            return info.GetValue(self);
        }
        public static object GetPrivate(this object self, string name)
        {
            MemberKey key = new MemberKey(self.GetType(), name);
            FieldInfo info;
            if (_fieldCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _fieldCache.Add(key, info);
            }
            return info.GetValue(self);
        }

        public static void SetPrivateProperty(this object self, string name, object value)
        {
            MemberKey key = new MemberKey(self.GetType(), name);
            PropertyInfo info;
            if (_propertyCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetProperty(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _propertyCache.Add(key, info);
            }
            info.SetValue(self, value, null);
        }

        public static object GetPrivateProperty(this object self, string name)
        {
            MemberKey key = new MemberKey(self.GetType(), name);
            PropertyInfo info;
            if (_propertyCache.TryGetValue(key, out info) == false)
            {
                info = key.type.GetProperty(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _propertyCache.Add(key, info);
            }
            return info.GetValue(self, null);
        }

        public static object CallPrivate(this object self, string name, params object[] p)
        {
            return self.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).Invoke(self, p);
        }

        public static object CallPrivate(this Type self, string name, params object[] p)
        {
            return self.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).Invoke(null, p);
        }

        public static void LoadWith<T>(this T to, T from)
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo fi in fields)
            {
                if (fi.FieldType.IsArray)
                {
                    Array arr = (Array)fi.GetValue(from);
                    Array arr2 = Array.CreateInstance(fi.FieldType.GetElementType(), arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                        arr2.SetValue(arr.GetValue(i), i);
                }
                else
                    fi.SetValue(to, fi.GetValue(from));
            }
        }

        public static void ReplaceEventsOf(this object self, object obj)
        {
            foreach (Button b in Resources.FindObjectsOfTypeAll<Button>())
            {
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onClick.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onClick.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
            foreach (Slider b in Resources.FindObjectsOfTypeAll<Slider>())
            {
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
            foreach (InputField b in Resources.FindObjectsOfTypeAll<InputField>())
            {
                for (int i = 0; i < b.onEndEdit.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onEndEdit.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onEndEdit.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
                if (b.onValidateInput != null && ReferenceEquals(b.onValidateInput.Target, obj))
                {
                    b.onValidateInput.SetPrivate("_target", obj);
                }
            }
            foreach (Toggle b in Resources.FindObjectsOfTypeAll<Toggle>())
            {
                for (int i = 0; i < b.onValueChanged.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b.onValueChanged.GetPersistentTarget(i), obj))
                    {
                        IList objects = b.onValueChanged.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }

#if HONEYSELECT
            foreach (UI_OnEnableEvent b in Resources.FindObjectsOfTypeAll<UI_OnEnableEvent>())
            {
                for (int i = 0; i < b._event.GetPersistentEventCount(); ++i)
                {
                    if (ReferenceEquals(b._event.GetPersistentTarget(i), obj))
                    {
                        IList objects = b._event.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                        objects[i].SetPrivate("m_Target", self);
                    }
                }
            }
#endif

            foreach (EventTrigger b in Resources.FindObjectsOfTypeAll<EventTrigger>())
            {
                foreach (EventTrigger.Entry et in b.triggers)
                {
                    for (int i = 0; i < et.callback.GetPersistentEventCount(); ++i)
                    {
                        if (ReferenceEquals(et.callback.GetPersistentTarget(i), obj))
                        {
                            IList objects = et.callback.GetPrivateExplicit<UnityEventBase>("m_PersistentCalls").GetPrivate("m_Calls") as IList;
                            objects[i].SetPrivate("m_Target", self);
                        }
                    }
                }
            }
        }

        public static MethodInfo GetCoroutineMethod(this Type objectType, string name)
        {
            Type t = null;
            name = "+<" + name + ">";
            foreach (Type type in objectType.GetNestedTypes(BindingFlags.NonPublic))
            {
                if (type.FullName.Contains(name))
                {
                    t = type;
                    break;
                }
            }

            if (t != null)
                return t.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
            return null;
        }
    }

    internal static class TransformExtensions
    {
        public static string GetPathFrom(this Transform self, Transform root, bool includeRoot = false)
        {
            if (self == root)
                return "";
            Transform self2 = self;
            StringBuilder path = new StringBuilder(self2.name);
            self2 = self2.parent;
            while (self2 != root)
            {
                path.Insert(0, "/");
                path.Insert(0, self2.name);
                self2 = self2.parent;
            }
            if (self2 != null && includeRoot)
            {
                path.Insert(0, "/");
                path.Insert(0, root.name);
            }
            return path.ToString();
        }

        public static bool IsChildOf(this Transform self, string parent)
        {
            while (self != null)
            {
                if (self.name.Equals(parent))
                    return true;
                self = self.parent;
            }
            return false;
        }

        public static string GetPathFrom(this Transform self, string root, bool includeRoot = false)
        {
            if (self.name.Equals(root))
                return "";
            Transform self2 = self;
            StringBuilder path = new StringBuilder(self2.name);
            self2 = self2.parent;
            while (self2 != null && self2.name.Equals(root) == false)
            {
                path.Insert(0, "/");
                path.Insert(0, self2.name);
                self2 = self2.parent;
            }
            if (self2 != null && includeRoot)
            {
                path.Insert(0, "/");
                path.Insert(0, root);
            }
            return path.ToString();
        }

        public static List<int> GetListPathFrom(this Transform self, Transform root)
        {
            List<int> path = new List<int>();
            Transform self2 = self;
            while (self2 != root)
            {
                path.Add(self2.GetSiblingIndex());
                self2 = self2.parent;
            }
            path.Reverse();
            return path;
        }

        public static Transform Find(this Transform self, List<int> path)
        {
            Transform self2 = self;
            for (int i = 0; i < path.Count; i++)
                self2 = self2.GetChild(path[i]);
            return self2;
        }

        public static Transform FindDescendant(this Transform self, string name)
        {
            if (self.name.Equals(name))
                return self;
            foreach (Transform t in self)
            {
                Transform res = t.FindDescendant(name);
                if (res != null)
                    return res;
            }
            return null;
        }

        public static Transform GetFirstLeaf(this Transform self)
        {
            while (self.childCount != 0)
                self = self.GetChild(0);
            return self;
        }

        public static Transform GetDeepestLeaf(this Transform self)
        {
            int d = -1;
            Transform res = null;
            foreach (Transform transform in self)
            {
                int resD;
                Transform resT = GetDeepestLeaf(transform, 0, out resD);
                if (resD > d)
                {
                    d = resD;
                    res = resT;
                }
            }
            return res;
        }

        private static Transform GetDeepestLeaf(Transform t, int depth, out int resultDepth)
        {
            if (t.childCount == 0)
            {
                resultDepth = depth;
                return t;
            }
            Transform res = null;
            int d = 0;
            foreach (Transform child in t)
            {
                int resD;
                Transform resT = GetDeepestLeaf(child, depth + 1, out resD);
                if (resD > d)
                {
                    d = resD;
                    res = resT;
                }
            }
            resultDepth = d;
            return res;
        }
    }

    internal static class XmlExtensions
    {
        public static XmlNode FindChildNode(this XmlNode self, string name)
        {
            if (self.HasChildNodes == false)
                return null;
            foreach (XmlNode childNode in self.ChildNodes)
                if (childNode.Name.Equals(name))
                    return childNode;
            return null;
        }

        public static int ReadInt(this XmlNode self, string label)
        {
            return XmlConvert.ToInt32(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, int value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static byte ReadByte(this XmlNode self, string label)
        {
            return XmlConvert.ToByte(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, byte value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static bool ReadBool(this XmlNode self, string label)
        {
            return XmlConvert.ToBoolean(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, bool value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static float ReadFloat(this XmlNode self, string label)
        {
            return XmlConvert.ToSingle(self.Attributes[label].Value);
        }

        public static void WriteValue(this XmlTextWriter self, string label, float value)
        {
            self.WriteAttributeString(label, XmlConvert.ToString(value));
        }

        public static Vector3 ReadVector3(this XmlNode self, string prefix)
        {
            return new Vector3(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}X"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Y"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Z"].Value)
                    );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Vector3 value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.x));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.y));
            self.WriteAttributeString($"{prefix}Z", XmlConvert.ToString(value.z));
        }

        public static Quaternion ReadQuaternion(this XmlNode self, string prefix)
        {
            return new Quaternion(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}X"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Y"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}Z"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}W"].Value)
                    );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Quaternion value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.x));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.y));
            self.WriteAttributeString($"{prefix}Z", XmlConvert.ToString(value.z));
            self.WriteAttributeString($"{prefix}W", XmlConvert.ToString(value.w));
        }

        public static Color ReadColor(this XmlNode self, string prefix)
        {
            return new Color(
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}R"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}G"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}B"].Value),
                    XmlConvert.ToSingle(self.Attributes[$"{prefix}A"].Value)
                    );
        }

        public static void WriteValue(this XmlTextWriter self, string prefix, Color value)
        {
            self.WriteAttributeString($"{prefix}X", XmlConvert.ToString(value.r));
            self.WriteAttributeString($"{prefix}Y", XmlConvert.ToString(value.g));
            self.WriteAttributeString($"{prefix}Z", XmlConvert.ToString(value.b));
            self.WriteAttributeString($"{prefix}W", XmlConvert.ToString(value.a));
        }

    }

    internal static class VariousExtensions
    {
        public static void Resize<T>(this List<T> self, int newSize)
        {
            int diff = self.Count - newSize;
            if (diff < 0)
                while (self.Count != newSize)
                    self.Add(default(T));
            else if (diff > 0)
                while (self.Count != newSize)
                    self.RemoveRange(newSize, diff);
        }

        public static int IndexOf<T>(this T[] self, T obj)
        {
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i].Equals(obj))
                    return i;
            }
            return -1;
        }

#if HONEYSELECT || PLAYHOME || KOIKATSU
        public static bool IsVisible(this TreeNodeObject self)
        {
            if (self.parent != null)
                return self.visible && self.parent.IsVisible();
            return self.visible;
        }
#endif

        public static int LastIndexOf(this byte[] self, byte[] neddle)
        {
            int limit = neddle.Length - 1;
            for (int i = self.Length - 1; i > limit; i--)
            {
                int j;
                int i2 = i;
                for (j = neddle.Length - 1; j >= 0; --j)
                {
                    if (self[i2] != neddle[j])
                        break;
                    --i2;
                }
                if (j == -1)
                    return i2 + 1;
            }
            return -1;
        }

#if KOIKATSU || AISHOUJO
        private static MethodInfo _initTransforms = null;
        public static void InitTransforms(this DynamicBone self)
        {
            if (_initTransforms == null)
                _initTransforms = self.GetType().GetMethod("InitTransforms", AccessTools.all);
            _initTransforms.Invoke(self, null);
        }

        private static MethodInfo _setupParticles = null;
        public static void SetupParticles(this DynamicBone self)
        {
            if (_setupParticles == null)
                _setupParticles = self.GetType().GetMethod("SetupParticles", AccessTools.all);
            _setupParticles.Invoke(self, null);
        }
#endif
    }

    public class HashedPair<T, T2>
    {
        public readonly T key;
        public readonly T2 value;

        private readonly int _hashCode;

        public HashedPair(T key, T2 value)
        {
            this.key = key;
            this.value = value;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (this.key != null ? this.key.GetHashCode() : 0);
                this._hashCode = hash * 31 + (this.value != null ? this.value.GetHashCode() : 0);
            }
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }
    }
}
