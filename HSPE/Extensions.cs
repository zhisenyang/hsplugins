using System;
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
}