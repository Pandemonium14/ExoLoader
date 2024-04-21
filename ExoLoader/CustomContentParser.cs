using ExoLoader.Debugging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Northway.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class CustomContentParser
    {
        public static Dictionary<string, string> customBackgrounds = new Dictionary<string, string>();

        private static readonly string[] expectedCardEntries = new string[]
        {
            "ID",
            "Name",
            "Type",
            "Level",
            "Suit",
            "Value"
        };

        public static void ParseContentFolder(string contentFolderPath, string contentType)
        {
            string[] folders = Directory.GetDirectories(contentFolderPath);
            foreach (string folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                if (folderName.Equals(contentType)) { 
                    switch (folderName)
                    {
                        case "Cards":
                        {
                            ModInstance.log("Parsing cards folder");
                            foreach (string file in Directory.GetFiles(folder))
                            {
                                if (file.EndsWith(".json"))
                                {
                                    ModInstance.log("Parsing file : " + Path.GetFileName(file));
                                    ParseCardData(file);
                                }
                            }

                            break;
                        }
                        case "Backgrounds":
                        {
                            ModInstance.log("Adding backgrounds and CGs");
                            List<string> cBgs = new List<string>(Singleton<AssetManager>.instance.backgroundAndEndingNames);
                            foreach (string file in Directory.GetFiles(folder))
                            {
                                if (file.EndsWith(".png"))
                                {
                                    string bgName = Path.GetFileName(file).Replace(".png", "");
                                    if (!Singleton<AssetManager>.instance.backgroundAndEndingNames.Contains(bgName))
                                    {
                                        ModInstance.log("Found bg " +  bgName);
                                        cBgs.Add(bgName);
                                        Singleton<AssetManager>.instance.backgroundAndEndingNames.Append(bgName);
                                        customBackgrounds.Add(bgName, folder);
                                        ModInstance.log("Added " + bgName + "to list");
                                    }
                                }
                            }
                            Singleton<AssetManager>.instance.backgroundAndEndingNames = cBgs.ToArray();
                            break;
                        }
                        case "Jobs":
                            {
                                ModInstance.log("Parsing job folders");
                                foreach (string jobFolder in CFileManager.GetAllCustomContentFolders("Jobs"))
                                {
                                    foreach (string file in Directory.GetFiles(jobFolder))
                                    {
                                        if (file.EndsWith(".json"))
                                        {
                                            ModInstance.log("Found job file " + Path.GetFileName(file) + ", parsing...");
                                            ParseJobData(file);
                                        }
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }

        private static void ParseCardData(string file)
        {
            string fullJson = File.ReadAllText(file);
            if (fullJson == null || fullJson.Length == 0)
            {
                DataDebugHelper.PrintDataError("Couldn't read json file for " + Path.GetFileName(file));
                ModInstance.instance.Log("Couldn't read text for " + Path.GetFileNameWithoutExtension(file));
                return;
            }
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            if (data == null || data.Count == 0)
            {
                DataDebugHelper.PrintDataError("Couldn't parse json file for " + Path.GetFileNameWithoutExtension(file), "Json file could be read as text but the text coudln't be parsed. Check for any missing \",{}, or comma");
                ModInstance.instance.Log("Couldn't parse json for " + Path.GetFileName(file));
                return;
            }
            List<string> missingKeys = new List<string>() {"The following keys are mandatory for a card file but were missing:"};
            foreach (string key in expectedCardEntries)
            {
                if (!data.ContainsKey(key))
                {
                    missingKeys.Add(key);
                }
            }
            if (missingKeys.Count > 1)
            {
                DataDebugHelper.PrintDataError("Missing mandatory keys for card " + Path.GetFileNameWithoutExtension(file), missingKeys.ToArray());
                return;
            }

            CustomCardData cardData = new CustomCardData();
            cardData.file = file;
            cardData.id = (string)data["ID"];
            cardData.name = (string)data["Name"];
            cardData.level = int.Parse((string)data["Level"]);

            switch ((string)data["Type"])
            {
                case "memory":
                    {
                        cardData.type = CardType.memory; 
                        break;
                    }
                default:
                    {
                        DataDebugHelper.PrintDataError(Path.GetFileNameWithoutExtension(file) + " has invalid or unsupported type", "Only memory type cards are currently supported");
                        return;
                    }
            }

            cardData.value = ((string)data["Value"]).ParseInt();
            

            switch (((string)data["Suit"]).ToLower())
            {
                case "physical":
                    {
                        cardData.suit = CardSuit.physical;
                        break;
                    }
                case "mental":
                    {
                        cardData.suit = CardSuit.mental;
                        break;
                    }
                case "social":
                    {
                        cardData.suit = CardSuit.social;
                        break;
                    }
                case "wild":
                    {
                        cardData.suit = CardSuit.wildcard;
                        break;
                    }
                default:
                    {
                        DataDebugHelper.PrintDataError(Path.GetFileNameWithoutExtension(file) + "has invalid suit", "Valid suits are limited to wild, physical, mental and social");
                        break;
                    }
            }

            if (data.TryGetValue("ArtistName", out object artistName))
            {
                cardData.artist = (string)artistName;
            }
            if (data.TryGetValue("ArtistSocialAt", out object artistAt))
            {
                cardData.artistAt = (string)artistAt;
            }

            if (data.TryGetValue("ArtistLink", out object artistLink))
            {
                cardData.artistLink = (string)artistLink;
            }

            List<CardAbilityType> abilities = new List<CardAbilityType>();
            List<int> values = new List<int>();
            List<CardSuit> suits = new List<CardSuit>();
            for (int i = 1; i <= 3; i++)
            {
                if (data.TryGetValue("Ability" + i.ToString(), out object abilityEntry))
                {
                    Dictionary<string, object> abilityMap;

                    abilityMap = ((JObject)abilityEntry).ToObject<Dictionary<string, object>>();

                    ModInstance.log("Reading an Ability entry");
                    string abID = (string)abilityMap.GetValueSafe("ID");
                    CardAbilityType abType = CardAbilityType.FromID(abID);
                    if (abType != null)
                    {
                        abilities.Add(abType);

                        values.Add(((string)abilityMap.GetValueSafe("Value")).ParseInt());
                        suits.Add(((string)abilityMap.GetValueSafe("Suit")).ParseEnum<CardSuit>());
                    } else if (abID != null && abID != "")
                    {
                        DataDebugHelper.PrintDataError("Invalid ability in " + Path.GetFileNameWithoutExtension(file));
                        ModInstance.log("WARNING: Incorrect Ability ID : " + abID);
                    }
                }
            }
            cardData.abilityIds = abilities;
            cardData.abilityValues = values;
            cardData.abilitySuits = suits;

            cardData.MakeCard();
        }

        private static void ParseJobData(string file)
        {
            string fullJson = File.ReadAllText(file);
            if (fullJson == null || fullJson.Length == 0)
            {
                ModInstance.instance.Log("Couldn't read text for " + Path.GetFileName(file));
            }
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            ModInstance.log("Json parsed");
            CustomJobData jobData = new CustomJobData();
            jobData.ID = (string)json["ID"];
            jobData.name = (string)json["Name"];
            jobData.location = Location.FromID((string)json["Location"]);
            ModInstance.log("Read first wave of data");
            jobData.skillChanges = new List<SkillChange>();
            if (json.ContainsKey("PrimarySkill"))
            {
                jobData.primarySkill = Skill.FromID((string)json["PrimarySkill"]);
                int primarySkillValue = int.Parse((string)json["PrimaryValue"]);
                jobData.skillChanges.Add(new SkillChange(jobData.primarySkill, primarySkillValue));
            }
            if (json.ContainsKey("SecondSkill"))
            {
                Skill secondSkill = Skill.FromID((string)json["SecondSkill"]);
                int secondValue = int.Parse((string)json["SecondValue"]);
                jobData.skillChanges.Add(new SkillChange(secondSkill, secondValue));
            }
            if (json.ContainsKey("Kudos"))
            {
                int kudos = int.Parse((string)json["Kudos"]);
                if (kudos != 0)
                {
                    jobData.skillChanges.Add(new SkillChange(Skill.FromID("kudos"), kudos));
                }
            }
            if (json.ContainsKey("Stress"))
            {
                int stress = int.Parse((string)json["Stress"]);
                if (stress != 0)
                {
                    jobData.skillChanges.Add(new SkillChange(Skill.FromID("stress"), stress));
                }
            }
            ModInstance.log("Read skill data");
            if (json.ContainsKey("IsRelax"))
            {
                jobData.isRelax = bool.Parse((string)json["IsRelax"]);
            } else
            {
                jobData.isRelax = false;
            }

            if (json.ContainsKey("Characters"))
            {
                string[] charaIDs = ((JArray)(json.GetValueSafe("Characters"))).ToObject<string[]>();
                foreach (string charaID in charaIDs)
                {
                    Chara chara = Chara.FromID(charaID);
                    if (chara != null)
                    {
                        jobData.skillChanges.Add(new SkillChange(chara, 1));
                    }
                }
            }
            ModInstance.log("Read characters data");
            if (json.ContainsKey("UltimateBonusSkill"))
            {
                Skill ultiSkill = Skill.FromID((string)json["UltimateBonusSkill"]);
                int ultiValue = int.Parse((string)json["UltimateBonusValue"]);
                jobData.ultimateBonus = new SkillChange(ultiSkill, ultiValue);
            }
            if (json.ContainsKey("BattleHeaderText"))
            {
                jobData.battleHeaderText = (string)json["BattleHeaderText"];
            }
            jobData.MakeJob();
        }
    }
}
