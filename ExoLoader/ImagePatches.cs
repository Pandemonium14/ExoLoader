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

namespace ExoLoader
{
    [HarmonyPatch]
    public class ImagePatches
    {

        [HarmonyPatch(typeof(CharaImage))]
        [HarmonyPatch(nameof(CharaImage.GetSprite))]
        [HarmonyPrefix]
        public static bool GetCustomSprite(ref Sprite __result, string spriteName)
        {
            if (spriteName ==  null)
            {
                ModInstance.log("Trying to load a story sprite with null ID string (from CharaImage)");
            }
            Chara ch = Chara.FromCharaImageID(spriteName);
            if (!(ch is CustomChara))
            {
                return true;
            }
            else
            {
                ModInstance.log("CharaImage is loading a custom chara sprite, getting image " + spriteName + "...");
                try
                {
                    __result = CFileManager.GetCustomImage(((CustomChara)ch).data.folderName, MakeRealSpriteName(spriteName, (CustomChara)ch), ((CustomChara)ch).data.spriteSize);
                    return false;
                }
                catch (Exception e)
                {
                    ModInstance.log("Couldn't get image");
                    ModInstance.log(e.ToString());
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadCharaSprite))]
        [HarmonyPrefix]
        public static bool SecondGetCustomSprite(ref Sprite __result, string spriteName)
        {
            if (spriteName == null)
            {
                ModInstance.log("Trying to load a story sprite with null ID string (from AssetManager)");
            }
            Chara ch = Chara.FromCharaImageID(spriteName);
            if (!(ch is CustomChara))
            {
                return true;
            }
            else
            {
                ModInstance.log("AssetManager is loading a custom chara sprite, getting image " + spriteName + "...");
                try
                {
                    __result = CFileManager.GetCustomImage(((CustomChara)ch).data.folderName, MakeRealSpriteName(spriteName, (CustomChara)ch), ((CustomChara)ch).data.spriteSize);
                    return false;
                }
                catch (Exception e)
                {
                    ModInstance.log("Couldn't get image");
                    ModInstance.log(e.ToString());
                    return true;
                }
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
                Texture2D bgTexture = CFileManager.GetTexture(Path.Combine(folder,spriteName + ".png"));
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
        public static void LoggingPatch(CharaImageLocation location, string spriteName) {
            if (spriteName != null)
            {
                ModInstance.log("===== Called SetCharaImage with location " + location.ToString() + " and spriteName '" + spriteName + "'");
            } else
            {
                ModInstance.log("===== Called SetCharaImage with a null spriteName");
            }
        }

    }
   
}
