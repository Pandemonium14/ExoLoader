using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Spine.Unity;

namespace ExoLoader
{
    internal class MapObjectFactory
    {
        public GameObject GetMapObjectTemplate(string charaId, string season, int week)
        {
            GameObject o = GameObject.Find("Seasonal");
            Transform seasonalTransform = o?.transform;
            Transform seasonTransform = seasonalTransform?.Find(season);

            if (seasonTransform == null)
            {
                GameObject childObject = o?.transform.GetComponentsInChildren<Transform>().FirstOrDefault(c => c.gameObject.name == "chara_" + charaId)?.gameObject;
                if (childObject != null)
                {
                    return childObject;
                }

                // This means we're likely in the expedition scene and trying to find Sym
                GameObject i = GameObject.Find("chara_" + charaId);

                if (i != null)
                {
                    return i;
                }

                return null;
            }

            Transform inner = seasonTransform.Find("inner");
            if (inner != null)
            {
                GameObject childObject = inner.GetComponentsInChildren<Transform>().FirstOrDefault(c => c.gameObject.name == "chara_" + charaId)?.gameObject;

                if (childObject != null)
                {
                    return childObject;
                }
            }

            Transform weekT = seasonTransform.Find("week" + week.ToString());
            if (weekT != null)
            {
                Transform weekTChara = weekT.Find("chara_" + charaId);

                if (weekTChara != null)
                {
                    return weekTChara.gameObject;
                }
            }

            Transform weekPlusT = seasonTransform.Find("week" + week.ToString() + "plus");
            if (weekPlusT != null)
            {
                Transform weekPlusTChara = weekPlusT.Find("chara_" + charaId);
                if (weekPlusTChara != null)
                {
                    return weekPlusTChara.gameObject;
                }
            }

            if (charaId == "dys")
            {
                Transform dysSurvey = seasonTransform.Find("toggleDysSurvey");

                if (dysSurvey != null)
                {
                    Transform dysSurveyChara = dysSurvey.Find("chara_" + charaId);
                    if (dysSurveyChara != null)
                    {
                        return dysSurveyChara.gameObject;
                    }
                }

                Transform dysNoSurvey = seasonTransform.Find("toggleDysNoSurvey");
                if (dysNoSurvey != null)
                {
                    Transform dysNoSurveyChara = dysNoSurvey.Find("chara_" + charaId);
                    if (dysNoSurveyChara != null)
                    {
                        return dysNoSurveyChara.gameObject;
                    }
                }
            }

            Transform directT = seasonTransform.Find("chara_" + charaId);
            if (directT == null)
            {
                return null;
            }

            return directT.gameObject;
        }

        public GameObject CreateCustomMapObject(GameObject templateObject, string templateSkeletonID, CustomChara chara, string scene)
        {
            float[] mapSpotPosition = chara.GetMapSpot(scene, Princess.season.seasonID);

            if (mapSpotPosition == null)
            {
                ModInstance.log($"Can't modify map object in unsupported scene: {scene} for chara {chara.charaID}");
                return null;
            }

            GameObject newObject = UnityEngine.Object.Instantiate(templateObject);
            newObject.name = "chara_" + chara.charaID;

            MapSpot mapSpot = newObject.GetComponent<MapSpot>();

            if (mapSpot == null)
            {
                ModInstance.log("MapSpot component not found on the new object for " + chara.charaID);
                return null;
            }

            mapSpot.charaID = chara.charaID;
            newObject.transform.localPosition = new Vector3(mapSpotPosition[0], mapSpotPosition[1], mapSpotPosition[2]);
            mapSpot.MoveToGround();

            List<Transform> artAgeTransforms = SetupArtObjects(templateSkeletonID, newObject, chara);

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

        private List<Transform> SetupArtObjects(string templateSkeletonID, GameObject newObject, CustomChara chara)
        {
            List<Transform> artAgeTransforms = new List<Transform>();

            for (int i = 1; i <= 3; i++)
            {
                GameObject artObject = newObject.transform.Find(templateSkeletonID)?.Find(templateSkeletonID + i.ToString())?.gameObject;
                bool isZeroStage = false;

                if (artObject)
                {
                    ModInstance.log("Got " + i.ToString() + "th art object");
                }

                if (artObject == null && (
                    templateSkeletonID == "sym" ||
                    templateSkeletonID == "mom" ||
                    templateSkeletonID == "dad" ||
                    templateSkeletonID == "utopia"
                ))
                {
                    artObject = newObject.transform.Find(templateSkeletonID).Find(templateSkeletonID + "0")?.gameObject;

                    if (artObject)
                    {
                        isZeroStage = true;
                        ModInstance.log("Using 0th art object from " + templateSkeletonID);
                    }
                }

                if (artObject != null)
                {
                    int currentStage = isZeroStage ? Princess.artStage : i;

                    ModInstance.log("Art object is named " + artObject.name);
                    float ageScale = (chara.data.overworldScales[currentStage - 1] != 0f) ? chara.data.overworldScales[currentStage - 1] : 0.004f;
                    artObject.name = chara.charaID + currentStage.ToString();
                    ModifyArtObject(artObject, chara, currentStage, ageScale);
                    ModInstance.log("Modified " + currentStage.ToString() + "th art object");
                    artAgeTransforms.Add(artObject.transform);

                    if (isZeroStage)
                    {
                        break;
                    }
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

        private void ModifyArtObject(GameObject existingSpineObject, CustomChara chara, int artStage, float scale)
        {
            string name = chara.charaID + "_model";

            if (chara.data.ages)
            {
                name += "_" + artStage.ToString();
            }

            SkeletonAsset skeletonAsset = ModAssetManager.GetSkeletonData(AssetContentType.CharacterModel, name);
            if (skeletonAsset == null)
            {
                ModInstance.log($"Skeleton {name} asset not found for {chara.charaID} art stage {artStage}");
                return;
            }

            TextAsset atlasText = skeletonAsset.AtlasText;
            TextAsset skeletonJson = skeletonAsset.SkeletonJson;
            Texture2D[] textures = skeletonAsset.Textures;
            bool isAnimated = skeletonAsset.isAnimated;
            if (atlasText == null || skeletonJson == null || textures == null || textures.Length == 0)
            {
                ModInstance.log($"Invalid skeleton asset for {chara.charaID} art stage {artStage}");
                return;
            }

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

            Material materialPropertySource = existingSpineObject.GetComponent<MeshRenderer>().materials[0];

            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(atlasText, textures, materialPropertySource, true);

            if (atlasAsset == null || atlasAsset.Materials == null || atlasAsset.Materials.Count() == 0)
            {
                ModInstance.log("Failed to create SpineAtlasAsset from provided atlas text and texture.");
                return;
            }

            var skeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(skeletonJson, atlasAsset, true, scale);

            skeletonMecanim.enabled = false;
            skeletonMecanim.skeletonDataAsset = skeletonDataAsset;
            skeletonMecanim.enabled = true;

            if (isAnimated)
            {
                skeletonMecanim.ClearState();
                skeletonMecanim.Initialize(true);
            }

            meshRenderer = existingSpineObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerID = originalSortingLayer;
                meshRenderer.sortingOrder = originalSortingOrder;
                meshRenderer.enabled = true;

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
                if (isAnimated)
                {
                    skeletonMecanim.skeleton.SetSkin("default");
                    skeletonMecanim.skeleton.SetToSetupPose();
                    skeletonMecanim.LateUpdate();
                }

                ModInstance.log($"Skeleton replaced. Bone count: {skeletonMecanim.skeleton?.Bones?.Count ?? 0}");
            }

            if (!isAnimated)
            {
                Sprite[] spriteFrames = ModAssetManager.GetSpriteAnimation(AssetContentType.CharacterSpriteModel, chara.charaID + "_model_" + artStage.ToString());
                if (spriteFrames != null && spriteFrames.Length > 0)
                {
                    GameObject spriteAnimationObject = new GameObject("SpriteAnimation_" + chara.charaID);
                    spriteAnimationObject.transform.SetParent(existingSpineObject.transform);
                    spriteAnimationObject.transform.localPosition = Vector3.zero;
                    spriteAnimationObject.transform.localRotation = Quaternion.identity;
                    spriteAnimationObject.transform.localScale = Vector3.one;

                    SpriteRenderer spriteRenderer = spriteAnimationObject.AddComponent<SpriteRenderer>();
                    spriteRenderer.material = originalMaterial;

                    MeshRenderer originalMeshRenderer = existingSpineObject.GetComponent<MeshRenderer>();
                    if (originalMeshRenderer != null)
                    {
                        spriteRenderer.sortingLayerID = originalSortingLayer;
                        spriteRenderer.sortingOrder = originalSortingOrder;
                        originalMeshRenderer.enabled = false;
                    }

                    SpriteAnimationPlayer animationPlayer = spriteAnimationObject.AddComponent<SpriteAnimationPlayer>();
                    animationPlayer.sprites = spriteFrames;
                    animationPlayer.frameRate = chara.data.spriteFrameRates[artStage - 1];

                    animationPlayer.PlayAnimation();

                    ModInstance.log($"Created sprite animation for {chara.charaID} art stage {artStage} with {spriteFrames.Length} frames");
                }
            }

            // FIXME: figure out later if we need to move the speech bubble, sometimes it looks weird
            // Transform speechBubbleTransform = existingSpineObject.transform.Find("speechBubble");
            // ModInstance.log("Got speech bubble transform " + speechBubbleTransform?.name);
        }
    }
}
