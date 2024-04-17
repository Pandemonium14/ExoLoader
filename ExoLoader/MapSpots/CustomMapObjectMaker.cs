using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Spine.Unity;
using Spine;
using System.IO;
using Newtonsoft.Json;

namespace ExoLoader
{
    public class CustomMapObjectMaker
    {
        public static Dictionary<string,Tuple<GameObject,Transform>> mapObjects = new Dictionary<string, Tuple<GameObject, Transform>>();


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

        private static GameObject GetMom(string season, int week)
        {
            return null;
        }

        //returns the map object modified to match the customChara along with the Transform it should be a child of
        public static Tuple<GameObject, Transform> MakeCustomMapObject(string customCharaId, string season, int week, string scene)
        {
            ModInstance.log("Entered model factory");
            string maybeKey = customCharaId + "_" + season + "_month" + (season != "glow" ? week.ToString() : "");
            if (mapObjects.ContainsKey(maybeKey))
            {
                return mapObjects[maybeKey];
            }
            CustomChara cC = CustomChara.customCharasById[customCharaId];
            if (cC == null)
            {
                ModInstance.log("Tried making a model for non-custom chara");
                return null;
            }

            if (!(((scene.Equals("strato") || scene.Equals("stratodestroyed")) && !cC.data.helioOnly) || scene.Equals("helio")))
            {
                ModInstance.log("Tried making " + customCharaId + " model in unsuitable scene " + scene);
                return null;
            }

            GameObject templateObject = GetMapObject(cC.data.skeleton, season, week);
            
            if (templateObject == null) 
            {
                ModInstance.log("Couldn't get base map object, cancelling map spot creation for : " + customCharaId);
                return null; 
            }

            ModInstance.log("Got original map object (or is null if failed)");

            GameObject customMapObject = CopyAndModifyMapObject(templateObject, cC, scene);

            ModInstance.log("Copied and modified map object");

            Tuple <GameObject,Transform> result = new Tuple<GameObject, Transform>(customMapObject, templateObject.transform.parent);
            mapObjects.Add(maybeKey, result);
            return result;
        }

        private static GameObject CopyAndModifyMapObject(GameObject templateObject, CustomChara cC, string scene) 
        {
            GameObject newObject = GameObject.Instantiate(templateObject);
            //ModInstance.log("Copied Map object");

            //map spot modification
            MapSpot mapSpot = newObject.GetComponent<MapSpot>();
            //ModInstance.log("MapSpot got");
            mapSpot.charaID = cC.charaID;
            //ModInstance.log("MapSpot charaID changed");

            try
            {
                //MethodInfo mInfo = typeof(MapSpot).GetMethod("EnableTrigger", BindingFlags.NonPublic | BindingFlags.Instance);
                //mInfo.Invoke(mapSpot, new object[] { true });
            } 
            catch (Exception e)
            {
                ModInstance.log("reflection on mapSpot failed");
                ModInstance.log(e.Message);
            }
            //ModInstance.log("triggerDisabled on MapSpot is " + mapSpot.triggerDisabled.ToString());


            if (cC.data.helioOnly && (scene.Equals("strato") || scene.Equals("stratodestroyed")))
            {
                ModInstance.log("Can't modify map object in unsupported scene");
                return null;
            }
            if (scene.Equals("strato"))
            {
                newObject.transform.localPosition = new Vector3(cC.data.stratoMapSpot[0], cC.data.stratoMapSpot[1], cC.data.stratoMapSpot[2]);
            }
            else if (scene.Equals("helio"))
            {
                newObject.transform.localPosition = new Vector3(cC.data.helioMapSpot[0], cC.data.helioMapSpot[1], cC.data.helioMapSpot[2]);
            }
            else if (scene.Equals("stratodestroyed"))
            {
                newObject.transform.localPosition = new Vector3(cC.data.destroyedMapSpot[0], cC.data.destroyedMapSpot[1], cC.data.destroyedMapSpot[2]);
            }
            mapSpot.MoveToGround();
            //ModInstance.log("Changed position of map object");
            List<Transform> artAgeTransforms = new List<Transform>(); //this is for the chara switcher

            ModInstance.log("Preparing to find and modify art objects");
            //art object modification
            GameObject artObject0 = null; //newObject.transform.Find(cC.data.skeleton + "0").gameObject;
            if (artObject0 != null)
            {
                ModifyArtObject(artObject0, cC, 0);
                artAgeTransforms.Add(artObject0.transform);
            } else
            {
                for (int i = 1; i <= 3; i++)
                {
                    //ModInstance.log("Searching " + i.ToString() + "th art object");
                    GameObject artObject = newObject.transform.Find(cC.data.skeleton).Find(cC.data.skeleton + i.ToString()).gameObject;
                    ModInstance.log("Got " + i.ToString() + "th art object");
                    if (artObject != null)
                    {
                        //ModInstance.log("Art object is named " + artObject.name);
                        artObject.name = cC.charaID + i.ToString();
                        ModifyArtObject(artObject, cC, i);
                        //ModInstance.log("Modified " + i.ToString() + "th art object");
                        artAgeTransforms.Add(artObject.transform);
                    }
                }
            }
            ModInstance.log("art object modified");

            //Chara Switcher modification
            CharaSwitcher charaSwitcher = newObject.GetComponentInChildren<CharaSwitcher>();
            charaSwitcher.name = cC.charaID + "switcher";
            try
            {
                FieldInfo fInfo = typeof(CharaSwitcher).GetField("artAgeTransforms", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fInfo != null)
                {
                    fInfo.SetValue(charaSwitcher, artAgeTransforms);
                } else
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
            
            //charaSwitcher.DestroySafe();
            ModInstance.log("CharaSwitcher removed");
            //newObject.GetComponent<SkeletonMecanim>().valid = true;
            newObject.transform.localScale = new Vector3(1f,1f,1f);
            return newObject;            
        }

        private static void ModifyArtObject(GameObject artObject, CustomChara cC, int artStage)
        {
            artObject.name = cC.charaID + artStage.ToString();

            //artObject.GetComponent<MeshRenderer>().material.name = artObject.name + "_Material";
            //artObject.GetComponent<MeshRenderer>().material.mainTexture = FileManager.GetCustomImage(cC.data.folderName, cC.charaID + "_model_" + artStage.ToString()).texture;
            //artObject.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("newTexture" + artObject.name, FileManager.GetCustomImage(cC.data.folderName, cC.charaID + "_model_" + artStage.ToString()).texture);
            //artObject.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_mainTex", new Color(255, 0, 0));
            //artObject.GetComponent<MeshRenderer>().materials[0].SetTexture("_MainTex", FileManager.GetCustomImage(cC.data.folderName, cC.charaID + "_model_" + artStage.ToString()).texture);
            //Material mat = artObject.GetComponent<MeshRenderer>().materials[0] = new Material(artObject.GetComponent<MeshRenderer>().materials[0]);
            //mat.mainTexture = FileManager.GetCustomImage(cC.data.folderName, cC.charaID + "_model_" + artStage.ToString()).texture;

            try
            {
                Shader shader = artObject.GetComponent<MeshRenderer>().materials[0].shader;
                if (shader == null) throw new Exception("shader is null");


                
                string skeletonDataPath = Path.Combine(CFileManager.commonFolderPath, "skeleton", "skeleton.json");
                string skeletonAtlasPath = Path.Combine(CFileManager.commonFolderPath, "skeleton", "skeleton.atlas");
                TextAsset skeDataFile = new TextAsset(File.ReadAllText(skeletonDataPath));
                TextAsset skeAtlasFile = new TextAsset(File.ReadAllText(skeletonAtlasPath));
               
                

                Texture2D[] textures = new Texture2D[1];
                textures[0] = CFileManager.GetCustomImage(cC.data.folderName, cC.charaID + "_model_" + artStage.ToString()).texture;
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
                ModInstance.log (e.StackTrace);
                ModInstance.log(e.Message);
            }
        }
    }
}
