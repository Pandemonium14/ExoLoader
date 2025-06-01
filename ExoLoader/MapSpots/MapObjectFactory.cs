using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using Spine.Unity;

namespace ExoLoader
{
    internal class MapObjectFactory
    {
        // Preloaded skeleton data for static art objects
        private readonly TextAsset staticSkeletonData;
        private readonly TextAsset staticSkeletonAtlas;

        public MapObjectFactory()
        {
            // Load static skeleton data once during construction
            string staticSkeletonDataPath = Path.Combine(CFileManager.commonFolderPath, "skeleton", "skeleton.json");
            string staticSkeletonAtlasPath = Path.Combine(CFileManager.commonFolderPath, "skeleton", "skeleton.atlas");

            try
            {
                staticSkeletonData = new TextAsset(File.ReadAllText(staticSkeletonDataPath));
                staticSkeletonAtlas = new TextAsset(File.ReadAllText(staticSkeletonAtlasPath));
                ModInstance.log("Successfully loaded static skeleton data in MapObjectFactory constructor");
            }
            catch (Exception e)
            {
                ModInstance.log($"Failed to load static skeleton data: {e.Message}");
                staticSkeletonData = null;
                staticSkeletonAtlas = null;
            }
        }

        public GameObject GetMapObjectTemplate(string charaId, string season, int week)
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
                if (byWeek != null)
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
                ModInstance.log("Couldn't find map object for " + charaId + " " + season + " " + week.ToString());
                return null;
            }

            return directT.gameObject;
        }

        public GameObject CreateCustomMapObject(GameObject templateObject, CustomChara chara, string scene)
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

            List<Transform> artAgeTransforms = SetupArtObjects(newObject, chara);

            if (!SetupCharaSwitcher(newObject, chara, artAgeTransforms))
            {
                return null;
            }

            newObject.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject actualSpot = PoolManager.Spawn(newObject, GameObject.Find("Seasonal").transform);
            actualSpot.transform.localPosition = newObject.transform.localPosition;
            newObject.DestroySafe();

            return actualSpot;
        }

        private List<Transform> SetupArtObjects(GameObject newObject, CustomChara chara)
        {
            List<Transform> artAgeTransforms = new List<Transform>();

            for (int i = 1; i <= 3; i++)
            {
                GameObject artObject = newObject.transform.Find(chara.data.skeleton).Find(chara.data.skeleton + i.ToString()).gameObject;
                ModInstance.log("Got " + i.ToString() + "th art object");

                if (artObject != null)
                {
                    ModInstance.log("Art object is named " + artObject.name);
                    float ageScale = (chara.data.overworldScales[i - 1] != 0f) ? chara.data.overworldScales[i - 1] : 0.004f;
                    artObject.name = chara.charaID + i.ToString();
                    ModifyArtObject(artObject, chara, i, ageScale);
                    ModInstance.log("Modified " + i.ToString() + "th art object");
                    artAgeTransforms.Add(artObject.transform);
                }
            }

            return artAgeTransforms;
        }

        private bool SetupCharaSwitcher(GameObject newObject, CustomChara chara, List<Transform> artAgeTransforms)
        {
            CharaSwitcher charaSwitcher = newObject.GetComponentInChildren<CharaSwitcher>();
            charaSwitcher.name = chara.charaID + "switcher";

            try
            {
                FieldInfo fInfo = typeof(CharaSwitcher).GetField("artAgeTransforms", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fInfo != null)
                {
                    fInfo.SetValue(charaSwitcher, artAgeTransforms);
                    return true;
                }
                else
                {
                    ModInstance.log("FieldInfo was null");
                    return false;
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Reflection on chara switcher failed...");
                ModInstance.log(e.Message);
                return false;
            }
        }

        private void ModifyArtObject(GameObject artObject, CustomChara chara, int artStage, float scale)
        {
            string spriteFolder = Path.Combine(chara.data.folderName, "Sprites", chara.charaID + "_model_" + artStage.ToString());

            if (!Directory.Exists(spriteFolder))
            {
                ModInstance.log($"Sprite folder {spriteFolder} does not exist for {chara.charaID} art stage {artStage}");
                ModifyStaticArtObject(artObject, chara, artStage, scale);
                return;
            }

            string skeletonDataPath = Directory.GetFiles(spriteFolder, "*.json").FirstOrDefault();
            if (string.IsNullOrEmpty(skeletonDataPath))
            {
                ModInstance.log($"No skeleton data found in {spriteFolder} for {chara.charaID} art stage {artStage}");
                ModifyStaticArtObject(artObject, chara, artStage, scale);
                return;
            }

            ModInstance.log($"Found skeleton data at {skeletonDataPath} for {chara.charaID} art stage {artStage}");

            string skeletonAtlasPath = Directory.GetFiles(spriteFolder, "*.atlas").FirstOrDefault();
            if (string.IsNullOrEmpty(skeletonAtlasPath))
            {
                ModInstance.log($"No skeleton atlas found in {spriteFolder} for {chara.charaID} art stage {artStage}");
                ModifyStaticArtObject(artObject, chara, artStage, scale);
                return;
            }

            ModInstance.log($"Found skeleton atlas at {skeletonAtlasPath} for {chara.charaID} art stage {artStage}");

            string texturePath = Directory.GetFiles(spriteFolder, "*.png").FirstOrDefault();
            if (string.IsNullOrEmpty(texturePath))
            {
                ModInstance.log($"No texture found in {spriteFolder} for {chara.charaID} art stage {artStage}");
                ModifyStaticArtObject(artObject, chara, artStage, scale);
                return;
            }

            ModInstance.log($"Found texture at {texturePath} for {chara.charaID} art stage {artStage}");

            ReplaceSkeletonData(artObject, skeletonDataPath, skeletonAtlasPath, texturePath, scale);
        }

        private void ReplaceSkeletonData(GameObject existingSpineObject, string jsonPath, string atlasPath, string texturePath, float scale)
        {
            SkeletonMecanim skeletonMecanim = existingSpineObject.GetComponent<SkeletonMecanim>();

            if (skeletonMecanim == null)
            {
                ModInstance.log("No SkeletonMecanim component found!");
                return;
            }

            Shader shader = existingSpineObject.GetComponent<MeshRenderer>().materials[0].shader;
            if (shader == null) throw new Exception("shader is null");

            // Store original rendering settings
            MeshRenderer meshRenderer = existingSpineObject.GetComponent<MeshRenderer>();
            Material originalMaterial = null;
            int originalSortingLayer = 0;
            int originalSortingOrder = 0;

            if (meshRenderer != null)
            {
                originalMaterial = meshRenderer.material;
                originalSortingLayer = meshRenderer.sortingLayerID;
                originalSortingOrder = meshRenderer.sortingOrder;
            }
            else
            {
                ModInstance.log("MeshRenderer not found on existing spine object, using default settings.");
            }

            SkeletonDataAsset existingSkeletonDataAsset = skeletonMecanim.skeletonDataAsset;

            if (existingSkeletonDataAsset == null)
            {
                ModInstance.log("No existing SkeletonDataAsset found on the SkeletonMecanim component.");
            }

            // Load texture
            byte[] textureBytes = File.ReadAllBytes(texturePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(textureBytes);
            texture.name = Path.GetFileNameWithoutExtension(texturePath);

            // Load atlas and skeleton data
            TextAsset atlasText = new TextAsset(File.ReadAllText(atlasPath));
            TextAsset skeletonJson = new TextAsset(File.ReadAllText(jsonPath));
            Material materialPropertySource = existingSpineObject.GetComponent<MeshRenderer>().materials[0];

            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(atlasText, new Texture2D[] { texture }, materialPropertySource, true);

            // Validate atlasAsset
            if (atlasAsset == null || atlasAsset.Materials == null || atlasAsset.Materials.Count() == 0)
            {
                ModInstance.log("Failed to create SpineAtlasAsset from provided atlas text and texture.");
                return;
            }

            var skeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(skeletonJson, atlasAsset, true, scale);

            ModInstance.log($"Assigning new asset: {skeletonDataAsset.name}, valid: {skeletonDataAsset.GetSkeletonData(true) != null}");

            // Replace the skeleton data asset
            skeletonMecanim.enabled = false;
            skeletonMecanim.skeletonDataAsset = skeletonDataAsset;
            skeletonMecanim.enabled = true;

            // Clear and reinitialize
            skeletonMecanim.ClearState();
            skeletonMecanim.Initialize(true);

            // Fix rendering after initialization
            meshRenderer = existingSpineObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Restore sorting settings
                meshRenderer.sortingLayerID = originalSortingLayer;
                meshRenderer.sortingOrder = originalSortingOrder;

                // Make sure it's enabled
                meshRenderer.enabled = true;

                // Check if material needs to be restored/fixed
                if (meshRenderer.material == null && originalMaterial != null)
                {
                    meshRenderer.material = originalMaterial;
                }
            }
            else
            {
                ModInstance.log("MeshRenderer not found on existing spine object after initialization, using default settings.");
            }

            // Force update the skeleton
            if (skeletonMecanim.skeleton != null)
            {
                skeletonMecanim.skeleton.SetSkin("default");
                skeletonMecanim.skeleton.SetToSetupPose();
                skeletonMecanim.LateUpdate(); // Force a rendering update
            }
            else
            {
                ModInstance.log("SkeletonMecanim skeleton is null after initialization, cannot force update.");
            }

            ModInstance.log($"Skeleton replaced. Bone count: {skeletonMecanim.skeleton?.Bones?.Count ?? 0}");
        }

        private void ModifyStaticArtObject(GameObject artObject, CustomChara chara, int artStage, float scale)
        {
            artObject.name = chara.charaID + artStage.ToString();

            try
            {
                // Check if preloaded skeleton data is available
                if (staticSkeletonData == null || staticSkeletonAtlas == null)
                {
                    ModInstance.log("Static skeleton data not loaded, cannot modify static art object");
                    return;
                }

                Shader shader = artObject.GetComponent<MeshRenderer>().materials[0].shader;
                if (shader == null) throw new Exception("shader is null");

                Texture2D[] textures = new Texture2D[1];
                textures[0] = CFileManager.GetCustomImage(chara.data.folderName, chara.charaID + "_model_" + artStage.ToString()).texture;
                textures[0].name = "skeleton";

                // Use preloaded skeleton data instead of loading from file
                SpineAtlasAsset spineAtlas = SpineAtlasAsset.CreateRuntimeInstance(staticSkeletonAtlas, textures, shader, true);
                SkeletonDataAsset skeData = SkeletonDataAsset.CreateRuntimeInstance(staticSkeletonData, spineAtlas, true, scale);

                try
                {
                    artObject.GetComponent<SkeletonMecanim>().skeletonDataAsset = skeData;
                    artObject.GetComponent<SkeletonMecanim>().Initialize(true);
                }
                catch (Exception e)
                {
                    ModInstance.log("Failed to initialize SkeletonMecanim: " + e.Message);
                    return;
                }

                if (shader == null) throw new Exception("shader is null after destroy");

                artObject.AddComponent<MeshRenderer>();
                MeshRenderer newMeshRenderer = artObject.GetComponent<MeshRenderer>();
                if (newMeshRenderer == null) throw new Exception("newMesh is null");
                newMeshRenderer.sharedMaterial = new Material(shader);
                newMeshRenderer.sharedMaterial.SetTexture("_MainTex", CFileManager.GetTexture(Path.Combine(CFileManager.commonFolderPath, "skeleton", "stickman.png")));
            }
            catch (Exception e)
            {
                ModInstance.log(e.StackTrace);
                ModInstance.log(e.Message);
            }
        }
    }
}
