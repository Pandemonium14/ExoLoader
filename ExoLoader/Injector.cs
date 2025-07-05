using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExoLoader
{
    [BepInEx.BepInPlugin("ExoLoaderInject", "ExoLoader", "1.6.1")]
    public class Injector : BaseUnityPlugin
    {
        public void Awake()
        {
            try
            {
                Logger.LogInfo("Doing ExoLoader patches...");
                var harmony = new Harmony("ExoLoader");
                harmony.PatchAll();
                Logger.LogInfo("ExoLoader patches done.");
                ModInstance.instance = this;
            }
            catch (Exception e)
            {
                Logger.LogError($"ExoLoader failed to initialize: {e.Message}\n{e.StackTrace}");
                ModLoadingStatus.LogError($"ExoLoader failed to initialize: {e.Message}");
            }
        }

        public void Log(string message)
        {
            Logger.LogInfo(message);
        }
    }
}
