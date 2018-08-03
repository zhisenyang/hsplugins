using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Vectrosity;

namespace RendererEditor
{
    public static class Extensions
    {
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

        public static void ExecuteDelayed(this MonoBehaviour self, Action action, int waitCount = 1)
        {
            self.StartCoroutine(ExecuteDelayed_Routine(action, waitCount));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, int waitCount)
        {
            for (int i = 0; i < waitCount; ++i)
                yield return null;
            action();
        }

        public static void SetPoints(this VectorLine self, params Vector3[] points)
        {
            for (int i = 0; i < self.points3.Count; i++)
                self.points3[i] = points[i];
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
    }
}
