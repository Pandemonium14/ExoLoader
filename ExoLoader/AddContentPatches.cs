using ExoLoader.Debugging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExoLoader
{
    [HarmonyPatch]
    public class AddContentPatches
    {
        [HarmonyPatch(typeof(ParserData), "LoadData")]
        [HarmonyPostfix]
        public static void AddCharaPatch(string filename)
        {
            try
            {
                if (filename == "Exocolonist - charas")
                {
                    LoadCustomContent("Characters");
                    LoadCustomContent("StorySprites");
                }
                else if (filename == "ExocolonistCards - cards")
                {
                    ModInstance.log("calling LoadCustomContent for Cards");
                    LoadCustomContent("Cards");
                }
                else if (filename == "Exocolonist - variables")
                {
                    ModInstance.log("Loading preliminary content");

                    ModInstance.log("Loading story patches");
                    StoryPatchManager.PopulatePatchList();

                    ModInstance.log("Patching story files");
                    StoryPatchManager.PatchAllStories();

                    ModInstance.log("Loading custom backgrounds");
                    LoadCustomContent("Backgrounds");

                }
                else if (filename == "Exocolonist - jobs")
                {
                    LoadCustomContent("Jobs");
                }
                else if (filename == "Exocolonist - endings")
                {
                    LoadCustomContent("Endings");
                }
                else if (filename == "ExocolonistCards - collectibles")
                {
                    LoadCustomContent("Collectibles");
                }
                else if (filename == "Exocolonist - cheevos")
                {
                    ModInstance.log("Loading custom achievements");
                    LoadCustomContent("Achievements");
                }
            }
            catch (Exception e)
            {
                ModLoadingStatus.LogError($"Error while loading custom content to {filename}: {e.Message}");
            }
        }

        [HarmonyPatch(typeof(ParserData), nameof(ParserData.LoadAllData))]
        [HarmonyPostfix]
        public static void FinalizeLoading()
        {
            FinalizeCharacters();
            LoadCustomContent("ScriptExtensions");
            // Add locale keys for all custom backgrounds
            CustomBackground.AddLocales();
        }

        public static void FinalizeCharacters() //Loads likes, dislikes
        {
            try
            {
                foreach (CustomChara CChara in CustomChara.customCharasById.Values)
                {
                    foreach (string like in CChara.data.likes)
                    {
                        CardData cd = CardData.FromID(like);
                        CChara.likedCards.AddSafe(cd);
                    }

                    foreach (string dislike in CChara.data.dislikes)
                    {
                        CardData cd = CardData.FromID(dislike);
                        CChara.dislikedCards.AddSafe(cd);
                    }

                    foreach (string jobID in CChara.data.jobs)
                    {
                        Job job = Job.FromID(jobID);
                        if (job != null)
                        {
                            int postCharaIndex = job.skillChanges.FindIndex((skillChange) => (skillChange.skill == Skill.kudos) || (skillChange.skill == Skill.stress));

                            if (postCharaIndex == -1)
                            {
                                job.skillChanges.Add(new SkillChange(CChara, 1));
                            }
                            else
                            {
                                job.skillChanges.Insert(postCharaIndex, new SkillChange(CChara, 1));
                            }
                        }
                    }
                }

                // Chara.allCharas is the array used in CharasMenu to display the list of charas
                // With this sorting, we maintain the order of original charas, but add custom charas with .canLove before original charas with .canLove = false
                // This is done to maintain the order of charas in the menu, and to make it easier to find custom befriendable charas
                ReorderCharas();
            }
            catch (Exception e)
            {
                ModLoadingStatus.LogError($"Error while finalizing characters: {e.Message}");
            }
        }

        private static void ReorderCharas()
        {
            int firstNonFriendIndex = Chara.allCharas.FindIndex(chara => !chara.canLove);
            for (int index = 0; index < Chara.allCharas.Count; index++)
            {
                if (Chara.allCharas[index] is CustomChara customChara && customChara.canLove)
                {
                    if (index < firstNonFriendIndex)
                    {
                        continue;
                    }
                    Chara.allCharas.Insert(firstNonFriendIndex, Chara.allCharas[index]);
                    Chara.allCharas.RemoveAt(index + 1); // Remove the original chara after inserting
                    firstNonFriendIndex++;
                }
            }
        }

        public static void LoadCustomContent(string contentType)
        {
            try
            {
                ModInstance.instance.Log("Checking CustomContent folders");
                string[] contentFolders = CFileManager.GetAllCustomContentFolders();
                if (contentFolders != null && contentFolders.Length == 0)
                {
                    ModInstance.instance.Log("Found no folder");
                    return;
                }
                foreach (string folder in contentFolders)
                {
                    ModInstance.log("Parsing " + contentType + " content folder: " + CFileManager.TrimFolderName(folder));
                    CustomContentParser.ParseContentFolder(folder, contentType);
                }
            }
            catch (Exception e)
            {
                ModLoadingStatus.LogError($"Error while loading custom content of type {contentType}: {e.Message}");
            }
        }

        [HarmonyPatch(typeof(CharaImage), nameof(CharaImage.SpriteExists))]
        [HarmonyPrefix]
        public static bool CheckIfCustomSprite(ref bool __result, string spriteName)
        {

            bool SpriteExists = ModAssetManager.StorySpriteExists(spriteName);
            if (SpriteExists)
            {
                ModInstance.log("Custom sprite found: " + spriteName);
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Chara), nameof(Chara.onMap), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool OnMapGetterPatch(Chara __instance, ref bool __result)
        {
            if (__instance is CustomChara chara)
            {
                // FIXME: to be honest, people who add characters should properly set mem_map_charaID when their character is added
                if (chara.isDead)
                {
                    __result = false;
                    return false;
                }

                __result = !chara.data.helioOnly || Princess.HasMemory("newship");
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(FileManager), nameof(FileManager.storiesPath), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool storiesPathGetterPatch(ref string __result)
        {
            __result = Path.Combine(CFileManager.commonFolderPath, "PatchedStories");
            return false;
        }

        // This patch is in case when a story requirement is set to a character that does not exist, it means the story will be triggered randomly with dark screen
        // To avoid this, we will replace the character requirement with a 'none' location, meaning this story will not be triggered unless called from somewhere else
        // This makes it possible for one mod to add story to a character from another mod, but the story will only be active when that character is present in the game
        [HarmonyPatch(typeof(ParserStoryReq), "ParseReqInner")]
        [HarmonyPrefix]
        public static bool ParserStoryReqParseReqInnerPrefix(StoryReq req, ref string line, Story story, Choice choice = null)
        {
            try
            {
                if (line != null && line.StartsWith("chara = "))
                {
                    // line can be chara = high_charaID, chara = _high_charaID, chara = charaID (or low), we need to get actual charaID
                    // So separate by both = and _ and get the last part
                    string[] parts = line.Split(['=', '_'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        string charaID = parts[parts.Length - 1].Trim(); // Get the last part and trim whitespace
                        Chara chara = Chara.FromID(charaID);
                        if (chara == null)
                        {
                            ModInstance.log($"{story?.storyID ?? "null"}: ParserStoryReqParseReqInnerPrefix: Chara.FromID returned null for {charaID}");
                            line = "location = none"; // replace invalid chara with a 'none' location, meaning this story will not be triggered unless called from somewhere else
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModInstance.log($"Error in ParserStoryReqParseReqInnerPrefix: {ex.Message}\n{ex.StackTrace}");
            }

            return true;
        }
    }
}
