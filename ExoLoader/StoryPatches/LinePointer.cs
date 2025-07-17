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

        public bool SkipUntilAfterMatch(string[] lines, string match, int targetIndex)
        {
            int foundCount = 0;
            for (int i = currentPos; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith(match))
                {
                    if (foundCount == targetIndex)
                    {
                        currentPos = i + 1;
                        return true;
                    }
                    foundCount++;
                }
            }
            ModInstance.log("SkipUntilMatchWithIndex never found index " + targetIndex + " for match '" + match + "'");
            return false;
        }

        public LinePointer Clone()
        {
            LinePointer clone = new()
            {
                currentPos = this.currentPos
            };
            return clone;
        }
    }
}
