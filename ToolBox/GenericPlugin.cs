#if HONEYSELECT || PLAYHOME
using IllusionInjector;
#else
using BepInEx;
using UnityEngine.SceneManagement;
#endif
using System;
using UnityEngine;
using System.Reflection;

namespace ToolBox
{
    public abstract class GenericPlugin
#if KOIKATSU || AISHOUJO
       : BaseUnityPlugin
#endif
    {
        internal Binary _binary;
        internal int _level = -1;
#if HONEYSELECT || PLAYHOME
        private static PluginComponent _pluginComponent;
        private Component _onGUIDispatcher = null;

        public abstract string Name { get; }
        public abstract string Version { get; }
        public abstract string[] Filter { get; }
        public GameObject gameObject
        {
            get
            {
                if (_pluginComponent == null)
                    _pluginComponent = UnityEngine.Object.FindObjectOfType<PluginComponent>();
                return _pluginComponent.gameObject;
            }
        }

        public void OnApplicationStart()
        {
            this.Awake();
        }

        public void OnApplicationQuit()
        {
            this.OnDestroy();
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
            this._level = level;
            this.LevelLoaded(level);
        }

        public void OnUpdate()
        {
            this.Update();
        }

        public void OnFixedUpdate()
        {
            this.FixedUpdate();
        }

        public void OnLateUpdate()
        {
            this.LateUpdate();
        }
#endif

        protected virtual void Awake()
        {
#if KOIKATSU || AISHOUJO
            SceneManager.sceneLoaded += this.LevelLoaded;
#endif
            switch (Application.productName)
            {
#if HONEYSELECT
                case "HoneySelect":
                case "Honey Select Unlimited":
#elif KOIKATSU
                case "Koikatsu Party":
                case "Koikatu":
#elif AISHOUJO
                case "AI-Syoujyo":
#endif
                    this._binary = Binary.Game;
                    break;
#if HONEYSELECT
                case "StudioNEO":
#elif KOIKATSU
                case "CharaStudio":
#elif AISHOUJO
                case "StudioNEOV2":
#endif
                    this._binary = Binary.Studio;
                    break;
            }
#if HONEYSELECT || PLAYHOME

            Component[] components = this.gameObject.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c.GetType().Name == nameof(OnGUIDispatcher))
                {
                    this._onGUIDispatcher = c;
                    break;
                }
            }
            if (this._onGUIDispatcher == null)
                this._onGUIDispatcher = this.gameObject.gameObject.AddComponent<OnGUIDispatcher>();
            this._onGUIDispatcher.GetType().GetMethod(nameof(OnGUIDispatcher.AddListener), BindingFlags.Instance | BindingFlags.Public).Invoke(this._onGUIDispatcher, new object[]{new Action(this.OnGUI)});
#endif
        }

        protected virtual void OnDestroy()
        {
#if HONEYSELECT || PLAYHOME
            if (this._onGUIDispatcher != null)
            this._onGUIDispatcher.GetType().GetMethod(nameof(OnGUIDispatcher.RemoveListener), BindingFlags.Instance | BindingFlags.Public).Invoke(this._onGUIDispatcher, new object[]{new Action(this.OnGUI)});
#endif
        }

        protected virtual void LevelLoaded(int level)
        {
        }

#if KOIKATSU || AISHOUJO
        protected virtual void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                this._level = scene.buildIndex;
                this.LevelLoaded(scene.buildIndex);
            }
        }
#endif

        protected virtual void Update()
        {

        }

        protected virtual void LateUpdate()
        {

        }

        protected virtual void FixedUpdate()
        {

        }

        protected virtual void OnGUI()
        {

        }
    }

#if HONEYSELECT || PLAYHOME
    internal class OnGUIDispatcher : MonoBehaviour
    {
        private event Action _onGUI;

        public void AddListener(Action listener)
        {
            this._onGUI += listener;
        }

        public void RemoveListener(Action listener)
        {
            this._onGUI -= listener;
        }

        private void OnGUI()
        {
            if (this._onGUI != null)
                this._onGUI();
        }
    }
#endif

    public enum Binary
    {
        Game,
        Studio
    }
}
