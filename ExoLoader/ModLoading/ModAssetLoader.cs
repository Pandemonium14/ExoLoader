using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

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

            if (LoadingProgress.Instance == null)
            {
                ModInstance.log("Creating LoadingProgress instance...");
                GameObject overlayObj = new GameObject("LoadingProgress");
                overlayObj.AddComponent<LoadingProgress>();
            }

            ModInstance.log("Loading mod assets...");
            LoadingProgress.Instance?.Show("Loading mod assets...");

            LoadingProgress.Instance?.UpdateProgress(0.1f, "Loading skeleton data...");
            yield return null;

            LoadStaticSkeletonData();

            LoadingProgress.Instance?.UpdateProgress(0.2f, "Loading characters...");
            yield return null;

            LoadCharacterAssets();

            LoadingProgress.Instance?.UpdateProgress(0.7f, "Loading backgrounds...");
            yield return null;

            LoadBackgrounds();

            LoadingProgress.Instance?.UpdateProgress(0.8f, "Loading achievements...");
            yield return null;

            LoadAchievements();

            LoadingProgress.Instance?.UpdateProgress(0.9f, "Loading cards...");
            yield return null;
            yield return new WaitForSeconds(0.2f);

            LoadCards();

            LoadingProgress.Instance?.UpdateProgress(1.0f, "ExoLoader loading complete!");
            yield return null;
            yield return new WaitForSeconds(0.2f);

            Loaded = true;
            ModInstance.log("Mod assets loaded successfully.");

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

                ModInstance.log($"Loading assets for character: {chara.charaID}");

                string folderName = chara.data.folderName;
                string spritesFolder = Path.Combine(folderName, "Sprites");

                for (int artStage = 1; artStage <= 3; artStage++)
                {
                    bool SilentErrors = artStage == 1 && chara.data.helioOnly;
                    // Portrait for the art stage
                    string portraitName = $"portrait_{chara.charaID}{artStage}";
                    ModInstance.log($"Loading character portrait: {portraitName} for art stage {artStage}");
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
                    ModInstance.log($"Loading story sprites {storySpritePrefix} story sprites for art stage {artStage} with target size {targetSpriteSize}");
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
                bool isAnimatedSpine = modelDirectoryExists && Directory.GetFiles(modelFolder, "*.png").Length > 0 && !string.IsNullOrEmpty(skeletonDataPath) && !string.IsNullOrEmpty(atlasDataPath);
                bool isAnimatedSprite = modelDirectoryExists && Directory.GetFiles(modelFolder, "*.png").Length > 0;

                if (isAnimatedSpine)
                {
                    Texture2D[] textures = ImageUtils.LoadTexturesFromFolder(modelFolder, SilentErrors);
                    TextAsset atlasText = new TextAsset(File.ReadAllText(atlasDataPath));
                    TextAsset skeletonJson = new TextAsset(File.ReadAllText(skeletonDataPath));

                    if (textures != null && textures.Length > 0 && atlasText != null && skeletonJson != null)
                    {
                        ModAssetManager.StoreSkeletonData(AssetContentType.CharacterModel, modelName, textures, atlasText, skeletonJson, true);
                        ModInstance.log($"Loaded animated character model: {modelName}");
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
                else if (isAnimatedSprite)
                {
                    SpriteLoadConfig config = new SpriteLoadConfig
                    {
                        SilentErrors = SilentErrors
                    };
                    Sprite[] sprites = ImageUtils.LoadSpritesFromFolder(modelFolder, config);

                    if (sprites != null && sprites.Length > 0)
                    {
                        Texture2D texture = sprites[0].texture;
                        texture.name = "skeleton";
                        ModAssetManager.StoreSkeletonData(AssetContentType.CharacterModel, modelName, new[] { texture }, staticSkeletonAtlas, staticSkeletonData, false);
                        ModAssetManager.StoreSpriteAnimation(AssetContentType.CharacterSpriteModel, modelName, sprites);
                        ModInstance.log($"Loaded animated sprite character model: {modelName}");
                    }
                    else
                    {
                        if (!SilentErrors)
                        {
                            ModInstance.log($"Failed to load animated sprite character model: {modelName}. No textures found.");
                            throw new Exception($"No textures found for animated sprite character model: {modelName}");
                        }
                    }
                }
                else
                {
                    Texture2D texture = ImageUtils.LoadTexture(Path.Combine(spritesFolder, modelName + ".png"), SilentErrors);
                    texture.name = "skeleton";

                    if (texture != null)
                    {
                        ModAssetManager.StoreSkeletonData(AssetContentType.CharacterModel, modelName, new[] { texture }, staticSkeletonAtlas, staticSkeletonData, false);
                        ModInstance.log($"Loaded static character model: {modelName}");
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
                ModInstance.log($"Loaded main menu sprite: {spriteName}");
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
                ModInstance.log($"Loaded story sprite: {spriteName}");
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
            ModInstance.log("Loading custom backgrounds...");

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
                LoadBackgroundThumbnail(spriteName, background);
            }

            ModInstance.log("Custom backgrounds loaded successfully.");
        }

        private static void LoadBackground(string spriteName, CustomBackground background)
        {
            try
            {
                ModInstance.log($"Loading background: {spriteName} from {CFileManager.TrimFolderName(background.file)}");

                string spritePath = Path.Combine(background.file, spriteName + ".png");
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = background.name ?? spriteName,
                    PixelsPerUnit = 1
                };
                Sprite sprite = ImageUtils.LoadSprite(spritePath, config);
                if (sprite != null)
                {
                    ModAssetManager.StoreSprite(AssetContentType.Background, spriteName, sprite);
                    ModInstance.log($"Loaded background sprite: {spriteName}");
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

        private static void LoadBackgroundThumbnail(string spriteName, CustomBackground background)
        {
            try
            {
                ModInstance.log($"Loading background thumbnail: {spriteName}_thumbnail from {CFileManager.TrimFolderName(background.file)}");

                string spritePath = Path.Combine(background.file, spriteName + "_thumbnail.png");
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = background.name ?? spriteName,
                    PixelsPerUnit = 1,
                    SilentErrors = true
                };
                Sprite sprite = ImageUtils.LoadSprite(spritePath, config);
                if (sprite != null)
                {
                    ModAssetManager.StoreSprite(AssetContentType.BackgroundThumbnail, spriteName, sprite);
                    ModInstance.log($"Loaded background sprite: {spriteName}");
                }
                else
                {
                    // No errors here, as we can use background itself as a fallback
                    ModInstance.log($"Thumbnail not found for background sprite: {spriteName}, using main sprite instead.");
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
            ModInstance.log("Loading custom cards...");

            // Load all custom cards
            foreach (var kvp in CustomCardData.idToFile)
            {
                string cardID = kvp.Key;
                string cardFile = kvp.Value;

                LoadCard(cardID, cardFile);
            }

            ModInstance.log("Custom cards loaded successfully.");
        }

        private static void LoadCard(string cardID, string originFile)
        {
            try
            {
                string spriteName = "card_" + cardID;
                string path = originFile.Replace(".json", ".png");

                ModInstance.log($"Loading card: {cardID} from {CFileManager.TrimFolderName(path)}");

                Sprite sprite = ImageUtils.LoadSprite(path, new SpriteLoadConfig
                {
                    SpriteName = spriteName
                });

                if (sprite != null)
                {
                    ModAssetManager.StoreSprite(AssetContentType.Card, spriteName, sprite);
                    ModInstance.log($"Loaded card sprite: {spriteName}");
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

        #region Achievements

        public static void LoadAchievements()
        {
            ModInstance.log("Loading custom achievements...");

            // Load all custom achievements
            foreach (CustomCheevo cheevo in CustomCheevo.customCheevos)
            {
                LoadAchievementIcon(cheevo);
            }

            ModInstance.log("Custom achievements loaded successfully.");
        }

        public static void LoadAchievementIcon(CustomCheevo cheevo)
        {
            if (cheevo == null || string.IsNullOrEmpty(cheevo.file))
            {
                ModInstance.log($"Skipping achievement {cheevo.customID} due to missing file path.");
                ModLoadingStatus.LogError($"Missing file path for achievement {cheevo.customID}");
                return;
            }

            try
            {
                string path = cheevo.file.Replace(".json", ".png");
                if (!File.Exists(path))
                {
                    ModInstance.log($"Achievement file not found: {path}");
                    ModLoadingStatus.LogError($"Achievement file not found: {CFileManager.TrimFolderName(path)}");
                    return;
                }
                
                SpriteLoadConfig config = new SpriteLoadConfig
                {
                    SpriteName = cheevo.customID
                };
                Sprite icon = ImageUtils.LoadSprite(path, config);
                if (icon != null)
                {
                    ModAssetManager.StoreSprite(AssetContentType.Achievement, cheevo.customID, icon);
                    ModInstance.log($"Loaded achievement icon: {cheevo.customID}");
                }
                else
                {
                    throw new Exception($"Could not load the sprite for achievement {cheevo.customID}: {path}");
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading achievement {cheevo.customID}: {e.Message}");
                ModLoadingStatus.LogError($"Error loading achievement {cheevo.customID}: {e.Message}");
            }
        }

        #endregion
    }
}
