using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class CustomChara : Chara
    {
        public static Dictionary<string, CustomChara> customCharasById = new Dictionary<string, CustomChara>();
        public static List<string> newCharaSprites = new List<string>();


        public CharaData data;

        public CustomChara(string idString, string nickname, GenderID genderID, bool canLove, int ageOffset, int birthMonthOfYear, string dialogColor, string defaultBackground, CharaFillbarData[] fillbarDatas, string name, string basics, string more, string enhancement, CharaData data)
            : base(idString, nickname, genderID, canLove, ageOffset, birthMonthOfYear, dialogColor, defaultBackground, fillbarDatas, name, basics, more, enhancement)
        {
            this.data = data;
        }

        public int artStage
        {
            get
            {
                if (!data.ages) return 0;

                if (data.customAging != null && data.customAging.Count > 0)
                {
                    return CalculateCustomArtStage();
                }
                else
                {
                    return Princess.artStage;
                }
            }
        }

        public int GetLastArtStage()
        {
            if (data.customAging != null && data.customAging.Count > 0)
            {
                int maxStage = data.customAging.Max(a => a.stage);
                return maxStage > 0 ? maxStage : 3;
            }
            else
            {
                return 3;
            }
        }

        private int CalculateCustomArtStage()
        {
            CustomAging lastValidAging = null;

            foreach (var aging in data.customAging)
            {
                if (IsValidStage(aging))
                {
                    if (lastValidAging == null || aging.stage > lastValidAging.stage)
                    {
                        lastValidAging = aging;
                    }
                }
            }

            if (lastValidAging != null)
            {
                return lastValidAging.stage;
            }

            return Princess.artStage;
        }

        private static bool IsValidStage(CustomAging data, int month = -1)
        {
            if (data == null)
            {
                return false;
            }

            int effectiveMonth = month;

            if (month == -1 && Savegame.instance != null)
            {
                effectiveMonth = Savegame.instance.week;
            }

            if (effectiveMonth < data.startDate)
            {
                return false;
            }

            if (data.requiredMemories != null)
            {
                foreach (var memory in data.requiredMemories)
                {
                    if (memory.StartsWith("!"))
                    {
                        if (Princess.HasMemory(memory.Substring(1)))
                        {
                            return false;
                        }
                    }
                    else if (!Princess.HasMemory(memory))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public float[] GetMapSpot(string scene, string seasonId)
        {
            if (scene.Equals("strato"))
            {
                if (data.stratoMapSpots != null && data.stratoMapSpots.ContainsKey(seasonId) && data.stratoMapSpots[seasonId] != null)
                {
                    return data.stratoMapSpots[seasonId];
                }

                if (data.stratoMapSpot != null)
                {
                    return data.stratoMapSpot;
                }
            }
            else if (scene.Equals("helio"))
            {
                if (data.helioMapSpots != null && data.helioMapSpots.ContainsKey(seasonId) && data.helioMapSpots[seasonId] != null)
                {
                    return data.helioMapSpots[seasonId];
                }

                if (data.helioMapSpot != null)
                {
                    return data.helioMapSpot;
                }
            }
            else if (scene.Equals("destroyed") || scene.Equals("stratodestroyed"))
            {
                return data.destroyedMapSpot;
            }
            else if (scene.Equals("nearbyhelio"))
            {
                if (data.nearbyHelioMapSpots != null && data.nearbyHelioMapSpots.ContainsKey(seasonId) && data.nearbyHelioMapSpots[seasonId] != null)
                {
                    return data.nearbyHelioMapSpots[seasonId];
                }
            }
            else if (scene.Equals("nearbystrato"))
            {
                if (data.nearbyStratoMapSpots != null && data.nearbyStratoMapSpots.ContainsKey(seasonId) && data.nearbyStratoMapSpots[seasonId] != null)
                {
                    return data.nearbyStratoMapSpots[seasonId];
                }
            }
            else if (scene.Equals("plains"))
            {
                if (data.plainsMapSpots != null && data.plainsMapSpots.ContainsKey(seasonId) && data.plainsMapSpots[seasonId] != null)
                {
                    return data.plainsMapSpots[seasonId];
                }
            }
            else if (scene.Equals("valley"))
            {
                if (data.valleyMapSpots != null && data.valleyMapSpots.ContainsKey(seasonId) && data.valleyMapSpots[seasonId] != null)
                {
                    return data.valleyMapSpots[seasonId];
                }
            }
            else if (scene.Equals("ridge"))
            {
                if (data.ridgeMapSpots != null && data.ridgeMapSpots.ContainsKey(seasonId) && data.ridgeMapSpots[seasonId] != null)
                {
                    return data.ridgeMapSpots[seasonId];
                }
            }
            return null;
        }

        public void DictionaryTests()
        {
            try
            {
                ModInstance.log("Testing " + charaID + " character's dictionaries");
                Chara test = Chara.FromID(charaID);
                if (test == null)
                {
                    ModInstance.log("Chara.FromID returned null for " + charaID);
                    return;
                }
                else
                {
                    ModInstance.log("Chara.FromID worked for " + charaID);
                }
                if (data.onMap)
                {
                    List<Story> storiesTest = Story.storiesByCharaLow[test];
                    if (storiesTest.Count == 0)
                    {
                        ModInstance.log("No low priority stories for " + test.charaID);
                    }

                }
                ModInstance.log("Dictionaries tests passed");
            }
            catch (Exception e)
            {
                ModInstance.log("Character with ID " + charaID + " failed the dictionary tests" + e);
            }
        }
    }
}
