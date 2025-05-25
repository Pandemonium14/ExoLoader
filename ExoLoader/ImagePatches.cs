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
        [HarmonyPatch(nameof(AssetManager.LoadBackgroundOrEndingSprite))]
        [HarmonyPrefix]
        public static bool LoadCustomBackground(ref Sprite __result, string spriteName)
        {
            //ModInstance.log("Loading custom background with id " + spriteName);
            string folder = CustomContentParser.customBackgrounds.GetSafe(spriteName);
            if (folder != null)
            {
                Texture2D bgTexture = CFileManager.GetTexture(Path.Combine(folder, spriteName + ".png"));
                __result = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0), 1);
                return false;
            }
            else
            {
                return true;
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
