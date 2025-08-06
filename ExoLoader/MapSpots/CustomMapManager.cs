using System;
using System.Collections.Generic;
using Northway.Utils;
using UnityEngine;

namespace ExoLoader
{
    internal class CustomMapManager
    {
        public static Dictionary<string, GameObject> mapObjects = new Dictionary<string, GameObject>();
        public static Dictionary<string, GameObject> expeditionObjects = new Dictionary<string, GameObject>();
        private static MapObjectFactory objectFactory = new MapObjectFactory();
        private static Dictionary<string, int> lastArtStage = new Dictionary<string, int>();

        public static void MakeCustomMapObject(CustomChara chara, string season, int week, string scene)
        {
            try
            {
                if (!IsValidScene(chara, scene) || chara.isDead)
                {
                    RemoveMapObject(chara.charaID);
                    ModInstance.log("Tried making " + chara.charaID + " model in unsuitable scene " + scene);
                    return;
                }

                if (ShouldRecreateMapObject(chara))
                {
                    RemoveMapObject(chara.charaID);
                }

                if (mapObjects.ContainsKey(chara.charaID))
                {
                    HandleExistingMapObject(chara, scene);
                    return;
                }

                CreateNewMapObject(chara, season, week, scene);
            }
            catch (Exception e)
            {
                ModInstance.log("Error while trying to get map object for " + chara.charaID + ": " + e.Message);
                ModInstance.log(e.ToString());
            }
        }

        private static bool ShouldRecreateMapObject(CustomChara chara)
        {
            if (mapObjects.ContainsKey(chara.charaID))
            {
                if (!lastArtStage.ContainsKey(chara.charaID) || lastArtStage[chara.charaID] != chara.artStage)
                {
                    ModInstance.log($"Recreating map object for {chara.charaID} due to art stage change");
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidScene(CustomChara chara, string scene)
        {
            if (scene.Equals("helio") || scene.Equals("nearbyhelio"))
            {
                return true;
            }

            if (chara.data.helioOnly)
            {
                if (!Princess.HasMemory(Princess.memNewship))
                {
                    return false;
                }
            }
            // swamp is unsupported
            return !scene.Equals("swamp");
        }

        private static void HandleExistingMapObject(CustomChara chara, string scene)
        {
            GameObject existingObject = mapObjects[chara.charaID];

            if (existingObject != null)
            {
                if (ShouldRemoveFromScene(chara, scene))
                {
                    RemoveMapObject(chara.charaID);
                    return;
                }

                if (ShouldMoveMapObject(existingObject, chara, scene))
                {
                    MoveMapObject(existingObject, chara, scene);
                }
            }
            else
            {
                CreateNewMapObject(chara, Princess.season.seasonID, Princess.monthOfSeason, scene);
            }
        }

        private static void CreateNewMapObject(CustomChara chara, string season, int week, string scene)
        {
            GameObject templateObject = null;
            string usedSkeleton = null;

            lastArtStage[chara.charaID] = chara.artStage;

            foreach (string skeleton in chara.data.skeleton)
            {
                templateObject = objectFactory.GetMapObjectTemplate(skeleton, season, week);

                if (templateObject != null)
                {
                    usedSkeleton = skeleton;
                    ModInstance.log($"Using skeleton {skeleton} for {chara.charaID}");
                    break;
                }
            }

            if (templateObject == null || usedSkeleton == null)
            {
                ModInstance.log("Couldn't get base map object, cancelling map spot creation for : " + chara.charaID);
                return;
            }

            GameObject customMapObject = objectFactory.CreateCustomMapObject(templateObject, usedSkeleton, chara, scene);

            if (customMapObject != null)
            {
                mapObjects[chara.charaID] = customMapObject;
            }
        }

        private static bool ShouldMoveMapObject(GameObject mapObject, CustomChara chara, string scene)
        {
            float[] targetPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (targetPosition == null)
            {
                ModInstance.log($"Character {chara.charaID} should not be on scene {scene}");
                return false;
            }

            Transform currentTransform = mapObject.GetComponentInChildren<Transform>();

            if (currentTransform == null)
            {
                ModInstance.log($"Map object for {chara.charaID} has no Transform component.");
                return true;
            }

            // y coordinate is weird, so we only check x and z
            Vector3 currentPosition = new Vector3(currentTransform.localPosition.x, 0, currentTransform.localPosition.z);
            Vector3 targetPos = new Vector3(targetPosition[0], 0, targetPosition[2]);

            float tolerance = 0.01f;
            return Vector3.Distance(currentPosition, targetPos) > tolerance;
        }

        private static bool ShouldRemoveFromScene(CustomChara chara, string scene)
        {
            float[] position = chara.GetMapSpot(scene, Princess.season.seasonID);
            return position == null;
        }

        private static void MoveMapObject(GameObject mapObject, CustomChara chara, string scene)
        {
            float[] newPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (newPosition == null)
            {
                ModInstance.log($"Cannot move {chara.charaID} - no valid position for scene {scene}");
                return;
            }

            mapObject.transform.localPosition = new Vector3(newPosition[0], newPosition[1], newPosition[2]);

            MapSpot mapSpot = mapObject.GetComponent<MapSpot>();
            if (mapSpot != null)
            {
                mapSpot.MoveToGround();
            }

            ModInstance.log($"Moved {chara.charaID} to position ({newPosition[0]}, {newPosition[1]}, {newPosition[2]})");
        }

        private static void RemoveMapObject(string charaID)
        {
            if (mapObjects.ContainsKey(charaID))
            {
                GameObject obj = mapObjects[charaID];
                mapObjects.Remove(charaID);

                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }

                ModInstance.log($"Removed map object for {charaID}");
            }
        }

        public static GameObject MakeExpeditionObject(GameObject newObject, string templateSkeletonID, CustomChara chara, Transform parent)
        {
            try
            {
                if (expeditionObjects.ContainsKey(chara.charaID))
                {
                    GameObject obj = expeditionObjects[chara.charaID];
                    if (obj != null)
                    {
                        UnityEngine.Object.Destroy(obj);
                    }
                }

                GameObject returnObject = objectFactory.CreateCustomExpeditionMapObject(newObject, templateSkeletonID, chara, parent);

                if (returnObject != null)
                {
                    expeditionObjects[chara.charaID] = returnObject;
                }

                return returnObject;
            }
            catch (Exception e)
            {
                ModInstance.log("Error while trying to create expedition map object: " + e.Message);
                ModInstance.log(e.StackTrace);
                return null;
            }
        }
    }
}
