using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using IllusionUtility.SetUtility;
using Studio;
using UILib;
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

#if HONEYSELECT
    [HarmonyPatch(typeof(CharClothes), "SetAccessoryScl", new[] {typeof(int), typeof(float), typeof(bool), typeof(int)})]
    public class CharClothes_SetAccessoryScl_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.improvedTransformOperations;
        }

        public static bool Prefix(CharClothes __instance, ref bool __result, int slotNo, float value, bool _add, int flags, CharInfo ___chaInfo, CharFileInfoClothes ___clothesInfo)
        {
            if (!MathfEx.RangeEqualOn(0, slotNo, 9))
            {
                __result = false;
                return false;
            }
            GameObject gameObject = ___chaInfo.chaBody.objAcsMove[slotNo];
            if (null == gameObject)
            {
                __result = false;
                return false;
            }
            if ((flags & 1) != 0)
            {
                float num = ((!_add) ? 0f : ___clothesInfo.accessory[slotNo].addScl.x) + value;
                ___clothesInfo.accessory[slotNo].addScl.x = num;
            }
            if ((flags & 2) != 0)
            {
                float num2 = ((!_add) ? 0f : ___clothesInfo.accessory[slotNo].addScl.y) + value;
                ___clothesInfo.accessory[slotNo].addScl.y = num2;
            }
            if ((flags & 4) != 0)
            {
                float num3 = ((!_add) ? 0f : ___clothesInfo.accessory[slotNo].addScl.z) + value;
                ___clothesInfo.accessory[slotNo].addScl.z = num3;
            }
            gameObject.transform.SetLocalScale(___clothesInfo.accessory[slotNo].addScl.x, ___clothesInfo.accessory[slotNo].addScl.y, ___clothesInfo.accessory[slotNo].addScl.z);
            __result = true;
            return false;
        }
    }
#endif

    public class TransformOperations : MonoBehaviour
    {
        private Button _copyTransform;
        private Button _pasteTransform;
        private Button _resetTransform;
        private int _lastObjectCount = 0;
        private HashSet<GuideObject> _hashSelectObject;
        private bool _clipboardEmpty = true;
        private Vector3 _savedPosition;
        private Vector3 _savedRotation;
        private Vector3 _savedScale;

        void Awake()
        {
            RectTransform guideInput = this.transform.Find("Guide Input") as RectTransform;
            Image container = UIUtility.CreatePanel("Additional Operations", this.transform);
            container.rectTransform.SetRect(Vector2.zero, Vector2.zero, guideInput.offsetMin + new Vector2(4 + guideInput.rect.width, -1f), guideInput.offsetMin + new Vector2(5 + guideInput.rect.width + 105, guideInput.rect.height + 1f));
            container.color = new Color32(59, 58, 56, 167);
            container.sprite = UIUtility.resources.inputField;

            for (int i = 0; i < this.transform.childCount; i++)
            {
                RectTransform rt = this.transform.GetChild(i) as RectTransform;
                if (rt != guideInput && rt != container.rectTransform)
                    rt.anchoredPosition += new Vector2(105, 0f);
            }
            Sprite background = null;
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                switch (sprite.name)
                {
                    case "sp_sn_12_00_01":
                        background = sprite;
                        goto DOUBLEBREAK;
                }
            }
            DOUBLEBREAK:
            this._copyTransform = UIUtility.CreateButton("Copy Transform", container.transform, "Copy Transform");
            this._copyTransform.transform.SetRect(new Vector2(0f, 0.666f), Vector2.one, new Vector2(5f, 1f), new Vector2(-5f, -5f));
            ((Image)this._copyTransform.targetGraphic).sprite = background;
            Text t = this._copyTransform.GetComponentInChildren<Text>();
            t.color = Color.white;
            t.resizeTextForBestFit = false;
            t.fontSize = 10;
            t.rectTransform.SetRect();
            this._copyTransform.onClick.AddListener(this.CopyTransform);

            this._pasteTransform = UIUtility.CreateButton("Paste Transform", container.transform, "Paste Transform");
            this._pasteTransform.transform.SetRect(new Vector2(0f, 0.333f), new Vector2(1f, 0.666f), new Vector2(5f, 2f), new Vector2(-5f, -2f));
            ((Image)this._pasteTransform.targetGraphic).sprite = background;
            t = this._pasteTransform.GetComponentInChildren<Text>();
            t.color = Color.white;
            t.resizeTextForBestFit = false;
            t.fontSize = 10;
            t.rectTransform.SetRect();
            this._pasteTransform.onClick.AddListener(this.PasteTransform);

            this._resetTransform = UIUtility.CreateButton("Reset Transform", container.transform, "Reset Transform");
            this._resetTransform.transform.SetRect(Vector2.zero, new Vector2(1f, 0.333f), new Vector2(5f, 5f), new Vector2(-5f, -1f));
            ((Image)this._resetTransform.targetGraphic).sprite = background;
            ((Image)this._resetTransform.targetGraphic).color = Color.Lerp(Color.red, Color.white, 0.3f); 
            t = this._resetTransform.GetComponentInChildren<Text>();
            t.color = Color.white;
            t.resizeTextForBestFit = false;
            t.fontSize = 10;
            t.rectTransform.SetRect();
            this._resetTransform.onClick.AddListener(this.ResetTransform);

            this._hashSelectObject = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");
        }

        void Update()
        {
            if (this._lastObjectCount != this._hashSelectObject.Count)
                this.UpdateButtonsVisibility();
            this._lastObjectCount = this._hashSelectObject.Count;
        }

        private void UpdateButtonsVisibility()
        {
            this._copyTransform.interactable = this._hashSelectObject.Count == 1;
            this._pasteTransform.interactable = this._hashSelectObject.Count > 0 && this._clipboardEmpty == false;
            this._resetTransform.interactable = this._hashSelectObject.Count > 0;
        }

        private void CopyTransform()
        {
            GuideObject source = this._hashSelectObject.First();
            this._savedPosition = source.changeAmount.pos;
            this._savedRotation = source.changeAmount.rot;
            this._savedScale = source.changeAmount.scale;
            this._clipboardEmpty = false;
            this.UpdateButtonsVisibility();
        }

        private void PasteTransform()
        {
            if (this._clipboardEmpty)
                return;
            this.SetValues(this._savedPosition, this._savedRotation, this._savedScale);
        }

        private void ResetTransform()
        {
            this.SetValues(Vector3.zero, Vector3.zero, Vector3.one);
        }

        private void SetValues(Vector3 pos, Vector3 rot, Vector3 scale)
        {
            List<GuideCommand.EqualsInfo> moveChangeAmountInfo = new List<GuideCommand.EqualsInfo>();
            List<GuideCommand.EqualsInfo> rotateChangeAmountInfo = new List<GuideCommand.EqualsInfo>();
            List<GuideCommand.EqualsInfo> scaleChangeAmountInfo = new List<GuideCommand.EqualsInfo>();

            foreach (GuideObject guideObject in this._hashSelectObject)
            {
                if (guideObject.enablePos)
                {
                    Vector3 oldPosValue = guideObject.changeAmount.pos;
                    guideObject.changeAmount.pos = pos;
                    moveChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                    {
                        dicKey = guideObject.dicKey,
                        oldValue = oldPosValue,
                        newValue = guideObject.changeAmount.pos
                    });
                }
                if (guideObject.enableRot)
                {
                    Vector3 oldRotValue = guideObject.changeAmount.rot;
                    guideObject.changeAmount.rot = rot;
                    rotateChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                    {
                        dicKey = guideObject.dicKey,
                        oldValue = oldRotValue,
                        newValue = guideObject.changeAmount.rot
                    });
                }
                if (guideObject.enableScale)
                {
                    Vector3 oldScaleValue = guideObject.changeAmount.scale;
                    guideObject.changeAmount.scale = scale;
                    scaleChangeAmountInfo.Add(new GuideCommand.EqualsInfo()
                    {
                        dicKey = guideObject.dicKey,
                        oldValue = oldScaleValue,
                        newValue = guideObject.changeAmount.scale
                    });
                }
            }
            UndoRedoManager.Instance.Push(new TransformEqualsCommand(moveChangeAmountInfo.ToArray(), rotateChangeAmountInfo.ToArray(), scaleChangeAmountInfo.ToArray()));
        }
    }

    public class TransformEqualsCommand : ICommand
    {
        private readonly Studio.GuideCommand.EqualsInfo[] _moveChangeAmountInfo;
        private readonly Studio.GuideCommand.EqualsInfo[] _rotateChangeAmountInfo;
        private readonly Studio.GuideCommand.EqualsInfo[] _scaleChangeAmountInfo;

        public TransformEqualsCommand(GuideCommand.EqualsInfo[] moveChangeAmountInfo, GuideCommand.EqualsInfo[] rotateChangeAmountInfo, GuideCommand.EqualsInfo[] scaleChangeAmountInfo)
        {
            this._moveChangeAmountInfo = moveChangeAmountInfo;
            this._rotateChangeAmountInfo = rotateChangeAmountInfo;
            this._scaleChangeAmountInfo = scaleChangeAmountInfo;
        }

        public void Do()
        {
            foreach (GuideCommand.EqualsInfo info in this._moveChangeAmountInfo)
            {
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                if (changeAmount != null)
                    changeAmount.pos = info.newValue;
            }

            foreach (GuideCommand.EqualsInfo info in this._rotateChangeAmountInfo)
            {
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                if (changeAmount != null)
                    changeAmount.rot = info.newValue;
            }

            foreach (GuideCommand.EqualsInfo info in this._scaleChangeAmountInfo)
            {
                ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                if (changeAmount != null)
                    changeAmount.scale = info.newValue;
            }

        }

        public void Redo()
        {
            this.Do();
        }

        public void Undo()
        {
            foreach (Studio.GuideCommand.EqualsInfo info in this._moveChangeAmountInfo)
            {
                Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                if (changeAmount != null)
                    changeAmount.pos = info.oldValue;
            }
            foreach (Studio.GuideCommand.EqualsInfo info in this._rotateChangeAmountInfo)
            {
                Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                if (changeAmount != null)
                    changeAmount.rot = info.oldValue;
            }
            foreach (Studio.GuideCommand.EqualsInfo info in this._scaleChangeAmountInfo)
            {
                Studio.ChangeAmount changeAmount = Studio.Studio.GetChangeAmount(info.dicKey);
                if (changeAmount != null)
                    changeAmount.scale = info.oldValue;
            }
        }
    }
}
