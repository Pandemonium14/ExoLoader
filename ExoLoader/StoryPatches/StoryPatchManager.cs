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
        public static readonly string patchedStoriesFolder = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomContent", "common", "PatchedStories"));
        
        
        
       
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
                
                foreach (string file in Directory.GetFiles(folder))
                {
                    ModInstance.log("Parsing patches in " + Path.GetFileName(file));
                    bool wasPatch = false;

                    string[] lines = File.ReadAllLines(file);
                    ModInstance.log("Successfully read lines");
                    int index = 0;
                    while (index < lines.Length)
                    {
                        if (lines[index].Trim().StartsWith(patchStartMarker)) {
                            ModInstance.log("Found patch starting line : " + lines[index]);
                            StoryPatch patch = null;
                            try
                            {
                                patch = StoryPatch.ReadPatch(lines, index);
                                ModInstance.log("Patch read");
                            } catch (Exception e)
                            {
                                ModInstance.log("Error reading patch with header " + lines[index]);
                                ModInstance.log(e.Message);
                                index ++;
                                continue;
                            }
                            if (eventsToPatches.ContainsKey(patch.eventID))
                            {
                                eventsToPatches[patch.eventID].Add(patch);
                                counter++;
                            } else
                            {
                                List<StoryPatch> list = new List<StoryPatch>() { patch };
                                eventsToPatches.Add(patch.eventID, list);
                                ModInstance.log("event '" + patch.eventID + "' now has patches to apply");
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
            }
            ModInstance.log("Loaded " + counter + " patches in total");
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
                    } else
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
            ModInstance.log("Event" + eventID + " has " + patches.Count +" patches to apply");
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
                    } else
                    {
                        pointer.SkipUntilAfterMatch(baseLines, toApply.key2);
                    }
                    continue;                
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
            ModInstance.log("Patching file " + filePath);
            WritePatchedStoryFile(writer, baseLines);

            writer.Flush();
            writer.Dispose();
        }


        public static bool ShouldWriteNewPatchedFiles()
        {
            if (!File.Exists(Path.Combine(patchedStoriesFolder, "patched_chara_anemone.exo"))) { return true; }


            DateTime mostRecentEdit = DateTime.MinValue;
            DateTime lastPatching = File.GetLastWriteTime(Directory.GetFiles(patchedStoriesFolder)[0]);

            //vanilla story files
            string storyFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exocolonist_Data", "StreamingAssets", "Stories");
            foreach (string storyFile in Directory.GetFiles(storyFolder))
            {
                if (File.GetLastWriteTime(storyFile) > mostRecentEdit)
                {
                    mostRecentEdit = File.GetLastWriteTime(storyFile);
                }
            }
            //custom story files
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
            //story patches
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

        public static void PatchAllStories()
        {
            if (ShouldWriteNewPatchedFiles())
            {
                ModInstance.log("Creating new patched files...");
                string storyFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exocolonist_Data", "StreamingAssets", "Stories");
                foreach (string storyFile in Directory.GetFiles(storyFolder))
                {
                    if (Path.GetExtension(storyFile) == ".exo")
                    {
                        PatchStoryFile(storyFile, "_");
                    }
                }
                foreach (string folder in CFileManager.GetAllCustomContentFolders("Stories"))
                {
                    foreach (string storyFile in Directory.GetFiles(folder))
                    {
                        if (Path.GetExtension(storyFile) == ".exo")
                        {
                            PatchStoryFile(storyFile);
                        }
                    }
                }
            } else
            {
                ModInstance.log("No modifiaction found, skipping making patched files");
            }
            ClearAll();
        }

    }
}
