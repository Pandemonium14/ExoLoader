using System;
using System.Collections;
using System.IO;
using System.Linq;
using Northway.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ExoLoader
{
    public class ModAssetLoader
    {
        public static bool Loaded = false;

        private static TextAsset staticSkeletonData;
        private static TextAsset staticSkeletonAtlas;

        public static NWText loadingText;

        public static IEnumerator LoadWithProgress()
        {
            if (Loaded)
                yield break;

            ModLoadingStatus.ClearErrors();

            if (LoadingProgress.Instance == null)
            {
                ModInstance.instance.Log("Creating LoadingProgress instance...");
                GameObject overlayObj = new GameObject("LoadingProgress");
                overlayObj.AddComponent<LoadingProgress>();
            }

            ModInstance.instance.Log("Loading mod assets...");
            LoadingProgress.Instance?.Show("Loading mod assets...");

            if (LoadingProgress.Instance != null)
                LoadingProgress.Instance.UpdateProgress(0.1f, "Loading skeleton data...");
            yield return null;

            LoadStaticSkeletonData();

            if (LoadingProgress.Instance != null)
                LoadingProgress.Instance.UpdateProgress(0.2f, "Loading characters...");
            yield return null;

            LoadCharacterAssets();

            if (LoadingProgress.Instance != null)
                LoadingProgress.Instance.UpdateProgress(0.7f, "Loading backgrounds...");
            yield return null;

            LoadBackgrounds();

            if (LoadingProgress.Instance != null)
                LoadingProgress.Instance.UpdateProgress(0.9f, "Loading cards...");
            yield return null;

            LoadCards();

            if (LoadingProgress.Instance != null)
                LoadingProgress.Instance.UpdateProgress(1.0f, "ExoLoader loading complete!");
            yield return null;

            Loaded = true;
            ModInstance.instance.Log("Mod assets loaded successfully.");

            if (ModLoadingStatus.HasErrors())
            {
                ModInstance.log("Mod loading completed with errors. Check ModLoadingStatus for details.");
                GameObject overlayObject = new GameObject("LoadingErrorOverlay");
                overlayObject.AddComponent<LoadingErrorOverlay>();
                UnityEngine.Object.DontDestroyOnLoad(overlayObject);
            }
        }

        public static void LoadStaticSkeletonData()
        {
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

        #region Characters
        public static void LoadCharacterAssets()
        {
            foreach (CustomChara chara in CustomChara.customCharasById.Values)
            {
                if (chara == null || chara.data == null)
                    continue;

                ModInstance.instance.Log($"Loading assets for character: {chara.charaID}");

                string folderName = chara.data.folderName;
                string spritesFolder = Path.Combine(folderName, "Sprites");

                for (int artStage = 1; artStage <= 3; artStage++)
                {
                    bool SilentErrors = artStage == 1 && chara.data.helioOnly;
                    // Portrait for the art stage
                    string portraitName = $"portrait_{chara.charaID}{artStage}";
                    LoadCharacterPortrait(spritesFolder, portraitName, SilentErrors);

                    // Load character models
                    if (chara.data.onMap)
                    {
                        string modelName = $"{chara.charaID}_model_{artStage}";
                        LoadCharacterModel(spritesFolder, modelName, SilentErrors);
                    }

                    // Story sprites
                    string storySpritePrefix = $"{chara.charaID}{artStage}_";
                    int targetSpriteSize = Math.Max(chara.data.spriteSize, chara.data.spriteSizes[artStage - 1]);
                    LoadCharacterStorySprites(spritesFolder, storySpritePrefix, SilentErrors, targetSpriteSize);
                }

                // Main menu sprite
                if (chara.data.mainMenu != null)
                {
                    string mainMenuSpriteName = $"chara_{chara.charaID}";
                    LoadCharacterMainMenuSprite(spritesFolder, mainMenuSpriteName);
                }

                string adultStorySpritePrefix = $"{chara.charaID}_";
                LoadCharacterStorySprites(spritesFolder, adultStorySpritePrefix, false, chara.data.spriteSize);
            }
        }

        private static void LoadCharacterPortrait(string spritesFolder, string portraitName, bool SilentErrors)
        {
            try
            {
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = portraitName,
                    SilentErrors = SilentErrors
                };
                Sprite portrait = ImageUtils.LoadSprite(Path.Combine(spritesFolder, portraitName + ".png"), config);
                ModAssetManager.StoreSprite(AssetContentType.CharacterPortrait, portraitName, portrait);
            }
            catch (Exception e)
            {
                if (!SilentErrors)
                {
                    ModInstance.log($"Failed to load character portrait: {portraitName}");
                    ModLoadingStatus.LogError($"Error loading portrait {portraitName}: {e.Message}");
                }
            }
        }

        private static void LoadCharacterModel(string spritesFolder, string modelName, bool SilentErrors)
        {
            try
            {
                string modelFolder = Path.Combine(spritesFolder, modelName);
                bool modelDirectoryExists = Directory.Exists(modelFolder);
                string atlasDataPath = modelDirectoryExists ? Directory.GetFiles(modelFolder, "*.atlas").FirstOrDefault() : null;
                string skeletonDataPath = modelDirectoryExists ? Directory.GetFiles(modelFolder, "*.json").FirstOrDefault() : null;
                bool isAnimated = modelDirectoryExists && Directory.GetFiles(modelFolder, "*.png").Length > 0 && !string.IsNullOrEmpty(skeletonDataPath) && !string.IsNullOrEmpty(atlasDataPath);

                if (isAnimated)
                {
                    Texture2D[] textures = ImageUtils.LoadTexturesFromFolder(modelFolder, SilentErrors);
                    TextAsset atlasText = new TextAsset(File.ReadAllText(atlasDataPath));
                    TextAsset skeletonJson = new TextAsset(File.ReadAllText(skeletonDataPath));

                    if (textures != null && textures.Length > 0 && atlasText != null && skeletonJson != null)
                    {
                        ModAssetManager.StoreSkeletonData(AssetContentType.CharacterModel, modelName, textures, atlasText, skeletonJson);
                        ModInstance.instance.Log($"Loaded animated character model: {modelName}");
                    }
                    else
                    {
                        if (!SilentErrors)
                        {
                            ModInstance.log($"Failed to load animated character model: {modelName}. Missing assets.");
                            ModInstance.log($"Textures: {textures?.Length ?? 0}, Atlas: {atlasText != null}, Skeleton: {skeletonJson != null}");
                            throw new Exception($"Missing assets for animated character model: {modelName}");
                        }
                    }
                }
                else
                {
                    Texture2D texture = ImageUtils.LoadTexture(Path.Combine(spritesFolder, modelName + ".png"), SilentErrors);

                    if (texture != null)
                    {
                        ModAssetManager.StoreSkeletonData(AssetContentType.CharacterModel, modelName, new[] { texture }, staticSkeletonAtlas, staticSkeletonData);
                        ModInstance.instance.Log($"Loaded static character model: {modelName}");
                    }
                    else
                    {
                        if (!SilentErrors)
                        {
                            ModInstance.log($"Failed to load static character model: {modelName}. Texture not found.");
                            throw new Exception($"Texture not found for static character model: {modelName}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!SilentErrors)
                {
                    ModInstance.log($"Failed to load character model: {modelName}: {e.Message}");
                    ModLoadingStatus.LogError($"Error loading model {modelName}: {e.Message}");
                }
            }
        }

        private static void LoadCharacterMainMenuSprite(string spritesFolder, string spriteName)
        {
            try
            {
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = spriteName,
                    PixelsPerUnit = 100
                };
                Sprite mainMenuSprite = ImageUtils.LoadSprite(Path.Combine(spritesFolder, spriteName + ".png"), config);
                ModAssetManager.StoreSprite(AssetContentType.CharacterMainMenu, spriteName, mainMenuSprite);
                ModInstance.instance.Log($"Loaded main menu sprite: {spriteName}");
            }
            catch (Exception e)
            {
                ModInstance.log($"Failed to load main menu sprite: {spriteName}");
                ModLoadingStatus.LogError($"Error loading main menu sprite {spriteName}: {e.Message}");
            }
        }

        private static void LoadCharacterStorySprite(string spritesFolder, string spriteName, bool SilentErrors, int targetSpriteSize)
        {
            try
            {
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = spriteName,
                    TargetHeightInUnits = targetSpriteSize
                };
                Sprite storySprite = ImageUtils.LoadSprite(Path.Combine(spritesFolder, spriteName + ".png"), config);
                ModAssetManager.StoreSprite(AssetContentType.CharacterStory, spriteName, storySprite);
                ModInstance.instance.Log($"Loaded story sprite: {spriteName}");
            }
            catch (Exception e)
            {
                if (!SilentErrors)
                {
                    ModInstance.log($"Failed to load story sprite: {spriteName}");
                    ModLoadingStatus.LogError($"Error loading story sprite {spriteName}: {e.Message}");
                }
            }
        }

        private static void LoadCharacterStorySprites(string spritesFolder, string prefix, bool SilentErrors, int targetSpriteSize = 16)
        {
            try
            {
                string[] storySprites = Directory.GetFiles(spritesFolder, $"{prefix}*.png")
                    .Where(file => !file.Contains("model_")).ToArray();

                foreach (string spritePath in storySprites)
                {
                    string spriteName = Path.GetFileNameWithoutExtension(spritePath);
                    LoadCharacterStorySprite(spritesFolder, spriteName, SilentErrors, targetSpriteSize);
                }
            }
            catch (Exception e)
            {
                if (!SilentErrors)
                {
                    ModInstance.log($"Failed to load story sprites for character: {prefix}");
                    ModLoadingStatus.LogError($"Error loading story sprites for {prefix}: {e.Message}");
                }
            }
        }

        #endregion

        #region Backgrounds

        public static void LoadBackgrounds()
        {
            ModInstance.instance.Log("Loading custom backgrounds...");

            foreach (var kvp in CustomBackground.allBackgrounds)
            {
                string spriteName = kvp.Key;
                CustomBackground background = kvp.Value;

                if (background == null || string.IsNullOrEmpty(background.file))
                {
                    ModInstance.log($"Skipping background {spriteName} due to missing file path.");
                    continue;
                }

                LoadBackground(spriteName, background);
            }

            ModInstance.instance.Log("Custom backgrounds loaded successfully.");
        }

        private static void LoadBackground(string spriteName, CustomBackground background)
        {
            try
            {
                ModInstance.instance.Log($"Loading background: {spriteName} from {background.file}");

                string spritePath = Path.Combine(background.file, spriteName + ".png");
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = spriteName,
                    PixelsPerUnit = 1
                };
                Sprite sprite = ImageUtils.LoadSprite(spritePath, config);
                if (sprite != null)
                {
                    ModAssetManager.StoreSprite(AssetContentType.Background, spriteName, sprite);
                    ModInstance.instance.Log($"Loaded background sprite: {spriteName}");
                }
                else
                {
                    throw new Exception($"Sprite not found at path: {spritePath}");
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Failed to load background sprite: {spriteName}");
                ModLoadingStatus.LogError($"Error loading background sprite {spriteName}: {e.Message}");
            }
        }

        #endregion

        #region Cards

        public static void LoadCards()
        {
            ModInstance.instance.Log("Loading custom cards...");

            // Load all custom cards
            foreach (var kvp in CustomCardData.idToFile)
            {
                string cardID = kvp.Key;
                string cardFile = kvp.Value;

                LoadCard(cardID, cardFile);
            }

            ModInstance.instance.Log("Custom cards loaded successfully.");
        }

        private static void LoadCard(string cardID, string originFile)
        {
            try
            {
                string spriteName = "card_" + cardID;
                string path = originFile.Replace(".json", ".png");

                ModInstance.instance.Log($"Loading card: {cardID} from {path}");

                Sprite sprite = ImageUtils.LoadSprite(path, new SpriteLoadConfig
                {
                    SpriteName = spriteName
                });

                if (sprite != null)
                {
                    ModAssetManager.StoreSprite(AssetContentType.Card, cardID, sprite);
                    ModInstance.instance.Log($"Loaded card sprite: {cardID}");
                }
                else
                {
                    throw new Exception($"Sprite not found at path: {path}");
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Failed to load card sprite: {cardID}");
                ModLoadingStatus.LogError($"Error loading card sprite {cardID}: {e.Message}");
            }
        }

        #endregion

        #region Public (runtime loading)
        // We load background thumbnails and achievement icons at runtime (because they are only needed in the gallery)
        // FIXME

        #endregion
    }
}
