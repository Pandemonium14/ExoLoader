using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace ExoLoader
{
    public class ExoLoaderSave
    {
        private static ExoLoaderSave _instance;
        private static bool isLoaded;
        private const int currentVersion = 1;
        public const string filename = "ExoLoaderSave.json";
        public const string filenameBackup = "ExoLoaderSave.bak";
        public int saveFileVersion = 1;

        public List<string> cheevos = [];
        public Dictionary<string, bool> settings = new()
        {
            { "showErrorOverlay", false }
        };

        public static ExoLoaderSave instance
        {
            get
            {
                _instance ??= new ExoLoaderSave();

                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static void UpdateSettings(string key, bool value)
        {
            if (instance.settings.ContainsKey(key))
            {
                instance.settings[key] = value;
            }
            else
            {
                instance.settings.Add(key, value);
            }

            Save();
        }

        public static bool GetSetting(string key, bool defaultValue = false)
        {
            if (instance.settings.TryGetValue(key, out bool value))
            {
                return value;
            }

            return defaultValue;
        }

        public static bool HasCheevo(string id)
        {
            return instance.cheevos.Contains(id.ToLower());
        }

        public static void MaybeAwardCheevo(string id)
        {
            if (HasCheevo(id))
            {
                return;
            }

            instance.cheevos.AddSafe(id.ToLower());
            Save();
        }

        public static void ClearCustomCheevos()
        {
            if (instance.cheevos.Count == 0)
            {
                return;
            }

            instance.cheevos.Clear();
            Save();
        }

        public void Load()
        {
            try
            {
                LoadInner();
            }
            catch (Exception ex)
            {
                ModInstance.log("ExoLoaderSave.Load error during FromJsonOverwrite: " + ex);
                isLoaded = true;
                Save();
            }
        }
        
        public void LoadInner()
        {
            bool flag = false;
            string text = FileManager.LoadFileString(filename, FileManager.documentsPath, warnFileMissing: false);
            if (text.IsNullOrEmptyOrWhitespace())
            {
                text = FileManager.LoadFileString(filenameBackup, FileManager.documentsPath, warnFileMissing: false);
                flag = true;
                if (text.IsNullOrEmptyOrWhitespace())
                {
                    ModInstance.log($"{filename} not found, no backup, must be a fresh install");
                    isLoaded = true;
                    Save();
                    return;
                }

                ModInstance.log($"{filename} was empty, loaded from backup {filenameBackup}");
            }

            try
            {
                JsonConvert.PopulateObject(text, this);
            }
            catch (Exception ex)
            {
                if (flag)
                {
                    ModInstance.log($"{filename} file is corrupt! Resetting to defaults. " + ex.Message);
                    isLoaded = true;
                    Save();
                    return;
                }

                text = FileManager.LoadFileString(filenameBackup, FileManager.documentsPath, warnFileMissing: false);
                flag = true;
                if (text.IsNullOrEmptyOrWhitespace())
                {
                    ModInstance.log($"{filenameBackup} file is corrupt, no backup. Resetting to defaults. " + ex.Message);
                    isLoaded = true;
                    Save();
                    return;
                }

                try
                {
                    JsonConvert.PopulateObject(text, this);
                }
                catch (Exception ex2)
                {
                    ModInstance.log($"{filename} file and backup are corrupt! Resetting to defaults. " + ex2.Message);
                    isLoaded = true;
                    Save();
                    return;
                }
            }

            if (saveFileVersion != currentVersion)
            {
                ModInstance.log($"{filename} version changed from " + saveFileVersion + " to " + currentVersion);
                saveFileVersion = currentVersion;
            }

            isLoaded = true;
            if (flag)
            {
                Save();
            }
        }

        private void SaveThread(string path)
        {
            FileManager.SaveFile(JsonConvert.SerializeObject(this), filename, path);
        }

        public static void Save(bool threaded = true)
        {
            try
            {
                if (!isLoaded)
                {
                    ModInstance.log("ExoLoaderSave.Save but hasn't been loaded yet, not saving");
                    return;
                }

                ExoLoaderSave clone = instance.DeepClone();
                string path = FileManager.documentsPath;
                if (threaded)
                {
                    ThreadWorker.ExecuteInThread(delegate
                    {
                        clone.SaveThread(path);
                    }, ThreadProcessID.saveFile);
                }
                else
                {
                    clone.SaveThread(path);
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error saving ExoLoaderSave: {e.Message}");
                if (e.InnerException != null)
                {
                    ModInstance.log($"Inner Exception: {e.InnerException.Message}");
                }
            }
        }

        public ExoLoaderSave DeepClone()
        {
            ExoLoaderSave save = (ExoLoaderSave)MemberwiseClone();

            save.cheevos = [.. cheevos];
            save.settings = new Dictionary<string, bool>(settings);

            return save;
        }

        
    }
}