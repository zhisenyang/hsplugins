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
}
