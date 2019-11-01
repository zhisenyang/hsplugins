﻿#if HONEYSELECT || PLAYHOME
using IllusionInjector;
using UnityEngine;
#else
using BepInEx;
using UnityEngine.SceneManagement;
#endif
using UnityEngine;

namespace ToolBox
{
    public abstract class GenericPlugin
#if KOIKATSU || AISHOUJO
       : BaseUnityPlugin
#endif
    {
        internal Binary _binary;
#if HONEYSELECT || PLAYHOME
        private static PluginComponent _pluginComponent;

        public abstract string Name { get; }
        public abstract string Version { get; }
        public abstract string[] Filter { get; }
        public GameObject gameObject
        {
            get
            {
                if (_pluginComponent == null)
                    _pluginComponent = Object.FindObjectOfType<PluginComponent>();
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
        }

        protected virtual void OnDestroy()
        {

        }

        protected virtual void LevelLoaded(int level)
        {
        }

#if KOIKATSU || AISHOUJO
        protected virtual void LevelLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                this.LevelLoaded(scene.buildIndex);
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
    }

    public enum Binary
    {
        Game,
        Studio
    }
}