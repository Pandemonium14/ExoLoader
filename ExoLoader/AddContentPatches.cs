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
            if (filename == "Exocolonist - charas")
            {
                LoadCustomContent("Characters");
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

        [HarmonyPatch(typeof(ParserData), nameof(ParserData.LoadAllData))]
        [HarmonyPostfix]
        public static void FinalizeLoading()
        {
            FinalizeCharacters();
            LoadCustomContent("ScriptExtensions");
            // Add locale keys for all custom backgrounds
            CustomBackground.addLocales();
        }

        public static void FinalizeCharacters() //Loads likes, dislikes
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
                        job.skillChanges.Add(new SkillChange(CChara, 1));
                    }
                }

            }

            // Chara.allCharas is the array used in CharasMenu to display the list of charas
            // With this sorting, we maintain the order of original charas, but add custom charas with .canLove before original charas with .canLove = false
            // This is done to maintain the order of charas in the menu, and to make it easier to find custom befriendable charas
            ReorderCharas();
        }

        public static void ReorderCharas()
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
            
 
    }
}
