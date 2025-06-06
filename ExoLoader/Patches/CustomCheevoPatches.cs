using HarmonyLib;
using System;
using UnityEngine;

namespace ExoLoader
{
    [HarmonyPatch]
    public class CustomCheevoPatches
    {
        // Load exoloader save data when loading the Groundhogs
        [HarmonyPatch(typeof(Groundhogs), "Load")]
        [HarmonyPostfix]
        public static void GroundhogsLoadPostfix()
        {
            ExoLoaderSave.instance.Load();
        }

        // Some endings can automatically award achievements
        [HarmonyPatch(typeof(Ending), "AwardEnding")]
        [HarmonyPostfix]
        public static void EndingAwardEndingPostfix(Ending __instance, string backgroundID)
        {
            string cheevoID = "ending_" + __instance.endingID.ToLower();
            if (CustomCheevo.customCheevosByID.TryGetValue(cheevoID.ToLower(), out CustomCheevo customCheevo))
            {
                ExoLoaderSave.MaybeAwardCheevo(cheevoID);
                ModInstance.log($"Awarded custom Cheevo: {cheevoID}");
            }
        }

        // chara_ achievements are granted when getting character's 3rd card
        [HarmonyPatch(typeof(Chara), nameof(Chara.MaybeAwardCardThreeCheevo))]
        [HarmonyPostfix]
        public static void MaybeAwardCardThreeCheevoPostfix(Chara __instance)
        {
            if (__instance == null || __instance.charaID == null)
            {
                return;
            }

            // If chara is instance of CustomChara
            if (__instance is CustomChara customChara)
            {
                ModInstance.log($"Custom character {customChara.charaID} detected");
                string cheevoID = "chara_" + __instance.charaID.ToLower();
                if (CustomCheevo.customCheevosByID.TryGetValue(cheevoID.ToLower(), out CustomCheevo customCheevo))
                {
                    // If a custom Cheevo is found, award it
                    ExoLoaderSave.MaybeAwardCheevo(cheevoID);
                    ModInstance.log($"Awarded custom Cheevo for character: {cheevoID}");
                }
                else
                {
                    ModInstance.log($"No custom Cheevo found for character: {cheevoID}");
                }
                return;
            }
        }

        [HarmonyPatch(typeof(Cheevo))]
        [HarmonyPatch(nameof(Cheevo.FromID), new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPostfix]
        public static void CheevoFromIDPatch(ref Cheevo __result, string idString, bool ignoreMissing = false)
        {
            if (__result != null && __result?.cheevoID != CheevoID.none)
            {
                return;
            }

            if (CustomCheevo.customCheevosByID.TryGetValue(idString.ToLower(), out CustomCheevo customCheevo))
            {
                // If a custom Cheevo is found, return it
                __result = customCheevo;
            }

        }

        // Show custom achievements in the Gallery
        [HarmonyPatch(typeof(GalleryMenu))]
        [HarmonyPatch("PopulateCheevos", MethodType.Normal)]
        [HarmonyPostfix]
        public static void PopulateCheevos_Postfix(GalleryMenu __instance)
        {
            ModInstance.log("PopulateCheevos was called!");

            foreach (var cheevo in CustomCheevo.customCheevos)
            {
                bool show = ExoLoaderSave.HasCheevo(cheevo.customID);
                Sprite sprite = ModAssetManager.GetSprite(AssetContentType.Achievement, cheevo.customID);
                if (sprite == null)
                {
                    ModInstance.log($"Cheevo {cheevo.customID} has no sprite, skipping.");
                    continue;
                }
                __instance.galleryItemCheevosPrefab.Spawn(__instance.imagesContainer).SetImage(GalleryMode.cheevos, cheevo.cheevoName, sprite, show, cheevo);
            }
        }

        [HarmonyPatch(typeof(CheevoManager), nameof(CheevoManager.CheckMissedCheevos))]
        [HarmonyPostfix]
        public static void CheckMissedCheevosPostfix()
        {
            foreach (CustomCheevo cheevo in CustomCheevo.customCheevos)
            {
                if (ExoLoaderSave.HasCheevo(cheevo.customID))
                {
                    continue;
                }

                // Handle achievements given for having rep with all characters in loveAll
                if (cheevo.loveAll != null && cheevo.loveAll.Count > 0)
                {
                    int hasCardCount = 0;

                    foreach (string charaID in cheevo.loveAll)
                    {
                        if (Princess.seenCards.ContainsSafe(charaID + "3"))
                        {
                            hasCardCount++;
                        }
                    }

                    ModInstance.log($"Checking custom Cheevo for love: {cheevo.customID}, hasCardCount: {hasCardCount}, loveAll count: {cheevo.loveAll.Count}");

                    if (hasCardCount == cheevo.loveAll.Count)
                    {
                        ExoLoaderSave.MaybeAwardCheevo(cheevo.customID);
                        ModInstance.log($"Awarded custom Cheevo for love: {cheevo.customID}");
                    }
                }

                // Handle achievements given for having specific achievements in requiredCheevos
                if (cheevo.requiredCheevos != null && cheevo.requiredCheevos.Count > 0)
                {
                    int hasRequiredCount = 0;

                    foreach (string requiredCheevoID in cheevo.requiredCheevos)
                    {
                        if (ExoLoaderSave.HasCheevo(requiredCheevoID))
                        {
                            hasRequiredCount++;
                            continue;
                        }
                        else
                        {
                            // Try to parse it as cheevoenum and check from cheevomanager, if it's a vanilla cheevo
                            if (Enum.TryParse(requiredCheevoID, out CheevoID cheevoID))
                            {
                                if (cheevoID != CheevoID.none && CheevoManager.HasCheevo(cheevoID))
                                {
                                    hasRequiredCount++;
                                    continue;
                                }
                            }
                        }
                    }

                    ModInstance.log($"Checking custom Cheevo for required: {cheevo.customID}, hasRequiredCount: {hasRequiredCount}, requiredCheevos count: {cheevo.requiredCheevos.Count}");

                    if (hasRequiredCount == cheevo.requiredCheevos.Count)
                    {
                        ExoLoaderSave.MaybeAwardCheevo(cheevo.customID);
                        ModInstance.log($"Awarded custom Cheevo for required: {cheevo.customID}");
                    }
                }

                // Handle achievements given for having card3 for a specific character
                if (cheevo.customID.StartsWith("chara_"))
                {
                    string charaId = cheevo.customID.RemoveStart("chara_").ToLower();

                    ModInstance.log($"Checking custom Cheevo for character: {cheevo.customID}, charaId: {charaId}");

                    if (Princess.seenCards.ContainsSafe(charaId + "3"))
                    {
                        // If the character has a card3, award the cheevo
                        ExoLoaderSave.MaybeAwardCheevo(cheevo.customID);
                        ModInstance.log($"Awarded custom Cheevo for character: {cheevo.customID}");
                    }
                }

                if (cheevo.customID.StartsWith("ending_"))
                {
                    Ending ending = Ending.FromID(cheevo.customID.RemoveStart("ending_"));
                    if (ending == null)
                    {
                        ModInstance.log($"Ending with ID {cheevo.customID} ({cheevo.customID.RemoveStart("ending_")}) not found, skipping.");
                        continue;
                    }
                    // If the cheevo is an ending, check if the ending has been awarded
                    if (ending.HasSeenEnding())
                    {
                        ExoLoaderSave.MaybeAwardCheevo(cheevo.customID);
                        ModInstance.log($"Awarded custom Cheevo for ending: {cheevo.customID}");
                    }
                }
            }
        }

        // This is for ~call cheevo("cheevoID") in story scripts
        [HarmonyPatch(typeof(StoryCalls), nameof(StoryCalls.cheevo))]
        [HarmonyPostfix]
        public static void StoryCallsCheevoPostfix(string cheevoID)
        {
            if (CustomCheevo.customCheevosByID.TryGetValue(cheevoID.ToLower(), out CustomCheevo customCheevo))
            {
                ExoLoaderSave.MaybeAwardCheevo(cheevoID);
                ModInstance.log($"Awarded custom Cheevo from StoryCalls: {cheevoID}");
            }
        }

        // CheevoManager.ClearAllCheevos (when user deletes their achievement data) should also clear custom cheevos
        [HarmonyPatch(typeof(CheevoManager), nameof(CheevoManager.ClearAllCheevos))]
        [HarmonyPostfix]
        public static void CheevoManagerClearAllCheevosPostfix()
        {
            ModInstance.log("Clearing all custom cheevos from ExoLoaderSave");
            ExoLoaderSave.ClearCustomCheevos();
        }
    }
}
