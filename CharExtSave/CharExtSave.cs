using IllusionPlugin;
using Harmony;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Xml;

namespace CharExtSave
{
    public class CharExtSave : IPlugin
    {
        #region Public Types
        public delegate void ExtSaveReadHandler(CharFile charFile, XmlNode node);
        public delegate void ExtSaveWriteHandler(CharFile charFile, XmlTextWriter writer);
        #endregion

        #region Private Types
        internal class HandlerPair
        {
            public ExtSaveReadHandler onRead;
            public ExtSaveWriteHandler onWrite;
        }
        #endregion

        #region Private Variables
        internal static Dictionary<string, HandlerPair> _handlers = new Dictionary<string, HandlerPair>();
        #endregion

        #region Public Accessors
        public static string logPrefix { get { return "CharExtSave: "; } }
        public string Name { get { return "CharExtSave"; } }
        public string Version { get { return "1.0.0"; } }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.charextsave");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnApplicationQuit()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Used to register a handler.
        /// </summary>
        /// <param name="name">Name of the handler: it must be unique and without special characters.</param>
        /// <param name="onRead">Callback that will be called when the plugin reads a character card.</param>
        /// <param name="onWrite">Callback that will be called when the plugin saves a character card.</param>
        /// <returns>true if the operation was successful, otherwise false</returns>
        public static bool RegisterHandler(string name, ExtSaveReadHandler onRead, ExtSaveWriteHandler onWrite)
        {
            if (string.IsNullOrEmpty(name))
            {
                UnityEngine.Debug.LogError(CharExtSave.logPrefix + "Name of the handler must not be null or empty...");
                return false;
            }
            HandlerPair pair;
            if (_handlers.TryGetValue(name, out pair))
                UnityEngine.Debug.LogWarning(CharExtSave.logPrefix + "Handler is already registered, updating callbacks...");
            else
                pair = new HandlerPair();
            pair.onRead = onRead;
            pair.onWrite = onWrite;
            return true;
        }

        /// <summary>
        /// Used to unregister a handler.
        /// </summary>
        /// <param name="name">Name of the handler: it must be unique and without special characters.</param>
        /// <returns>true if the operation was successful, false otherwise</returns>
        public static bool UnregisterHandler(string name)
        {
            if (_handlers.ContainsKey(name))
            {
                _handlers.Remove(name);
                return true;
            }
            UnityEngine.Debug.LogWarning(CharExtSave.logPrefix + "Handler is not registered, operation will be ignored...");
            return false;
        }
        #endregion
    }
}
