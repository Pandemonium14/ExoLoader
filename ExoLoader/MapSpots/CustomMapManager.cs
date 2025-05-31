using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using Spine.Unity;
using System.IO;

namespace ExoLoader
{
    internal class CustomMapManager
    {
        public static Dictionary<string, GameObject> mapObjects = new Dictionary<string, GameObject>();

        public static void MakeCustomMapObject(CustomChara chara, string season, int week, string scene)
        {
            try
            {
                if (!(((scene.Equals("strato") || scene.Equals("stratodestroyed")) && !chara.data.helioOnly) || scene.Equals("helio")))
                {
                    RemoveMapObject(chara.charaID);
                    ModInstance.log("Tried making " + chara.charaID + " model in unsuitable scene " + scene);
                    return;
                }

                ModInstance.log("Making map object for " + chara.charaID + " in scene " + scene);

                if (chara.isDead)
                {
                    RemoveMapObject(chara.charaID);
                    return;
                }

                if (mapObjects.ContainsKey(chara.charaID))
                    {
                        // Object exists, check if it needs to be moved
                        GameObject existingObject = mapObjects[chara.charaID];

                        if (existingObject != null)
                        {
                            ModInstance.log($"Found existing map object for {chara.charaID} in scene {scene}");
                            if (ShouldRemoveFromScene(chara, scene))
                            {
                                ModInstance.log($"Removing {chara.charaID} from scene {scene}");
                                RemoveMapObject(chara.charaID);
                                return;
                            }

                            if (ShouldMoveMapObject(existingObject, chara, scene))
                            {
                                ModInstance.log($"Moving map object for {chara.charaID} to new position");
                                MoveMapObject(existingObject, chara, scene);
                            }
                            else
                            {
                                ModInstance.log($"Map object for {chara.charaID} is already in correct position");
                            }
                            return;
                        }
                        else
                        {
                            ModInstance.log($"Existing map object for {chara.charaID} is null, creating a new one");
                        }
                    }

                GameObject templateObject = GetMapObject(chara.data.skeleton, season, week);

                if (templateObject == null)
                {
                    ModInstance.log("Couldn't get base map object, cancelling map spot creation for : " + chara.charaID);
                    return;
                }

                ModInstance.log("Got original map object (or is null if failed)");

                GameObject customMapObject = CopyAndModifyMapObject(templateObject, chara, scene);

                mapObjects[chara.charaID] = customMapObject;
            }
            catch (Exception e)
            {
                ModInstance.log("Error while trying to get map object for " + chara.charaID + ": " + e.Message);
                ModInstance.log(e.ToString());
                return;
            }
        }

        private static bool ShouldMoveMapObject(GameObject mapObject, CustomChara chara, string scene)
        {
            float[] targetPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (targetPosition == null)
            {
                // Character shouldn't be on this scene anymore
                ModInstance.log($"Character {chara.charaID} should not be on scene {scene}");
                return false;
            }

            Transform currentTransform = mapObject.GetComponentInChildren<Transform>();

            if (currentTransform == null)
            {
                ModInstance.log($"Map object for {chara.charaID} has no Transform component.");
                return true;
            }

            Vector3 currentPosition = currentTransform.localPosition;
            Vector3 targetPos = new Vector3(targetPosition[0], targetPosition[1], targetPosition[2]);

            // Check if positions are different (with small tolerance for floating point comparison)
            float tolerance = 0.01f;
            return Vector3.Distance(currentPosition, targetPos) > tolerance;
        }

        private static bool ShouldRemoveFromScene(CustomChara chara, string scene)
        {
            float[] position = chara.GetMapSpot(scene, Princess.season.seasonID);
            ModInstance.log($"Checking if {chara.charaID} should be removed from scene {scene}: position = {position} {position == null}");
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

            // Update position
            mapObject.transform.localPosition = new Vector3(newPosition[0], newPosition[1], newPosition[2]);

            // Move to ground if needed (like in your original code)
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

                if (obj == null)
                {
                    return;
                }

                mapObjects.Remove(charaID);

                // Clean up the object
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }

                ModInstance.log($"Removed map object for {charaID}");
            }
        }

        private static GameObject CopyAndModifyMapObject(GameObject templateObject, CustomChara chara, string scene)
        {
            GameObject newObject = UnityEngine.Object.Instantiate(templateObject);
            newObject.name = "chara_" + chara.charaID;

            MapSpot mapSpot = newObject.GetComponent<MapSpot>();
            mapSpot.charaID = chara.charaID;

            float[] mapSpotPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (mapSpotPosition == null)
            {
                ModInstance.log("Can't modify map object in unsupported scene");
                return null;
            }

            newObject.transform.localPosition = new Vector3(mapSpotPosition[0], mapSpotPosition[1], mapSpotPosition[2]);
            mapSpot.MoveToGround();

            List<Transform> artAgeTransforms = new List<Transform>(); //this is for the chara switcher

            for (int i = 1; i <= 3; i++)
            {
                //ModInstance.log("Searching " + i.ToString() + "th art object");
                GameObject artObject = newObject.transform.Find(chara.data.skeleton).Find(chara.data.skeleton + i.ToString()).gameObject;
                ModInstance.log("Got " + i.ToString() + "th art object");
                if (artObject != null)
                {
                    ModInstance.log("Art object is named " + artObject.name);
                    artObject.name = chara.charaID + i.ToString();
                    ModifyArtObject(artObject, chara, i);
                    ModInstance.log("Modified " + i.ToString() + "th art object");
                    artAgeTransforms.Add(artObject.transform);
                }
            }

            CharaSwitcher charaSwitcher = newObject.GetComponentInChildren<CharaSwitcher>();
            charaSwitcher.name = chara.charaID + "switcher";

            try
            {
                FieldInfo fInfo = typeof(CharaSwitcher).GetField("artAgeTransforms", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fInfo != null)
                {
                    fInfo.SetValue(charaSwitcher, artAgeTransforms);
                }
                else
                {
                    ModInstance.log("FieldInfo was null");
                    return null;
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Reflection on chara switcher failed...");
                ModInstance.log(e.Message);
                return null;
            }

            newObject.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject actualSpot = PoolManager.Spawn(newObject, GameObject.Find("Seasonal").transform);
            actualSpot.transform.localPosition = newObject.transform.localPosition;
            newObject.DestroySafe();

            return actualSpot;
        }

        private static void ModifyArtObject(GameObject artObject, CustomChara chara, int artStage)
        {
            artObject.name = chara.charaID + artStage.ToString();

            try
            {
                Shader shader = artObject.GetComponent<MeshRenderer>().materials[0].shader;
                if (shader == null) throw new Exception("shader is null");

                string skeletonDataPath = Path.Combine(CFileManager.commonFolderPath, "skeleton", "skeleton.json");
                string skeletonAtlasPath = Path.Combine(CFileManager.commonFolderPath, "skeleton", "skeleton.atlas");
                TextAsset skeDataFile = new TextAsset(File.ReadAllText(skeletonDataPath));
                TextAsset skeAtlasFile = new TextAsset(File.ReadAllText(skeletonAtlasPath));

                Texture2D[] textures = new Texture2D[1];
                textures[0] = CFileManager.GetCustomImage(chara.data.folderName, chara.charaID + "_model_" + artStage.ToString()).texture;
                textures[0].name = "skeleton";
                SpineAtlasAsset spineAtlas = SpineAtlasAsset.CreateRuntimeInstance(skeAtlasFile, textures, shader, true);

                SkeletonDataAsset skeData = SkeletonDataAsset.CreateRuntimeInstance(skeDataFile, spineAtlas, true, 0.004f);

                //Animator animator = artObject.GetComponent<Animator>();
                artObject.GetComponent<SkeletonMecanim>().skeletonDataAsset = skeData;
                artObject.GetComponent<SkeletonMecanim>().Initialize(true);

                //artObject.GetComponent<SkeletonMecanim>().skeleton = new Skeleton(skeData.GetSkeletonData(false));
                //artObject.GetComponent<SkeletonMecanim>().skeleton.UpdateWorldTransform();

                //UnityEngine.Object.Destroy(artObject.GetComponent<SkeletonMecanim>());

                if (shader == null) throw new Exception("shader is null after destroy");

                artObject.AddComponent<MeshRenderer>();
                MeshRenderer newMeshRenderer = artObject.GetComponent<MeshRenderer>();
                if (newMeshRenderer == null) throw new Exception("newMesh is null");
                newMeshRenderer.sharedMaterial = new Material(shader);
                newMeshRenderer.sharedMaterial.SetTexture("_MainTex", CFileManager.GetTexture(Path.Combine(CFileManager.commonFolderPath, "skeleton", "stickman.png")));
            }
            catch
            (Exception e)
            {
                ModInstance.log(e.StackTrace);
                ModInstance.log(e.Message);
            }
        }

        private static GameObject GetMapObject(string charaId, string season, int week)
        {
            GameObject o = GameObject.Find("Seasonal");
            Transform seasonalTransform = o.transform;
            Transform seasonTransform = seasonalTransform.Find(season);
            ModInstance.log("Got Season tranform");
            if (seasonTransform == null) 
            {
                ModInstance.log("Season transform is null!");
                return null;
            }

            Transform inner = seasonTransform.Find("inner");
            if (inner != null)
            {
                Transform direct = inner.Find("chara_" + charaId);
                if (direct != null) return direct.gameObject;
                Transform byWeek = seasonTransform.Find("week" + week.ToString());
                if (byWeek  != null)
                {
                    Transform attempt = byWeek.Find("chara_" + charaId);
                    if (attempt != null) return attempt.gameObject;
                }
            }

            Transform weekT = seasonTransform.Find("week" + week.ToString());
            if (weekT != null)
            {
                return weekT.Find("chara_" + charaId).gameObject;
            }

            Transform weekPlusT = seasonTransform.Find("week" + week.ToString() + "plus");
            if (weekPlusT != null)
            {
                return weekPlusT.Find("chara_" + charaId).gameObject;
            }

            Transform directT = seasonTransform.Find("chara_" + charaId);
            if (directT == null)
            {
                ModInstance.log("Couldn't find map object for " + charaId + " " +  season + " " + week.ToString());
                return null;
            }

            return directT.gameObject;
        }
    }
}
