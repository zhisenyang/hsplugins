using System.Collections.Generic;
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
        public static bool Prepare()
        {
            return HSUS.self.improvedTransformOperations;
        }

        public static bool Prefix(GuideInput __instance, int _target)
        {
            HashSet<GuideObject> hashSelecterObject = (HashSet<GuideObject>)__instance.GetPrivate("hashSelectObject");
            InputField[] inputScale = (InputField[])__instance.GetPrivate("inputScale");
            if (hashSelecterObject.Count == 0)
            {
                return false;
            }
            float num = __instance.InputToFloat(inputScale[_target]);
            List<GuideCommand.EqualsInfo> list = new List<GuideCommand.EqualsInfo>();
            foreach (GuideObject guideObject in hashSelecterObject)
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
            if (!list.IsNullOrEmpty<GuideCommand.EqualsInfo>())
            {
                UndoRedoManager.Instance.Push(new GuideCommand.ScaleEqualsCommand(list.ToArray()));
            }
            __instance.SetInputTextScale(Vector3.zero);
            return false;
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
                b = __instance.AxisPos(_eventData.position) - __instance.AxisPos((Vector2)_prevPos.GetValue(__instance));
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
    }
}
