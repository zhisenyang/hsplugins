using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using Studio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HSUS
{
    [HarmonyPatch(typeof(GuideInput), "OnEndEditScale", new []{typeof(int)})]
    public class GuideInput_OnEndEditScale_Patches
    {
        private static FieldInfo _hashSelectObject;
        private static FieldInfo _inputScale;

        public static bool Prepare()
        {
            if (HSUS.self.improvedTransformOperations)
            {
                _hashSelectObject = typeof(GuideInput).GetField("hashSelectObject", BindingFlags.Instance | BindingFlags.NonPublic);
                _inputScale = typeof(GuideInput).GetField("inputScale", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return HSUS.self.improvedTransformOperations;
        }

        public static bool Prefix(GuideInput __instance, int _target)
        {
            HashSet<GuideObject> hashSelectObject = (HashSet<GuideObject>)_hashSelectObject.GetValue(__instance);
            InputField[] inputScale = (InputField[])_inputScale.GetValue(__instance);
            if (hashSelectObject.Count == 0)
            {
                return false;
            }
            float num = InputToFloat(inputScale[_target]);
            List<GuideCommand.EqualsInfo> list = new List<GuideCommand.EqualsInfo>();
            foreach (GuideObject guideObject in hashSelectObject)
            {
                if (guideObject.enableScale)
                {
                    Vector3 scale = guideObject.changeAmount.scale;
                    if (scale[_target] != num)
                    {
                        scale[_target] = num;
                        Vector3 scale2 = guideObject.changeAmount.scale;
                        guideObject.changeAmount.scale = scale;
                        list.Add(new GuideCommand.EqualsInfo
                        {
                            dicKey = guideObject.dicKey,
                            oldValue = scale2,
                            newValue = scale
                        });
                    }
                }
            }
            if (!list.IsNullOrEmpty())
            {
                UndoRedoManager.Instance.Push(new GuideCommand.ScaleEqualsCommand(list.ToArray()));
            }

            GuideObject guideObj = hashSelectObject.ElementAtOrDefault(0);
            Vector3 vector = (!guideObj) ? Vector3.zero : guideObj.changeAmount.scale;
            bool[] array = new bool[]
            {
                true,
                true,
                true
            };
            foreach (GuideObject guideObject2 in hashSelectObject)
            {
                Vector3 scale = guideObject2.changeAmount.scale;
                for (int i = 0; i < 3; i++)
                {
                    array[i] = (vector[i] == scale[i]);
                }
            }
            for (int j = 0; j < 3; j++)
            {
                inputScale[j].text = ((!array[j]) ? "-" : vector[j].ToString("0.000"));
            }

            return false;
        }

        private static float InputToFloat(InputField _input)
        {
            return (!float.TryParse(_input.text, out float num)) ? 0f : num;
        }
    }

    [HarmonyPatch(typeof(GuideScale), "OnDrag", new[] {typeof(PointerEventData) })]
    public class GuideScale_OnDrag_Patches
    {
        private static FieldInfo _prevPos;
        private static FieldInfo _speed;
        private static FieldInfo _dicChangeAmount;

        public static bool Prepare()
        {
            if (HSUS.self.improvedTransformOperations)
            {
                _prevPos = typeof(GuideScale).GetField("prevPos", BindingFlags.Instance | BindingFlags.NonPublic);
                _speed = typeof(GuideScale).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic);
                _dicChangeAmount = typeof(GuideScale).GetField("dicChangeAmount", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return HSUS.self.improvedTransformOperations;
        }

        public static bool Prefix(GuideScale __instance, PointerEventData _eventData)
        {
            Vector3 b = Vector3.zero;
            if (__instance.axis == GuideScale.ScaleAxis.XYZ)
            {
                Vector2 delta = _eventData.delta;
                float d = (delta.x + delta.y) * (float)_speed.GetValue(__instance);
                b = Vector3.one * d;
            }
            else
            {
                b = AxisPos(__instance, _eventData.position) - AxisPos(__instance, (Vector2)_prevPos.GetValue(__instance));
                _prevPos.SetValue(__instance, _eventData.position);
            }
            foreach (KeyValuePair<int, ChangeAmount> keyValuePair in (Dictionary<int, ChangeAmount>)_dicChangeAmount.GetValue(__instance))
            {
                Vector3 vector = keyValuePair.Value.scale;
                vector += b;
                keyValuePair.Value.scale = vector;
            }
            return false;
        }

        private static Vector3 AxisPos(GuideScale __instance, Vector2 _screenPos)
        {
            Vector3 position = __instance.transform.position;
            Plane plane = new Plane(Camera.main.transform.forward * -1f, position);
            Ray ray = RectTransformUtility.ScreenPointToRay(Camera.main, _screenPos);
            float distance = 0f;
            Vector3 a = (!plane.Raycast(ray, out distance)) ? position : ray.GetPoint(distance);
            Vector3 vector = a - position;
            Vector3 onNormal = __instance.transform.up;
            switch (__instance.axis)
            {
                case GuideScale.ScaleAxis.X:
                    onNormal = Vector3.right;
                    break;
                case GuideScale.ScaleAxis.Y:
                    onNormal = Vector3.up;
                    break;
                case GuideScale.ScaleAxis.Z:
                    onNormal = Vector3.forward;
                    break;
            }
            return Vector3.Project(vector, onNormal);
        }
    }
}
