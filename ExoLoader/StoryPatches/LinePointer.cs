using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class LinePointer
    {
        public int currentPos = 0;

        public int GetCurrent()
        {
            return currentPos;
        }

        public int Next()
        {
            currentPos++;
            return currentPos;
        }

        public bool SkipUntilAfterMatch(string[] lines, string match)
        {
            for (int i = currentPos;  i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith(match))
                {
                    currentPos = i+1;
                    return true;
                }
            }
            ModInstance.log("SkipUntilMatch never stopped");
            return false;
        }
    }
}
