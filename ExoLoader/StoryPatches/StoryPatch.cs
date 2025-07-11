using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class StoryPatch
    {

        public string eventID;
        public StoryPatchType patchType;

        public string key;
        public string key2;//only used in replace patches
        public int keyIndex = -1;//used for disambiguation
        public int keyIndex2 = -1;
        public int indexCounter = 0;
        public int indexCounter2 = 0;

        public List<string> contentLines;

        public int patchEnd = -1;

        public bool wasWritten = false;

        public static StoryPatch ReadPatch(string[] lines, int index)
        {
            StoryPatch patch = new StoryPatch();
            string[] patchInfo = SplitWithEscapes(lines[index], '|', '@');

            patch.patchType = patchInfo[1].ParseEnum<StoryPatchType>();
            patch.eventID = patchInfo[2];
            patch.key = patchInfo[3];
            if (patch.patchType == StoryPatchType.insert)
            {
                patch.keyIndex = int.Parse(patchInfo[4]);
            }
            if (patch.patchType == StoryPatchType.replace)
            {
                patch.key2 = patchInfo[4];
                ModInstance.log("This patch has ke2 equal to " + patch.key2);
                patch.keyIndex = int.Parse(patchInfo[5]);
                patch.keyIndex2 = int.Parse(patchInfo[6]);
            }
            index += 2;
            patch.contentLines = [];
            while (lines[index].Trim(' ') != "}")
            {
                patch.contentLines.Add(lines[index]);
                index++;
            }
            patch.patchEnd = index;
            return patch;
        }

        private static string[] SplitWithEscapes(string input, params char[] separators)
        {
            List<string> parts = [];
            StringBuilder currentPart = new();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '\\' && i + 1 < input.Length && Array.IndexOf(separators, input[i + 1]) >= 0)
                {
                    currentPart.Append(input[i + 1]);
                    i++;
                }
                else if (Array.IndexOf(separators, c) >= 0)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            parts.Add(currentPart.ToString());

            return [.. parts];
        }

        public bool CheckForKey(string line)
        {
            return line.StartsWith(key);
        }

        public bool CheckForKey2(string line)
        {
            return line.StartsWith(key2); 
        }
    }
}
