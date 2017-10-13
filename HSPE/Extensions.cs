using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using JetBrains.Annotations;
using UnityEngine;

public static class Extensions
{
    public static void SetPrivate<T>(this T self, string name, object value)
    {
        typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).SetValue(self, value);
    }
    public static void SetPrivate(this object self, string name, object value)
    {
        self.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).SetValue(self, value);
    }
    public static void SetPrivateProperty<T>(this T self, string name, object value)
    {
        typeof(T).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).SetValue(self, value, null);
    }
    public static object GetPrivate<T>(this T self, string name)
    {
        return typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).GetValue(self);
    }
    public static object GetPrivate(this object self, string name)
    {
        return self.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).GetValue(self);
    }
    public static object GetPrivateProperty<T>(this T self, string name)
    {
        return typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).GetValue(self);
    }
    public static object CallPrivate<T>(this T self, string name, params object[] p)
    {
        return typeof(T).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Invoke(self, p);
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

    public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        self.anchorMin = anchorMin;
        self.anchorMax = anchorMax;
        self.offsetMin = offsetMin;
        self.offsetMax = offsetMax;
    }

    public static float DirectionalAngle(Vector3 from, Vector3 to, Vector3 up)
    {
        Vector3 f = Vector3.ProjectOnPlane(from, up);
        Vector3 t = Vector3.ProjectOnPlane(to, up);

        Quaternion toZ = Quaternion.FromToRotation(up, Vector3.up);
        f = toZ * f;
        t = toZ * t;

        Quaternion fromTo = Quaternion.FromToRotation(f, t);

        return fromTo.eulerAngles.y;
    }

    public static float DirectionalAngleSigned(Vector3 from, Vector3 to, Vector3 up)
    {
        Vector3 f = Vector3.ProjectOnPlane(from, up);
        Vector3 t = Vector3.ProjectOnPlane(to, up);
        return Quaternion.Angle(Quaternion.LookRotation(f, up), Quaternion.LookRotation(t, up));
    }

    public static float NormalizeAngle(float angle)
    {
        angle = (angle + 360) % 360;
        if (angle > 180)
            angle -= 360;
        return angle;
    }

    [CanBeNull]
    public static XmlNode FindChildNode([NotNull] this XmlNode self, string name)
    {
        if (self.HasChildNodes == false)
            return null;
        foreach (XmlNode chilNode in self.ChildNodes)
            if (chilNode.Name.Equals(name))
                return chilNode;
        return null;
    }

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

    public static string GetPathFrom(this Transform self, Transform root)
    {
        Transform self2 = self;
        string path = self2.name;
        self2 = self2.parent;
        while (self2 != root)
        {
            path = self2.name + "/" + path;
            self2 = self2.parent;
        }
        return path;
    }

    public static string GetChosenScenePath(this Studio.SceneLoadScene self)
    {
        List<string> listPath = (List<string>)typeof(Studio.SceneLoadScene).GetField("listPath", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self);
        return listPath[(int)typeof(Studio.SceneLoadScene).GetField("select", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self)];
    }

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

    public static Coroutine ExecuteDelayedFixed(this MonoBehaviour self, Action action)
    {
        return self.StartCoroutine(ExecuteDelayedFixed_Routine(action));
    }

    private static IEnumerator ExecuteDelayedFixed_Routine(Action action)
    {
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