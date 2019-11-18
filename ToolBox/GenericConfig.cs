#if HONEYSELECT || PLAYHOME
using IllusionPlugin;
#elif KOIKATSU || AISHOUJO
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
#endif

namespace ToolBox
{
    public class GenericConfig
    {
        private readonly string _name;
#if KOIKATSU || AISHOUJO
        private readonly ConfigFile _configFile;
#endif

        public GenericConfig(string name, GenericPlugin plugin = null)
        {
            this._name = name;
#if KOIKATSU || AISHOUJO
            if (plugin != null && plugin.Config != null)
                this._configFile = plugin.Config;
            else
                this._configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, this._name + ".cfg"), true);
#endif
        }

#if KOIKATSU ||AISHOUJO
        private ConfigEntry<T> GetOrAddEntry<T>(string key, T defaultValue, string description = null)
        {
            ConfigEntry<T> entry;
            if (this._configFile.TryGetEntry(this._name, key, out entry) == false)
            {
                if (description == null)
                    entry = this._configFile.Bind(this._name, key, defaultValue, new ConfigDescription("", null, "Advanced"));
                else
                    entry = this._configFile.Bind(this._name, key, defaultValue, new ConfigDescription(description));
            }
            return entry;
        }

        private ConfigEntry<T> GetEntry<T>(string key)
        {
            ConfigEntry<T> entry;
            if (this._configFile.TryGetEntry(this._name, key, out entry))
                return entry;
            return null;
        }
#endif

        public string AddString(string key, string defaultValue, bool autoSave, string description = null)
        {
#if HONEYSELECT || PLAYHOME
            return ModPrefs.GetString("VideoExport", key, defaultValue, autoSave);
#elif KOIKATSU || AISHOUJO
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetString(string key, string value)
        {
#if HONEYSELECT || PLAYHOME
            ModPrefs.SetString("VideoExport", key, value);
#elif KOIKATSU || AISHOUJO
            this.GetEntry<string>(key).Value = value;
#endif
        }

        public int AddInt(string key, int defaultValue, bool autoSave, string description = null)
        {
#if HONEYSELECT || PLAYHOME
            return ModPrefs.GetInt("VideoExport", key, defaultValue, autoSave);
#elif KOIKATSU || AISHOUJO
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetInt(string key, int value)
        {
#if HONEYSELECT || PLAYHOME
            ModPrefs.SetInt("VideoExport", key, value);
#elif KOIKATSU || AISHOUJO
            this.GetEntry<int>(key).Value = value;
#endif
        }

        public bool AddBool(string key, bool defaultValue, bool autoSave, string description = null)
        {
#if HONEYSELECT || PLAYHOME
            return ModPrefs.GetBool("VideoExport", key, defaultValue, autoSave);
#elif KOIKATSU || AISHOUJO
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetBool(string key, bool value)
        {
#if HONEYSELECT || PLAYHOME
            ModPrefs.SetBool("VideoExport", key, value);
#elif KOIKATSU || AISHOUJO
            this.GetEntry<bool>(key).Value = value;
#endif
        }

        public float AddFloat(string key, float defaultValue, bool autoSave, string description = null)
        {
#if HONEYSELECT || PLAYHOME
            return ModPrefs.GetFloat("VideoExport", key, defaultValue, autoSave);
#elif KOIKATSU || AISHOUJO
            return this.GetOrAddEntry(key, defaultValue, description).Value;
#endif
        }

        public void SetFloat(string key, float value)
        {
#if HONEYSELECT || PLAYHOME
            ModPrefs.SetFloat("VideoExport", key, value);
#elif KOIKATSU || AISHOUJO
            this.GetEntry<float>(key).Value = value;
#endif
        }

        public void Save()
        {
#if KOIKATSU || AISHOUJO
            this._configFile.Save();
#endif
        }
    }
}
