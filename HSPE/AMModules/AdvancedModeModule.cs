using System.Xml;
using Studio;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public abstract class AdvancedModeModule
    {

        #region Constants
        public static readonly Color _redColor = Color.red;
        public static readonly Color _greenColor = Color.green;
        public static readonly Color _blueColor = Color.Lerp(Color.blue, Color.cyan, 0.5f);
        protected float _inc = 1f;
        #endregion

        #region Protected Variables
        internal bool _isEnabled = false;
        protected PoseController _parent;
        #endregion

        #region Private Variables
        internal static float _repeatTimer = 0f;
        internal static bool _repeatCalled = false;
        private const float _repeatBeforeDuration = 0.5f;
        protected int _incIndex = 0;
        #endregion

        #region Abstract Fields
        public abstract AdvancedModeModuleType type { get; }
        public abstract string displayName { get; }
        #endregion

        #region Public Accessors
        public virtual bool isEnabled { get { return this._isEnabled; } set { this._isEnabled = value; } }
        public virtual bool shouldDisplay { get { return true; } }
        #endregion

        #region Public Methods
        protected AdvancedModeModule(PoseController parent)
        {
            this._parent = parent;
            this._parent.onDestroy += this.OnDestroy;
        }

        public virtual void OnDestroy()
        {
            this._parent.onDestroy -= this.OnDestroy;
        }
        public virtual void IKSolverOnPostUpdate(){}
        public virtual void FKCtrlOnPreLateUpdate() { }
        public virtual void IKExecutionOrderOnPostLateUpdate(){}
//#if HONEYSELECT
//        public virtual void CharBodyPreLateUpdate(){}
//        public virtual void CharBodyPostLateUpdate(){}
//#elif KOIKATSU
//        public virtual void CharacterPreLateUpdate() { }
//        public virtual void CharacterPostLateUpdate() { }
//#endif
        public virtual void OnCharacterReplaced() { }
        public virtual void OnLoadClothesFile() { }
#if HONEYSELECT
        public virtual void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force){}
#elif KOIKATSU
        public virtual void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force){}
#endif
        public virtual void OnParentage(TreeNodeObject parent, TreeNodeObject child) { }
        public virtual void DrawAdvancedModeChanged() { }
        #endregion

        #region Abstract Methods
        public abstract void GUILogic();
        public abstract int SaveXml(XmlTextWriter xmlWriter);
        public abstract bool LoadXml(XmlNode xmlNode);
        #endregion

        #region Protected Methods
        protected bool RepeatControl()
        {
            _repeatCalled = true;
            if (Mathf.Approximately(_repeatTimer, 0f))
                return true;
            return Event.current.type == EventType.Repaint && _repeatTimer > _repeatBeforeDuration;
        }

        protected void IncEditor(int maxHeight = 75, bool label = false)
        {
            GUILayout.BeginVertical();
            if (label)
                GUILayout.Label("10^1", GUI.skin.box, GUILayout.MaxWidth(45));
            Color c = GUI.color;
            GUI.color = Color.white;
            float maxWidth = label ? 45 : 20;
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(maxWidth));
            GUILayout.FlexibleSpace();
            this._incIndex = Mathf.RoundToInt(GUILayout.VerticalSlider(this._incIndex, 1f, -5f, GUILayout.MaxHeight(maxHeight)));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = c;
            _inc = Mathf.Pow(10, this._incIndex);
            if (label)
                GUILayout.Label("10^-5", GUI.skin.box, GUILayout.MaxWidth(45));
            GUILayout.EndVertical();
        }

        protected void IncEditor(ref int incIndex, out float inc, int maxHeight = 75, bool label = false)
        {
            GUILayout.BeginVertical();
            if (label)
                GUILayout.Label("10^1", GUI.skin.box, GUILayout.MaxWidth(45));
            Color c = GUI.color;
            GUI.color = Color.white;
            float maxWidth = label ? 45 : 20;
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(maxWidth));
            GUILayout.FlexibleSpace();
            incIndex = Mathf.RoundToInt(GUILayout.VerticalSlider(incIndex, 1f, -5f, GUILayout.MaxHeight(maxHeight)));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = c;
            inc = Mathf.Pow(10, incIndex);
            if (label)
                GUILayout.Label("10^-5", GUI.skin.box, GUILayout.MaxWidth(45));
            GUILayout.EndVertical();
        }

        protected Vector3 Vector3Editor(Vector3 value)
        {
            GUILayout.BeginVertical();
            Color c = GUI.color;
            GUI.color = _redColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:\t" + value.x.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-_inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= _inc * Vector3.right;
            if (GUILayout.RepeatButton(_inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += _inc * Vector3.right;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.color = _greenColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:\t" + value.y.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-_inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= _inc * Vector3.up;
            if (GUILayout.RepeatButton(_inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += _inc * Vector3.up;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.color = _blueColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:\t" + value.z.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-_inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= _inc * Vector3.forward;
            if (GUILayout.RepeatButton(_inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += _inc * Vector3.forward;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }

        protected Vector3 Vector3Editor(Vector3 value, Color color)
        {
            GUILayout.BeginVertical();
            Color c = GUI.color;
            GUI.color = color;
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:\t" + value.x.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-_inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= _inc * Vector3.right;
            if (GUILayout.RepeatButton(_inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += _inc * Vector3.right;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:\t" + value.y.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-_inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= _inc * Vector3.up;
            if (GUILayout.RepeatButton(_inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += _inc * Vector3.up;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:\t" + value.z.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-_inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= _inc * Vector3.forward;
            if (GUILayout.RepeatButton(_inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += _inc * Vector3.forward;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }
        #endregion
    }

    public enum AdvancedModeModuleType
    {
        BonesEditor = 0,
        CollidersEditor,
        BoobsEditor,
        DynamicBonesEditor,
        BlendShapes
    }
}
