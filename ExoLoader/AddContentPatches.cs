using ExoLoader.Debugging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExoLoader
{
    [HarmonyPatch]
    public class AddContentPatches
    {
        private static bool logStoryReq;

        [HarmonyPatch(typeof(ParserData), "LoadData")]
        [HarmonyPostfix]
        public static void AddCharaPatch(string filename)
        {
            if (filename == "Exocolonist - charas")
            {
                ModInstance.instance.Log("Checking CustomCharacter folders");
                string[] charaFolders = CFileManager.GetAllCustomCharaFolders();
                if (charaFolders != null && charaFolders.Length == 0) {
                    ModInstance.instance.Log("Found no folder");
                    return;
                }
                foreach (string folder in charaFolders)
                {
                    ModInstance.instance.Log("Parsing folder " + CFileManager.TrimFolderName(folder));
                    CharaData data = null;
                    try
                    {
                        data = CFileManager.ParseCustomData(folder);
                    } catch (Exception e)
                    {
                        if (e is InvalidCastException)
                        {
                            DataDebugHelper.PrintDataError("Invalid cast when loading character " + Path.GetFileName(folder), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                        } else
                        {
                            DataDebugHelper.PrintDataError("Unexpected error when loading " + Path.GetFileName(folder), e.Message);
                        }
                        throw e;
                    }
                    ModInstance.log("Adding character: " + data.id);
                    if (data != null)
                    {
                        data.MakeChara();
                        CustomChara.customCharasById.Add(data.id, (CustomChara)Chara.FromID(data.id));
                    }
                    ModInstance.log(data.id + " added succesfully, adding images to the character sprite list");
                    string[] originalList = Northway.Utils.Singleton<AssetManager>.instance.charaSpriteNames;
                    List<string> newlist = originalList.ToList<string>();
                    string spritesPath = Path.Combine(folder, "Sprites");
                    int counter = 0;
                    foreach (string filePath in Directory.EnumerateFiles(spritesPath))
                    {
                        string file = Path.GetFileName(filePath);
                        //ModInstance.log("Checking " + file);
                        if (file.EndsWith(".png") && file.StartsWith(data.id))
                        {
                            newlist.Add(file.Replace(".png", ""));
                            List<string> l = Northway.Utils.Singleton<AssetManager>.instance.spritesByCharaID.GetSafe(data.id);
                            if (l == null)
                            {
                                l = new List<string>();
                                Northway.Utils.Singleton<AssetManager>.instance.spritesByCharaID.Add(data.id, l);
                            }
                            l.Add(file.Replace(".png", "").Replace("_normal", ""));
                            CustomChara.newCharaSprites.Add(file.Replace(".png", ""));
                            counter++;
                        }
                    }

                    Northway.Utils.Singleton<AssetManager>.instance.charaSpriteNames = newlist.ToArray();
                    ModInstance.log("Added " + counter + " image names to the list");

                }
            } else if (filename == "ExocolonistCards - cards")
            {
                ModInstance.log("calling LoadCustomContent for Cards");
                LoadCustomContent("Cards");
            } else if (filename == "Exocolonist - variables")
            {
                ModInstance.log("Loading preliminary content");

                ModInstance.log("Loading story patches");
                StoryPatchManager.PopulatePatchList();

                ModInstance.log("Patching story files");
                StoryPatchManager.PatchAllStories();

                ModInstance.log("Loading custom backgrounds");
                LoadCustomContent("Backgrounds");

            } else if (filename == "Exocolonist - jobs")
            {
                LoadCustomContent("Jobs");
            }
        }

        [HarmonyPatch(typeof(ParserData), nameof(ParserData.LoadAllData))]
        [HarmonyPostfix]
        public static void FinalizeLoading()
        {
            FinalizeCharacters();
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
            __result = CustomChara.newCharaSprites.Contains(spriteName);
            return !__result;
        }

        [HarmonyPatch(typeof(Chara), nameof(Chara.onMap), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool OnMapGetterPatch(Chara __instance, ref bool __result)
        {
            if (__instance is CustomChara)
            {
                __result = !((CustomChara)__instance).data.helioOnly || Princess.HasMemory("newship");
                return false;
            } else
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
