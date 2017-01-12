using System.Collections.Generic;
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
}