using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Northway.Utils;
using UnityEngine;
using UnityEngine.Windows;

namespace ExoLoader
{
    [HarmonyPatch]
    public class ImagePatches
    {
        // For story sprites at the current age
        [HarmonyPatch(typeof(Chara))]
        [HarmonyPatch(nameof(Chara.GetStorySprite))]
        [HarmonyPostfix]
        public static void GetCustomSprite(Chara __instance, ref Sprite __result, string expression, int overrideArtStage)
        {
            if (__result)
            {
                return;
            }

            ModInstance.log($"Loading custom sprite for {__instance.nickname} with expression {expression} and override art stage {overrideArtStage}");

            try
            {
                if (__instance is CustomChara ch)
                {
                    ModInstance.log("Chara is getting a custom chara sprite, getting image " + expression + "...");
                    try
                    {
                        int artStage = overrideArtStage > 0 ? overrideArtStage : Princess.artStage;
                        string spriteName = __instance.charaID + artStage + "_" + expression;
                        int targetSpriteSize = Math.Max(ch.data.spriteSize, ch.data.spriteSizes[artStage - 1]);
                        ModInstance.log("Getting story sprite with size " + targetSpriteSize);
                        __result = CFileManager.GetCustomImage(ch.data.folderName, spriteName, targetSpriteSize);
                    }
                    catch (Exception e)
                    {
                        ModInstance.log($"Error loading custom sprite for {__instance.nickname} with expression {expression}: {e}");
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading custom sprite for {__instance.nickname}: {e}");
            }
        }

        // For calling custom sprites of specific ages
        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadCharaSprite))]
        [HarmonyPostfix]
        public static void LoadCustomCharaSprite(ref Sprite __result, string spriteName)
        {
            if (__result != null)
            {
                return;
            }

            ModInstance.log("Loading custom chara sprite with name " + spriteName);
            Chara ch = Chara.FromCharaImageID(spriteName);
            if (ch == null || !(ch is CustomChara))
            {
                return;
            }
            else
            {
                CustomChara customChara = (CustomChara)ch;
                int targetSpriteSize = Math.Max(customChara.data.spriteSize, customChara.data.spriteSizes[getArtStageFromSpriteName(spriteName) - 1]);
                __result = CFileManager.GetCustomImage(customChara.data.folderName, MakeRealSpriteName(spriteName, customChara), targetSpriteSize);
                return;
            }
        }

        private static int getArtStageFromSpriteName(string spriteName)
        {
            string[] split = spriteName.Split('_');
            string first = split[0];
            if ((!first.EndsWith("1") && !first.EndsWith("2") && !first.EndsWith("3")))
            {
                ModInstance.log("For age " + Princess.artStage);
                return Princess.artStage;
            }
            else
            {
                ModInstance.log("For age " + int.Parse(first[first.Length - 1].ToString()));
                return int.Parse(first[first.Length - 1].ToString());
            }
        }


        private static string MakeRealSpriteName(string input, CustomChara ch)
        {
            string[] split = input.Split('_');
            string first = split[0];
            string second = null;
            if (split.Length > 1)
            {
                second = split[1];
            }
            if ((!first.EndsWith("1") && !first.EndsWith("2") && !first.EndsWith("3")) && ch.data.ages)
            {
                first += Princess.artStage.ToString();
            }
            if (second == null)
            {
                second = "normal";
            }

            return first + "_" + second;
        }

        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadCharaPortrait))]
        [HarmonyPrefix]
        public static bool LoadCustomPortrait(ref Sprite __result, string spriteName)
        {
            Chara ch = Chara.FromCharaImageID(spriteName);
            if (ch == null || !(ch is CustomChara))
            {
                return true;
            }
            else
            {
                __result = CFileManager.GetCustomPortrait(((CustomChara)ch).data.folderName, MakeActualPortraitName(spriteName, (CustomChara)ch));
                return false;
            }
        }

        private static string MakeActualPortraitName(string input, CustomChara ch)
        {
            if (!input.EndsWith("1") && !input.EndsWith("2") && !input.EndsWith("3") && ch.data.ages)
            {
                input += Princess.artStage.ToString();
            }
            else if (!ch.data.ages && (input.EndsWith("1") || input.EndsWith("2") || !input.EndsWith("3")))
            {
                input = input.RemoveEnding(input[-1].ToString());
            }
            return input;
        }


        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.GetCardSprite))]
        [HarmonyPrefix]
        public static bool LoadCustomCardSprite(ref Sprite __result, string cardID)
        {
            //ModInstance.log("Loading a card image, id = " + cardID);
            string file = CustomCardData.idToFile.GetSafe(cardID);
            if (file != null)
            {
                __result = CFileManager.GetCustomCardSprite(cardID, file);
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadGalleryThumbnail))]
        [HarmonyPostfix]
        public static void LoadCustomGalleryThumbnail(ref Sprite __result, string backgroundName)
        {
            if (__result)
            {
                return;
            }

            try
            {
                ModInstance.log("Loading custom gallery thumbnail with id " + backgroundName);

                // First try and get the image from CustomBackgrounds.backgroundThumbnails dictionary, it may be already loaded
                if (CustomBackground.backgroundThumbnails.TryGetValue(backgroundName, out Sprite existingSprite))
                {
                    __result = existingSprite;
                    return;
                }

                if (CustomBackground.allBackgrounds.TryGetValue(backgroundName, out CustomBackground background))
                {
                    ModInstance.log($"Found custom gallery thumbnail with id {backgroundName}, file {background.file}");
                    string folder = background.file;
                    Texture2D thumbnailTexture = CFileManager.GetTexture(Path.Combine(folder, backgroundName + "_thumbnail.png"), true);

                    // If the thumbnail texture is not found, try to load the full background texture
                    if (thumbnailTexture == null)
                    {
                        ModInstance.log($"Thumbnail texture not found for {backgroundName}, trying to load full background texture");
                        thumbnailTexture = CFileManager.GetTexture(Path.Combine(folder, backgroundName + ".png"));
                    }

                    if (thumbnailTexture != null)
                    {
                        ModInstance.log($"Successfully loaded thumbnail texture for {backgroundName}");
                        Sprite bgSprite = Sprite.Create(thumbnailTexture, new Rect(0, 0, thumbnailTexture.width, thumbnailTexture.height), new Vector2(0.5f, 0), 1);
                        bgSprite.name = backgroundName; // Use the background's name if available, otherwise use the id
                        CustomBackground.backgroundThumbnails[backgroundName] = bgSprite;

                        __result = bgSprite;

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading custom gallery thumbnail with id {backgroundName}: {e}");
            }
        }

        // Patch for AssetManager.ReleaseGalleryThumbnails - clean up loaded backgrounds
        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.ReleaseGalleryThumbnails))]
        [HarmonyPostfix]
        public static void ReleaseGalleryThumbnails()
        {
            ModInstance.log("Releasing gallery thumbnails");
            CustomBackground.backgroundThumbnails.Clear();
        }

        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadBackgroundOrEndingSprite))]
        [HarmonyPostfix]
        public static void LoadCustomBackground(ref Sprite __result, string spriteName)
        {
            if (__result)
            {
                return;
            }

            try
            {
                ModInstance.log("Loading custom background with id " + spriteName);

                // First try and get the image from CustomBackgrounds.loadedBackgrounds dictionary, it may be already loaded
                if (CustomBackground.loadedBackgrounds.TryGetValue(spriteName, out Sprite existingSprite))
                {
                    __result = existingSprite;
                    return;
                }

                if (CustomBackground.allBackgrounds.TryGetValue(spriteName, out CustomBackground background))
                {
                    ModInstance.log($"Found custom background with id {spriteName}, file {background.file}");
                    string folder = background.file;
                    Texture2D bgTexture = CFileManager.GetTexture(Path.Combine(folder, spriteName + ".png"));
                    if (bgTexture != null)
                    {
                        ModInstance.log($"Successfully loaded background texture for {spriteName}");
                        Sprite bgSprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0), 1);
                        bgSprite.name = spriteName;
                        CustomBackground.loadedBackgrounds[spriteName] = bgSprite;
                        Princess.seenBackgrounds.AddSafe(spriteName, -1, false); // Register the background as seen for the gallery
                        __result = bgSprite;

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading custom background with id {spriteName}: {e}");
            }
        }

        [HarmonyPatch(typeof(Result))]
        [HarmonyPatch(nameof(Result.SetCharaImage))]
        [HarmonyPrefix]
        public static void LoggingPatch(CharaImageLocation location, string spriteName)
        {
            if (spriteName != null)
            {
                ModInstance.log("===== Called SetCharaImage with location " + location.ToString() + " and spriteName '" + spriteName + "'");
            }
            else
            {
                ModInstance.log("===== Called SetCharaImage with a null spriteName");
            }
        }

        [HarmonyPatch(typeof(MainMenuCharas), "OnEnable")]
        [HarmonyPostfix]
        static void MainMenuCharasOnEnablePostfix(MainMenuCharas __instance)
        {
            ModInstance.log("MainMenuCharas OnEnable called, adding custom characters...");
            PatchMainManuCharas(__instance);
        }

        public static void PatchMainManuCharas(MainMenuCharas instance)
        {
            foreach (CustomChara chara in CustomChara.customCharasById.Values)
            {
                if (!chara.canLove || !chara.hasCard3 || chara.data.mainMenu == null)
                {
                    continue;
                }

                try
                {
                    // Find the template object to copy
                    Transform templateChara = instance.charaContainer.GetChildByName("chara_" + chara.data.mainMenu.template);
                    if (templateChara == null)
                    {
                        ModInstance.log($"Template chara not found: chara_{chara.data.mainMenu.template}");
                        continue;
                    }

                    // Check if custom chara already exists
                    string customCharaName = "chara_" + chara.charaID;
                    Transform existingCustomChara = instance.charaContainer.GetChildByName(customCharaName);
                    if (existingCustomChara != null)
                    {
                        // Already exists, just make sure it's active
                        existingCustomChara.SetActiveMaybe();
                        continue;
                    }

                    // existing sprite object
                    Sprite templateSprite = templateChara.GetComponentInChildren<SpriteRenderer>()?.sprite;
                    if (templateSprite == null)
                    {
                        ModInstance.log($"Main menu template sprite not found for {chara.charaID}");
                        continue;
                    }

                    GameObject customCharaObj = UnityEngine.Object.Instantiate(templateChara.gameObject);
                    customCharaObj.name = customCharaName;
                    ModInstance.log($"Instantiated custom character object: {customCharaObj.name}");
                    Sprite sprite = ModAssetManager.GetSprite(AssetContentType.CharacterMainMenu, customCharaName);

                    if (sprite == null)
                    {
                        ModInstance.log($"Failed to load sprite for {chara.charaID}");
                        UnityEngine.Object.Destroy(customCharaObj);
                        continue;
                    }

                    SpriteRenderer childSpriteRenderer = customCharaObj.GetComponentInChildren<SpriteRenderer>();
                    if (childSpriteRenderer != null)
                    {
                        ModInstance.log($"Applying custom sprite to child SpriteRenderer in {customCharaObj.name}");
                        childSpriteRenderer.sprite = sprite;
                    }
                    else
                    {
                        ModInstance.log($"Failed to apply sprite for {chara.charaID}");
                        UnityEngine.Object.Destroy(customCharaObj);
                        continue;
                    }

                    customCharaObj.transform.SetParent(instance.charaContainer);
                    customCharaObj.transform.localPosition = new Vector3(
                        chara.data.mainMenu.position[0],
                        chara.data.mainMenu.position[1],
                        0.00f
                    );
                    customCharaObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                    SpriteRenderer templateRenderer = templateChara.GetComponentInChildren<SpriteRenderer>();
                    if (templateRenderer != null)
                    {
                        SpriteRenderer customRenderer = customCharaObj.GetComponentInChildren<SpriteRenderer>();
                        if (customRenderer != null)
                        {
                            customRenderer.sortingLayerID = templateRenderer.sortingLayerID;
                            customRenderer.sortingOrder = templateRenderer.sortingOrder;
                            customRenderer.sortingLayerName = templateRenderer.sortingLayerName;
                        }
                    }

                    customCharaObj.SetActiveMaybe();

                    ModInstance.log($"Successfully added custom character to the main menu: {chara.charaID}");
                }
                catch (System.Exception ex)
                {
                    ModInstance.log($"Error adding custom character to the main menu {chara.charaID}: {ex}");
                }
            }
        }
    }
   
}
