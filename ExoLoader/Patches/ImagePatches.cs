using System;
using HarmonyLib;
using UnityEngine;

namespace ExoLoader
{
    [HarmonyPatch]
    public class ImagePatches
    {
        // For story sprites at the current age
        [HarmonyPatch(typeof(Chara))]
        [HarmonyPatch(nameof(Chara.GetStorySprite))]
        [HarmonyPrefix]
        public static bool GetCustomStorySprite(Chara __instance, ref Sprite __result, string expression, int overrideArtStage)
        {
            try
            {
                if (__instance is CustomChara ch)
                {
                    ModInstance.log("Chara is getting a custom chara sprite, getting image " + expression + "...");
                    int artStage = overrideArtStage > 0 ? overrideArtStage : Princess.artStage;
                    string spriteName = ch.data.ages
                        ? __instance.charaID + "_" + expression
                        : __instance.charaID + artStage + "_" + expression;
                    ModInstance.log($"Looking for sprite: {spriteName} with art stage {artStage}");
                    Sprite sprite = ModAssetManager.GetStorySprite(spriteName);

                    if (sprite != null)
                    {
                        ModInstance.log($"Loaded chara sprite: {spriteName}, {sprite.name}");
                        __result = sprite;
                        return false;
                    }
                    else
                    {
                        // Maybe adult sprite, in that case no artStage
                        spriteName = __instance.charaID + "_" + expression;
                        sprite = ModAssetManager.GetStorySprite(spriteName);

                        if (sprite != null)
                        {
                            ModInstance.log($"Loaded chara sprite without art stage: {spriteName}");
                            __result = sprite;
                            return false;
                        }
                        else
                        {
                            ModInstance.log($"No custom sprite found for {__instance.nickname} with expression {expression} and art stage {artStage}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading custom sprite for {__instance.nickname}: {e}");
            }

            return true;
        }

        [HarmonyPatch(typeof(CharaImage))]
        [HarmonyPatch(nameof(CharaImage.GetSprite))]
        [HarmonyPrefix]
        public static bool GetCustomCharaImageSprite(ref Sprite __result, string spriteName)
        {
            if (spriteName == null)
            {
                return true;
            }

            try
            {
                Sprite sprite = GetSpriteByName(spriteName);

                if (sprite != null)
                {
                    ModInstance.log($"Loaded CharaImage sprite: {spriteName}");
                    __result = sprite;
                    return false;
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading CharaImage sprite with id {spriteName}: {e}");
            }
            
            return true;
        }


        // For calling custom sprites of specific ages
        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadCharaSprite))]
        [HarmonyPrefix]
        public static bool LoadCustomCharaSprite(ref Sprite __result, string spriteName)
        {
            if (spriteName == null)
            {
                return true;
            }

            try
            {
                Sprite sprite = GetSpriteByName(spriteName);

                if (sprite != null)
                {
                    __result = sprite;
                    ModInstance.log($"Loaded AssetManager sprite: {spriteName}");
                    return false;
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading AssetManager sprite with id {spriteName}: {e}");
            }

            return true;
        }

        private static Sprite GetSpriteByName(string spriteName)
        {
            Chara chara = Chara.FromCharaImageID(spriteName);

            if (chara != null)
            {
                return ModAssetManager.GetStorySprite(MakeRealSpriteName(spriteName)) ??
                       ModAssetManager.GetStorySprite(spriteName);
            }

            return ModAssetManager.GetStorySprite(spriteName);
        }

        private static string MakeRealSpriteName(string input)
        {
            string[] split = input.Split('_');
            string first = split[0];
            string second = null;
            if (split.Length > 1)
            {
                second = split[1];
            }

            second ??= "normal";

            if (ModAssetManager.StorySpriteExists(first + "_" + second))
            {
                return first + "_" + second;
            }

            if (!first.EndsWith("1") && !first.EndsWith("2") && !first.EndsWith("3"))
            {
                first += Princess.artStage.ToString();
            }

            return first + "_" + second;
        }

        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.LoadCharaPortrait))]
        [HarmonyPrefix]
        public static bool LoadCustomPortrait(ref Sprite __result, string spriteName)
        {
            Chara ch = Chara.FromCharaImageID(spriteName);
            if (ch == null || ch is not CustomChara)
            {
                return true;
            }
            else
            {
                string portraitName = "portrait_" + MakeActualPortraitName(spriteName, (CustomChara)ch);
                Sprite sprite = ModAssetManager.GetSprite(AssetContentType.CharacterPortrait, portraitName);
                if (sprite != null)
                {
                    __result = sprite;
                    ModInstance.log($"Loaded custom portrait: {portraitName}");
                    return false;
                }
                else
                {
                    ModInstance.log($"No custom portrait found for {portraitName}");
                }
            }

            return true;
        }

        private static string MakeActualPortraitName(string input, CustomChara ch)
        {
            string result = input;
            if (!input.EndsWith("1") && !input.EndsWith("2") && !input.EndsWith("3") && ch.data.ages)
            {
                result = input + Princess.artStage.ToString();
            }
            else if (!ch.data.ages && (input.EndsWith("1") || input.EndsWith("2") || input.EndsWith("3")))
            {
                result = input.Substring(0, input.Length - 1);
            }
            return result;
        }

        [HarmonyPatch(typeof(AssetManager))]
        [HarmonyPatch(nameof(AssetManager.GetCardSprite))]
        [HarmonyPrefix]
        public static bool LoadCustomCardSprite(ref Sprite __result, string cardID)
        {
            string spriteName = "card_" + cardID;
            Sprite sprite = ModAssetManager.GetSprite(AssetContentType.Card, spriteName);
            if (sprite != null)
            {
                __result = sprite;
                ModInstance.log($"Loaded custom card sprite: {spriteName}");
                return false;
            }

            return true;
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

                Sprite sprite = ModAssetManager.GetSprite(AssetContentType.BackgroundThumbnail, backgroundName);

                if (sprite != null)
                {
                    __result = sprite;
                    ModInstance.log($"Loaded custom gallery thumbnail: {backgroundName}");
                    return;
                }

                // Fallback to just background
                sprite = ModAssetManager.GetSprite(AssetContentType.Background, backgroundName);
                if (sprite != null)
                {
                    __result = sprite;
                    ModInstance.log($"Loaded custom gallery thumbnail as fallback: {backgroundName}");
                    return;
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading custom gallery thumbnail with id {backgroundName}: {e}");
            }
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

                Sprite sprite = ModAssetManager.GetSprite(AssetContentType.Background, spriteName);
                if (sprite != null)
                {
                    __result = sprite;
                    ModInstance.log($"Loaded custom background: {spriteName}");
                    return;
                }
            }
            catch (Exception e)
            {
                ModInstance.log($"Error loading custom background with id {spriteName}: {e}");
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
