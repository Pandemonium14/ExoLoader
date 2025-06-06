using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (!MapManager.IsColonyScene(MapManager.currentScene))
            {
                return;
            }

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

        [HarmonyPatch(typeof(MapSpot), nameof(MapSpot.SetOrPickCharaStory))]
        [HarmonyPrefix]
        public static void LoggingPatch(Story newStory, MapSpot __instance)
        {
            if (!CustomChara.customCharasById.ContainsKey(__instance.charaID))
            {
                return;
            }
            if (newStory == null)
            {
                ModInstance.log("Called SetOrPickCharaStory on MapSpot with charaID " + __instance.charaID + " for Picking");
            } else
            {
                ModInstance.log("Called SetOrPickCharaStory on MapSpot with charaID " + __instance.charaID + " for Setting");
            }
            if (CustomChara.customCharasById.ContainsKey(__instance.charaID))
            {
                CustomChara.customCharasById[__instance.charaID].DictionaryTests();
            }
        }

        [HarmonyPatch(typeof(MapSpot), nameof(MapSpot.Trigger))]
        [HarmonyPrefix]
        public static void LoggingPatchAgain(MapSpot __instance)
        {
            if (__instance.type == MapSpotType.location)
            {
                ModInstance.log("Triggered location " + __instance.locationID + " map spot");
            }
            if (__instance.story == null)
            {
                ModInstance.log("Triggered MapSpot with charaID " + __instance.charaID + " but story is null");
            }
            else
            {
                ModInstance.log("Triggered MapSpot with charaID " + __instance.charaID + " with story " + __instance.storyName.ToString());
            }
        }

 
    }
}
