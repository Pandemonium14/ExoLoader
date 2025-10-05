using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class StoryPatchManager
    {
        public static readonly string patchFolderName = "StoryPatches";
        public static readonly string patchStartMarker = "@";
        public static readonly string patchedStoriesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomContent", "common", "PatchedStories");

        public static Dictionary<string, List<StoryPatch>> eventsToPatches = new Dictionary<string, List<StoryPatch>>();

        public static Dictionary<string, DateTime> patchFilesToDates = new Dictionary<string, DateTime>();

        public static void ClearAll()
        {
            eventsToPatches.Clear();
            patchFilesToDates.Clear();
        }

        public static List<string> GetAllPatchFolders()
        {
            return CFileManager.GetAllCustomContentFolders(patchFolderName);
        }

        public static void PopulatePatchList()
        {
            int counter = 0;
            foreach (string folder in GetAllPatchFolders())
            {
                string modName = CFileManager.GetModName(folder);

                if (!ExoLoaderSave.GetModEnabled(modName))
                {
                    ModInstance.log("Skipping patches for disabled mod " + modName);
                    continue;
                }

                foreach (string file in Directory.GetFiles(folder))
                {
                    try
                    {
                        ModInstance.log("Parsing patches from " + modName);
                        bool wasPatch = false;

                        string[] lines = File.ReadAllLines(file);

                        int index = 0;
                        while (index < lines.Length)
                        {
                            if (lines[index].Trim().StartsWith(patchStartMarker))
                            {
                                ModInstance.log("Found patch starting line : " + lines[index]);
                                StoryPatch patch = null;
                                try
                                {
                                    patch = StoryPatch.ReadPatch(lines, index);
                                    patch.modName = modName;
                                }
                                catch (Exception e)
                                {
                                    ModInstance.log("Error reading patch with header " + lines[index]);
                                    ModInstance.log(e.Message);
                                    ModLoadingStatus.LogError("Error while reading patch with header " + lines[index] + ": " + e.Message);
                                    index++;
                                    continue;
                                }
                                if (eventsToPatches.ContainsKey(patch.eventID))
                                {
                                    eventsToPatches[patch.eventID].Add(patch);
                                    counter++;
                                }
                                else
                                {
                                    List<StoryPatch> list = new List<StoryPatch>() { patch };
                                    eventsToPatches.Add(patch.eventID, list);
                                    counter++;
                                }
                                wasPatch = true;
                                index = patch.patchEnd;
                            }
                            index++;
                        }

                        if (wasPatch)
                        {
                            patchFilesToDates.Add(file, File.GetLastWriteTime(file));
                        }
                    }
                    catch (Exception e)
                    {
                        ModLoadingStatus.LogError("Error while parsing patch file " + Path.GetFileName(file) + ": " + e.Message);
                    }
                }
            }
            ModInstance.log("Loaded " + counter + " patches in total");
            ClearIgnorePatches();
        }

        private static void ClearIgnorePatches()
        {
            try
            {
                // check if there are ignore patches, find patches they apply to and remove those patches
                foreach (var eventPatches in eventsToPatches.Values)
                {
                    var ignorePatches = eventPatches.Where(p => p.patchType == StoryPatchType.ignore).ToList();
                    foreach (var ignorePatch in ignorePatches)
                    {
                        ModInstance.log("Applying ignore patch for mod " + ignorePatch.ignoreModName + " event " + ignorePatch.eventID + " key " + ignorePatch.key + " key2 " + ignorePatch.key2);
                        ModInstance.log("Before ignore, event has " + eventPatches.Count + " patches");
                        eventPatches.RemoveAll(p => p.modName == ignorePatch.ignoreModName && p.eventID == ignorePatch.eventID && p.key == ignorePatch.key && p.key2 == ignorePatch.key2 && p.keyIndex == ignorePatch.keyIndex && p.keyIndex2 == ignorePatch.keyIndex2);
                        ModInstance.log("After ignore, event has " + eventPatches.Count + " patches");
                        eventPatches.Remove(ignorePatch);
                        ModInstance.log("Removed ignore patch, " + eventPatches.Count + " patches left");
                    }
                }
            }
            catch (Exception e)
            {
                ModLoadingStatus.LogError("Error while applying ignore patches: " + e.Message);
            }
        }

        public static bool IsStartOfEvent(string line)
        {
            return line.Trim().StartsWith("===") && !line.Trim('=', ' ').IsNullOrEmptyOrWhitespace();
        }

        public static StoryPatch TestKeys(string line, List<StoryPatch> patches)
        {
            foreach (StoryPatch p in patches)
            {
                if (p.CheckForKey(line)) {
                    if (p.indexCounter == p.keyIndex)
                    {
                        return p;
                    }
                    else
                    {
                        p.indexCounter++;
                    }
                }
            }
            return null;
        }

        //Call with pointer on the line just after the head
        public static void WritePatchedEvent(StreamWriter writer, string eventID, string[] baseLines, LinePointer pointer)
        {
            List<StoryPatch> patches = eventsToPatches.GetSafe(eventID);
            if (patches == null)
            {
                while (pointer.GetCurrent() < baseLines.Length && !IsStartOfEvent(baseLines[pointer.GetCurrent()]))
                {
                    writer.WriteLine(baseLines[pointer.GetCurrent()]);
                    pointer.Next();
                }
                return;
            }
            ModInstance.log("Event " + eventID + " has " + patches.Count + " patches to apply");
            while (pointer.GetCurrent() < baseLines.Length && !IsStartOfEvent(baseLines[pointer.GetCurrent()]))
            {
                StoryPatch toApply = TestKeys(baseLines[pointer.GetCurrent()].Trim(), patches);
                if (toApply == null)
                {
                    writer.WriteLine(baseLines[pointer.GetCurrent()]);
                    pointer.Next();
                    continue;
                }
                ModInstance.log("Applying patch with key " + toApply.key);
                if (toApply.patchType == StoryPatchType.insert)
                {
                    foreach (string line in toApply.contentLines)
                    {
                        writer.WriteLine(line);
                    }
                    toApply.wasWritten = true;
                    patches.Remove(toApply);
                    continue;
                }
                if (toApply.patchType == StoryPatchType.replace)
                {
                    foreach (string line in toApply.contentLines)
                    {
                        writer.WriteLine(line);
                    }
                    toApply.wasWritten = true;
                    patches.Remove(toApply);
                    if (toApply.key2 == "")
                    {
                        pointer.Next();
                    }
                    else
                    {
                        pointer.SkipUntilAfterMatch(baseLines, toApply.key2, toApply.keyIndex2);
                    }
                    continue;
                }

            }

            // Log any patches that weren't applied
            foreach (StoryPatch patch in patches.ToList())
            {
                if (!patch.wasWritten)
                {
                    ModInstance.log("Warning: Patch with key '" + patch.key + "' in event '" + eventID + "' was not applied");
                    ModLoadingStatus.LogError("Patch with key '" + patch.key + "' in event '" + eventID + "' was not applied");
                }
            }
        }

        public static void WritePatchedStoryFile(StreamWriter writer, string[] baseLines)
        {
            LinePointer pointer = new LinePointer();
            while (pointer.GetCurrent() < baseLines.Length)
            {
                if (IsStartOfEvent(baseLines[pointer.GetCurrent()]))
                {
                    string eventID = baseLines[pointer.GetCurrent()].Trim('=',' ');
                    writer.WriteLine(baseLines[pointer.GetCurrent()]);
                    pointer.Next();
                    WritePatchedEvent(writer, eventID, baseLines, pointer);
                    continue;
                }
                writer.WriteLine(baseLines[pointer.GetCurrent()]);
                pointer.Next();
            }
        }

        public static void PatchStoryFile(string filePath)
        {
            PatchStoryFile(filePath, "");
        }

        public static void PatchStoryFile(string filePath, string additionalPrefix)
        {
            string[] baseLines = File.ReadAllLines(filePath);
            StreamWriter writer = new StreamWriter(Path.Combine(patchedStoriesFolder, additionalPrefix + "patched_" + Path.GetFileNameWithoutExtension(filePath) +".exo"), false);
            ModInstance.log("Patching file " + CFileManager.TrimFolderName(filePath));
            WritePatchedStoryFile(writer, baseLines);

            writer.Flush();
            writer.Dispose();
        }


        public static bool ShouldWriteNewPatchedFiles()
        {
            if (!File.Exists(Path.Combine(patchedStoriesFolder, "patched_chara_anemone.exo")))
            {
                return true;
            }

            if (!File.Exists(Path.Combine(patchedStoriesFolder, "modlist.json")))
            {
                ModInstance.log("Mod list file not found, rewriting patched files");
                return true;
            }

            string modListPath = Path.Combine(patchedStoriesFolder, "modlist");
            Dictionary<string, bool> modList = new Dictionary<string, bool>();

            if (File.Exists(modListPath))
            {
                foreach (var line in File.ReadLines(modListPath))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2 && bool.TryParse(parts[1], out bool isEnabled))
                    {
                        modList[parts[0]] = isEnabled;
                    }
                }
            }

            foreach (var kvp in modList)
            {
                if (ExoLoaderSave.GetModEnabled(kvp.Key, true) != kvp.Value)
                {
                    ModInstance.log("Mod enabled state changed for " + kvp.Key + ", rewriting patched files");
                    return true;
                }
            }

            DateTime mostRecentEdit = DateTime.MinValue;
            DateTime lastPatching = File.GetLastWriteTime(Directory.GetFiles(patchedStoriesFolder)[0]);

            // vanilla story files
            string storyFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exocolonist_Data", "StreamingAssets", "Stories");
            foreach (string storyFile in Directory.GetFiles(storyFolder))
            {
                if (File.GetLastWriteTime(storyFile) > mostRecentEdit)
                {
                    mostRecentEdit = File.GetLastWriteTime(storyFile);
                }
            }

            // custom story files
            foreach (string folder in CFileManager.GetAllCustomContentFolders("Stories"))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (File.GetLastWriteTime(file) > mostRecentEdit)
                    {
                        mostRecentEdit = File.GetLastWriteTime(file);
                    }
                }
            }

            // story patches
            foreach (string folder in CFileManager.GetAllCustomContentFolders(patchFolderName))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (File.GetLastWriteTime(file) > mostRecentEdit)
                    {
                        mostRecentEdit = File.GetLastWriteTime(file);
                    }
                }
            }

            return mostRecentEdit > lastPatching;
        }

        private static void CreateModlistFile()
        {
            string modListPath = Path.Combine(patchedStoriesFolder, "modlist");
            Dictionary<string, bool> modList = new Dictionary<string, bool>();

            foreach (ContentMod mod in ContentMod.allMods.Values)
            {
                modList[mod.id] = ExoLoaderSave.GetModEnabled(mod.id, true);
            }

            using (StreamWriter writer = new StreamWriter(modListPath))
            {
                foreach (var kvp in modList)
                {
                    writer.WriteLine($"{kvp.Key}:{kvp.Value}");
                }
            }
        }

        private static void AddModPatch()
        {
            List<ContentMod> enabledMods = new List<ContentMod>();

            foreach (ContentMod mod in ContentMod.allMods.Values)
            {
                if (ExoLoaderSave.GetModEnabled(mod.id, true))
                {
                    enabledMods.Add(mod);
                }
            }

            if (enabledMods.Count == 0)
            {
                return;
            }

            StoryPatch modPatch = new StoryPatch();
            modPatch.eventID = "gameStartIntro";
            modPatch.patchType = StoryPatchType.insert;
            modPatch.key = "Warning:";
            modPatch.keyIndex = 0;
            modPatch.contentLines = [
                "		= exoloaderJump1",
                "		The game has been modified by the following ExoLoader content mods:"
            ];

            StoryPatch modPatch2 = new StoryPatch();
            modPatch2.eventID = "gameStartIntro";
            modPatch2.patchType = StoryPatchType.insert;
            modPatch2.key = "Warning:";
            modPatch2.keyIndex = 1;
            modPatch2.contentLines = [
                "		= exoloaderJump2",
                "		The game has been modified by the following ExoLoader content mods:"
            ];

            List<string> creditsPatches = [];
            List<string> creditPatches2 = [];

            foreach (ContentMod mod in enabledMods)
            {
                modPatch.contentLines.Add("		- " + mod.name + (mod.version != null ? (" v" + mod.version) : ""));
                modPatch2.contentLines.Add("		- " + mod.name + (mod.version != null ? (" v" + mod.version) : ""));

                if (mod.introCredits != null && mod.introCredits.Count > 0)
                {
                    creditsPatches.Add($"		** {mod.name} Info");
                    creditsPatches.Add("");
                    creditsPatches.AddRange(mod.introCredits.Select(line => "			" + line));
                    creditsPatches.Add("");
                    creditsPatches.Add("			*** Back");
                    creditsPatches.Add("				> exoloaderJump1");

                    creditPatches2.Add($"		** {mod.name} Info");
                    creditPatches2.Add("");
                    creditPatches2.AddRange(mod.introCredits.Select(line => "			" + line));
                    creditPatches2.Add("");
                    creditPatches2.Add("			*** Back");
                    creditPatches2.Add("				> exoloaderJump2");
                }
            }

            modPatch.contentLines.Add("");
            modPatch.contentLines.Add("		** Continue");
            modPatch.contentLines.Add("		 	> introwarning1");
            modPatch.contentLines.Add("");
            modPatch.contentLines.AddRange(creditsPatches);
            modPatch.contentLines.Add("");
            modPatch.contentLines.Add("	*= introwarning1");

            modPatch2.contentLines.Add("");
            modPatch2.contentLines.Add("		** Continue");
            modPatch2.contentLines.Add("		 	> introwarning2");
            modPatch2.contentLines.Add("");
            modPatch2.contentLines.AddRange(creditPatches2);
            modPatch2.contentLines.Add("");
            modPatch2.contentLines.Add("	*= introwarning2");

            // Add to list of patches
            if (!eventsToPatches.ContainsKey(modPatch.eventID))
            {
                eventsToPatches[modPatch.eventID] = new List<StoryPatch>();
            }

            eventsToPatches[modPatch.eventID].Add(modPatch);
            eventsToPatches[modPatch2.eventID].Add(modPatch2);
        }

        public static void PatchAllStories()
        {
            if (ShouldWriteNewPatchedFiles())
            {
                ModInstance.log("Creating new patched files...");

                if (Directory.Exists(patchedStoriesFolder))
                {
                    Directory.Delete(patchedStoriesFolder, true);
                }
                Directory.CreateDirectory(patchedStoriesFolder);

                CreateModlistFile();
                AddModPatch();

                string storyFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exocolonist_Data", "StreamingAssets", "Stories");
                foreach (string storyFile in Directory.GetFiles(storyFolder))
                {
                    if (Path.GetExtension(storyFile) == ".exo")
                    {
                        try
                        {
                            PatchStoryFile(storyFile, "_");
                        }
                        catch (Exception e)
                        {
                            ModLoadingStatus.LogError("Error while patching story file " + Path.GetFileName(storyFile) + ": " + e.Message);
                        }
                    }
                }

                foreach (string folder in CFileManager.GetAllCustomContentFolders("Stories"))
                {
                    string modName = CFileManager.GetModName(folder);

                    if (!ExoLoaderSave.GetModEnabled(modName))
                    {
                        ModInstance.log("Skipping custom stories for disabled mod " + modName);
                        continue;
                    }

                    foreach (string storyFile in Directory.GetFiles(folder))
                    {
                        if (Path.GetExtension(storyFile) == ".exo")
                        {
                            try
                            {
                                PatchStoryFile(storyFile);
                            }
                            catch (Exception e)
                            {
                                ModLoadingStatus.LogError("Error while patching custom story file " + Path.GetFileName(storyFile) + ": " + e.Message);
                            }
                        }
                    }
                }
            }
            else
            {
                ModInstance.log("No modifiaction found, skipping making patched files");
            }
            ClearAll();
        }

    }
}
