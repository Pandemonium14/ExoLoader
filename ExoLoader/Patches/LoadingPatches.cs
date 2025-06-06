using System.Collections;
using UnityEngine;
using Northway.Utils;
using HarmonyLib;
using UnityEngine.UIElements;

namespace ExoLoader
{
    public class ExoLoadingManager : MonoBehaviour
    {
        private static ExoLoadingManager _instance;
        public static ExoLoadingManager Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                ModInstance.log("ExoLoadingManager created.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(LoadWithProgress());
        }

        private IEnumerator LoadWithProgress()
        {
            ModInstance.log("Starting loading process...");

            yield return StartCoroutine(ModAssetLoader.LoadWithProgress());

            ModInstance.log("Loading process completed.");

            yield return new WaitForSeconds(0.2f);

            MainMenuCharas mainMenuCharas = FindObjectOfType<MainMenuCharas>();

            if (mainMenuCharas != null)
            {
                ImagePatches.PatchMainManuCharas(mainMenuCharas);
            }
        }
    }

    [HarmonyPatch]
    public class LoadingPatches
    {
        [HarmonyPatch(typeof(MainMenu), "OnStartOpen")]
        [HarmonyPostfix]
        public static void CreateLoadingManager(MainMenu __instance)
        {
            if (ExoLoadingManager.Instance == null)
            {
                GameObject loaderObj = new GameObject("ExoLoadingManager");
                loaderObj.AddComponent<ExoLoadingManager>();
                ModInstance.log("Created ExoLoadingManager");
            }
        }
    }
}
