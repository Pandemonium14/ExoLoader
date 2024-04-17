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

        [HarmonyPatch(typeof(BillboardManager), nameof(BillboardManager.OnMapLoaded))]
        [HarmonyPrefix]
        public static void AddCustomMapSpots()
        {
            if (!MapManager.IsColonyScene(MapManager.currentScene))
            {
                return;
            }
            string scene = MapManager.currentScene.RemoveStart("Colony").ToLower();
            string season = Princess.season.seasonID;
            ModInstance.log(season);
            int week = Princess.monthOfSeason;

            foreach (CustomChara cC in CustomChara.customCharasById.Values)
            {
                if (cC.data.onMap && ( !cC.data.helioOnly || scene.Equals("helio"))) {
                    Tuple<GameObject, Transform> pair = CustomMapObjectMaker.MakeCustomMapObject(cC.charaID, season, week, scene);

                    if (pair != null && pair.Item1 != null && pair.Item2 != null)
                    {
                        /*ModInstance.log("GameObject-Transform pair wasn't null");
                        ModInstance.log("GameObject active state : " + pair.Item1.activeSelf.ToString() + " self, and " + pair.Item1.activeInHierarchy.ToString() + " in hierarchy");
                        
                        pair.Item1.transform.parent = pair.Item2;
                        ModInstance.log("Object parent set to provided Transform");
                        if (!MapSpot.allMapSpots.Contains(pair.Item1.GetComponent<MapSpot>()))
                        {
                            MapSpot.allMapSpots.Add(pair.Item1.GetComponent<MapSpot>());
                        }*/
                        GameObject actualSpot = PoolManager.Spawn(pair.Item1, pair.Item2);
                        actualSpot.transform.localPosition = pair.Item1.transform.localPosition;
                        pair.Item1.DestroySafe();
                    }
                    else
                    {
                        ModInstance.log("GameObject-Transform pair was null");
                    }

                }
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
