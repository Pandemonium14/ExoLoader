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

                // First try and get the image from CustomBackgrounds.loadedBackgrounds dictionary, it may be already loaded
                if (CustomBackground.loadedBackgrounds.TryGetValue(backgroundName, out Sprite existingSprite))
                {
                    __result = existingSprite;
                    return;
                }

                if (CustomBackground.allBackgrounds.TryGetValue(backgroundName, out CustomBackground background))
                {
                    ModInstance.log($"Found custom gallery thumbnail with id {backgroundName}, file {background.file}");
                    string folder = background.file;
                    Texture2D thumbnailTexture = CFileManager.GetTexture(Path.Combine(folder, backgroundName + ".png"));
                    if (thumbnailTexture != null)
                    {
                        ModInstance.log($"Successfully loaded thumbnail texture for {backgroundName}");
                        Sprite bgSprite = Sprite.Create(thumbnailTexture, new Rect(0, 0, thumbnailTexture.width, thumbnailTexture.height), new Vector2(0.5f, 0), 1);
                        bgSprite.name = backgroundName; // Use the background's name if available, otherwise use the id
                        CustomBackground.loadedBackgrounds[backgroundName] = bgSprite;

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
            CustomBackground.loadedBackgrounds.Clear();
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
    }
   
}
