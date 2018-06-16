using System.Xml;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public abstract class AdvancedModeModule : MonoBehaviour
    {
        static AdvancedModeModule()
        {
            VectorLine.SetEndCap("vector", EndCap.Back, 0f, -1f, 1f, 4f, MainWindow.self.vectorMiddle, MainWindow.self.vectorEndCap);
            VectorLine.canvas.sortingOrder -= 40;
        }

        #region Constants
        public static readonly Color _redColor = Color.red;
        public static readonly Color _greenColor = Color.green;
        public static readonly Color _blueColor = Color.Lerp(Color.blue, Color.cyan, 0.5f);
        protected static float _inc = 1f;
        #endregion

        #region Protected Variables
        #endregion

        #region Private Variables
        private float _repeatTimer = 0f;
        private bool _repeatCalled = false;
        private float _repeatBeforeDuration = 0.5f;
        private int _incIndex = 0;
        #endregion

        #region Abstract Fields
        public abstract AdvancedModeModuleType type { get; }
        public abstract string displayName { get; }
        #endregion

        #region Public Accessors
        public virtual bool isEnabled { get; set; } = false;
        public virtual bool drawAdvancedMode { get; set; } = false;
        #endregion

        #region Unity Methods
        protected virtual void Update()
        {
            if (this._repeatCalled)
                this._repeatTimer += Time.unscaledDeltaTime;
            else
                this._repeatTimer = 0f;
            this._repeatCalled = false;
        }
        #endregion

        #region Abstract Methods
        public abstract void GUILogic();
        public abstract int SaveXml(XmlTextWriter xmlWriter);
        public abstract void LoadXml(XmlNode xmlNode);
        #endregion

        #region Public Methods
        public virtual void IKSolverOnPreRead(){}
        public virtual void IKSolverOnPostUpdate(){}
        public virtual void FKCtrlOnPostLateUpdate(){}
        public virtual void IKExecutionOrderOnPostLateUpdate(){}
#if HONEYSELECT
        public virtual void CharBodyPreLateUpdate(){}
        public virtual void CharBodyPostLateUpdate(){}
#elif KOIKATSU
        public virtual void CharacterPreLateUpdate() { }
        public virtual void CharacterPostLateUpdate() { }
#endif
        #endregion

        #region Protected Methods
        protected bool RepeatControl()
        {
            this._repeatCalled = true;
            if (Mathf.Approximately(this._repeatTimer, 0f))
                return true;
            return Event.current.type == EventType.Repaint && this._repeatTimer > this._repeatBeforeDuration;
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
        BoobsEditor,
        DynamicBonesEditor,
        BlendShapes
    }
}
