using HarmonyLib;
using Northway.Utils;
using System;
using UnityEngine;

namespace ExoLoader
{
    [HarmonyPatch]
    public class AddMapSpotPatches
    {

        [HarmonyPatch(typeof(BillboardManager), nameof(BillboardManager.FillMapspots))]
        [HarmonyPrefix]
        public static void AddCustomMapSpots()
        {
            string scene = MapManager.currentScene.RemoveStart("Colony").ToLower();
            string season = Princess.season.seasonID;
            int week = Princess.monthOfSeason;

            try
            {
                foreach (CustomChara cC in CustomChara.customCharasById.Values)
                {
                    if (cC.data.onMap && (!cC.data.helioOnly || scene.Equals("helio")))
                    {
                        CustomMapManager.MakeCustomMapObject(cC, season, week, scene);
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Error while trying to add custom map spots: " + e.Message);
                ModInstance.log(e.StackTrace);
            }
        }

        [HarmonyPatch(typeof(MapSpot), nameof(MapSpot.SetStory))]
        [HarmonyPostfix]
        public static void SetStoryPatch(MapSpot __instance, Story newStory)
        {
            try
            {
                if (__instance.type == MapSpotType.chara || __instance.type == MapSpotType.story || __instance.story == null)
                {
                    return;
                }

                MapSpot mapSpot = __instance;

                string spriteID = mapSpot.story.GetSpriteID();

                CustomChara customChara = CustomChara.customCharasById.GetValueSafe(spriteID);

                if (customChara == null || customChara.data.skeleton.Length == 0)
                {
                    return;
                }

                GameObject mapSpotPrefab = null;
                string usedSkeleton = null;

                foreach (GameObject gameObject in Singleton<AssetManager>.instance.mapSpotPrefabs)
                {
                    foreach (string skeleton in customChara.data.skeleton)
                    {
                        if (gameObject != null && string.Equals(gameObject.name, skeleton, StringComparison.CurrentCultureIgnoreCase))
                        {
                            mapSpotPrefab = gameObject;
                            usedSkeleton = skeleton;
                            break;
                        }
                    }

                    if (mapSpotPrefab != null)
                    {
                        ModInstance.log($"Using skeleton {usedSkeleton} for {customChara.charaID}");
                        break;
                    }
                }

                GameObject newObject = UnityEngine.Object.Instantiate(mapSpotPrefab, mapSpot.transform);
                GameObject spriteObject = CustomMapManager.MakeExpeditionObject(newObject, usedSkeleton, customChara, mapSpot.transform);

                if (spriteObject == null)
                {
                    ModInstance.log($"Failed to create map spot for {customChara.charaID} with skeleton {usedSkeleton}");
                    return;
                }

                AccessTools.Field(typeof(MapSpot), "spriteObject").SetValue(mapSpot, spriteObject);

                MapSpotIndicator mapSpotIndicator = mapSpot.mapSpotIndicator;
                if (mapSpotIndicator != null)
                {
                    mapSpotIndicator.Hide();
                    mapSpotIndicator.Show(mapSpot, spriteObject != null, mapSpot.story.priority == Priority.high);
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Error while trying to set story for map spot: " + e.Message);
                ModInstance.log(e.StackTrace);
            }
        }

        [HarmonyPatch(typeof(MapSpot), nameof(MapSpot.SetOrPickCharaStory))]
        [HarmonyPrefix]
        public static void LoggingPatch(Story newStory, MapSpot __instance)
        {
            // if (!CustomChara.customCharasById.ContainsKey(__instance.charaID))
            // {
            //     return;
            // }
            // if (newStory == null)
            // {
            //     ModInstance.log("Called SetOrPickCharaStory on MapSpot with charaID " + __instance.charaID + " for Picking");
            // } else
            // {
            //     ModInstance.log("Called SetOrPickCharaStory on MapSpot with charaID " + __instance.charaID + " for Setting");
            // }
            // if (CustomChara.customCharasById.ContainsKey(__instance.charaID))
            // {
            //     CustomChara.customCharasById[__instance.charaID].DictionaryTests();
            // }
        }

        [HarmonyPatch(typeof(MapSpot), nameof(MapSpot.Trigger))]
        [HarmonyPrefix]
        public static void LoggingPatchAgain(MapSpot __instance)
        {
            // if (__instance.type == MapSpotType.location)
            // {
            //     ModInstance.log("Triggered location " + __instance.locationID + " map spot");
            // }
            // if (__instance.story == null)
            // {
            //     ModInstance.log("Triggered MapSpot with charaID " + __instance.charaID + " but story is null");
            // }
            // else
            // {
            //     ModInstance.log("Triggered MapSpot with charaID " + __instance.charaID + " with story " + __instance.storyName.ToString());
            // }
        }

 
    }
}
