using ExoLoader.Debugging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Northway.Utils;
using Spine;
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
        private static readonly string[] expectedCardEntries = new string[]
        {
            "ID",
            "Name",
            "Type",
            "Level",
            "Suit",
            "Value"
        };

        private static readonly string[] expectedJobEntries = new string[]
        {
            "ID",
            "Name",
            "Location"
        };

        private static readonly string[] expectedEndingEntries = new string[]
        {
            "ID",
            "Name",
            "Preamble"
        };

        private static readonly string[] expectedCollectibleEntries = new string[]
        {
            "ID",
            "Name",
            "Plural",
        };

        public static void ParseContentFolder(string contentFolderPath, string contentType)
        {
            string[] folders = Directory.GetDirectories(contentFolderPath);
            foreach (string folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                if (folderName.Equals(contentType))
                {
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
                                        try
                                        {

                                            ParseCardData(file);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex is InvalidCastException)
                                            {
                                                DataDebugHelper.PrintDataError("Invalid cast when loading card " + Path.GetFileNameWithoutExtension(file), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                            }
                                            else
                                            {
                                                DataDebugHelper.PrintDataError("Unexpected error when loading " + Path.GetFileNameWithoutExtension(file), ex.Message);
                                            }
                                            throw ex;
                                        }
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
                                      string bgName = Path.GetFileName(file).Replace(".png", "").ToLower();
                                      if (!Singleton<AssetManager>.instance.backgroundAndEndingNames.Contains(bgName))
                                      {
                                          ModInstance.log("Found bg " +  bgName);
                                          cBgs.Add(bgName);
                                          Singleton<AssetManager>.instance.backgroundAndEndingNames.Append(bgName);
                                          CustomBackground.Add(bgName, Path.GetDirectoryName(file));
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
                                            try
                                            {
                                                ParseJobData(file);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex is InvalidCastException)
                                                {
                                                    DataDebugHelper.PrintDataError("Invalid cast when loading job " + Path.GetFileNameWithoutExtension(file), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                                }
                                                else
                                                {
                                                    DataDebugHelper.PrintDataError("Unexpected error when loading " + Path.GetFileNameWithoutExtension(file), ex.Message);
                                                }
                                                throw ex;
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case "Endings":
                            {
                                ModInstance.log("Parsing ending folders");
                                foreach (string jobFolder in CFileManager.GetAllCustomContentFolders("Endings"))
                                {
                                    foreach (string file in Directory.GetFiles(jobFolder))
                                    {
                                        if (file.EndsWith(".json"))
                                        {
                                            ModInstance.log("Found ending file " + Path.GetFileName(file) + ", parsing...");
                                            try
                                            {
                                                ParseEndingData(file);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex is InvalidCastException)
                                                {
                                                    DataDebugHelper.PrintDataError("Invalid cast when loading ending " + Path.GetFileNameWithoutExtension(file), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                                }
                                                else
                                                {
                                                    DataDebugHelper.PrintDataError("Unexpected error when loading ending " + Path.GetFileNameWithoutExtension(file), ex.Message);
                                                }
                                                throw ex;
                                            }
                                        }
                                    }
                                }
                                break;
                            }

                        case "ScriptExtensions":
                            {
                                ModInstance.log("Parsing script extensions folder");
                                foreach (string file in Directory.GetFiles(folder))
                                {
                                    if (file.EndsWith(".json"))
                                    {
                                        ModInstance.log("Parsing script extension file : " + Path.GetFileName(file));
                                        try
                                        {
                                            ScriptExtensionsParser.ParseFile(file);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex is InvalidCastException)
                                            {
                                                DataDebugHelper.PrintDataError("Invalid cast when loading script extension " + Path.GetFileNameWithoutExtension(file), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                            }
                                            else
                                            {
                                                DataDebugHelper.PrintDataError("Unexpected error when loading script extension " + Path.GetFileNameWithoutExtension(file), ex.Message);
                                            }
                                            throw ex;
                                        }
                                    }
                                }
                                break;
                            }

                        case "Collectibles":
                            {
                                ModInstance.log("Parsing collectibles folders");
                                foreach (string jobFolder in CFileManager.GetAllCustomContentFolders("Collectibles"))
                                {
                                    foreach (string file in Directory.GetFiles(jobFolder))
                                    {
                                        if (file.EndsWith(".json"))
                                        {
                                            ModInstance.log("Found collectible file " + Path.GetFileName(file) + ", parsing...");
                                            try
                                            {
                                                ParseCollectibleData(file);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex is InvalidCastException)
                                                {
                                                    DataDebugHelper.PrintDataError("Invalid cast when loading collectible " + Path.GetFileNameWithoutExtension(file), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                                }
                                                else
                                                {
                                                    DataDebugHelper.PrintDataError("Unexpected error when loading collectible " + Path.GetFileNameWithoutExtension(file), ex.Message);
                                                }
                                                throw ex;
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }

        private static void ParseCollectibleData(string file)
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

            List<string> missingKeys = new List<string>() { "The following keys are mandatory for a collectible file but were missing:" };
            foreach (string key in expectedCollectibleEntries)
            {
                if (!data.ContainsKey(key))
                {
                    missingKeys.Add(key);
                }
            }

            if (missingKeys.Count > 1)
            {
                DataDebugHelper.PrintDataError("Missing mandatory keys for collectible " + Path.GetFileNameWithoutExtension(file), missingKeys.ToArray());
                return;
            }

            CustomCollectible collectible = new()
            {
                file = file,
                id = (string)data["ID"],
                name = (string)data["Name"],
                namePlural = (string)data["Plural"],
                cardId = (string)data["ID"],

                // like and dislike are optional fields of strings separated by commans, we need to split them into arrays
                like = data.ContainsKey("Like") ? [.. ((string)data["Like"]).Split(',').Select(l => l.Trim())] : [],
                dislike = data.ContainsKey("Dislike") ? [.. ((string)data["Dislike"]).Split(',').Select(d => d.Trim())] : []
            };

            // Card specific fields
            if (data.TryGetValue("HowGet", out object howGetValue))
            {
                collectible.howGet = ((string)howGetValue).ToLower() switch
                {
                    "unique" => HowGet.unique,
                    "training" => HowGet.training,
                    "trainingbuy" => HowGet.trainingBuy,
                    "shopdefault" => HowGet.shopDefault,
                    "shopclothes" => HowGet.shopClothes,
                    "shopweapons" => HowGet.shopWeapons,
                    "shopgadgets" => HowGet.shopGadgets,
                    _ => HowGet.none,
                };
            }
            else
            {
                collectible.howGet = HowGet.none; // default value
            }

            if (data.TryGetValue("Kudos", out object kudoCost))
            {
                collectible.kudoCost = ((string)kudoCost).ParseInt();
            }

            if (data.TryGetValue("ArtistName", out object artistName))
            {
                collectible.artist = (string)artistName;
            }
            if (data.TryGetValue("ArtistSocialAt", out object artistAt))
            {
                collectible.artistAt = (string)artistAt;
            }

            if (data.TryGetValue("ArtistLink", out object artistLink))
            {
                collectible.artistLink = (string)artistLink;
            }

            List<CardAbilityType> abilities = [];
            List<int> values = [];
            List<CardSuit> suits = [];

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
                    }
                    else if (abID != null && abID != "")
                    {
                        DataDebugHelper.PrintDataError("Invalid ability in " + Path.GetFileNameWithoutExtension(file));
                        ModInstance.log("WARNING: Incorrect Ability ID : " + abID);
                    }
                }
            }

            collectible.abilityIds = abilities;
            collectible.abilityValues = values;
            collectible.abilitySuits = suits;

            collectible.MakeCollectible();
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
                case "gear":
                    {
                        cardData.type = CardType.gear;
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
                case "none":
                    {
                        cardData.suit = CardSuit.none;
                        break;
                    }
                default:
                    {
                        DataDebugHelper.PrintDataError(Path.GetFileNameWithoutExtension(file) + "has invalid suit", "Valid suits are limited to none, wild, physical, mental and social");
                        break;
                    }
            }

            try
            {
                string howGetValue = data.ContainsKey("HowGet") ? ((string)data["HowGet"]).ToLower() : "none";

                switch (howGetValue)
                {
                    case "unique":
                        {
                            cardData.howGet = HowGet.unique;
                            break;
                        }
                    case "training":
                        {
                            cardData.howGet = HowGet.training;
                            break;
                        }
                    case "trainingbuy":
                        {
                            cardData.howGet = HowGet.trainingBuy;
                            break;
                        }
                    case "shopdefault":
                        {
                            cardData.howGet = HowGet.shopDefault;
                            break;
                        }
                    case "shopclothes":
                        {
                            cardData.howGet = HowGet.shopClothes;
                            break;
                        }
                    case "shopweapons":
                        {
                            cardData.howGet = HowGet.shopWeapons;
                            break;
                        }
                    case "shopgadgets":
                        {
                            cardData.howGet = HowGet.shopGadgets;
                            break;
                        }
                    case "none":
                    default:
                        {
                            cardData.howGet = HowGet.none;
                            break;
                        }
                }

                if (data.TryGetValue("UpgradeFrom", out object upgradeFromCardID))
                {
                    cardData.upgradeFromCardID = (string)upgradeFromCardID;
                }
                else
                {
                    cardData.upgradeFromCardID = null;
                }

                if (data.TryGetValue("Kudos", out object kudoCost))
                {
                    cardData.kudoCost = ((string)kudoCost).ParseInt();
                }
                else
                {
                    cardData.kudoCost = 0;
                }
            }
            catch (Exception ex)
            {
                ModInstance.log("Invalid new field value in " + Path.GetFileNameWithoutExtension(file) + ": " + ex.Message);
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
                    }
                    else if (abID != null && abID != "")
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
                DataDebugHelper.PrintDataError("Couldn't read json file for " + Path.GetFileName(file));
                ModInstance.instance.Log("Couldn't read text for " + Path.GetFileName(file));
                return;
            }
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            if (json == null || json.Count == 0)
            {
                DataDebugHelper.PrintDataError("Couldn't parse json file for " + Path.GetFileNameWithoutExtension(file), "Json file could be read as text but the text coudln't be parsed. Check for any missing \",{}, or comma");
                ModInstance.instance.Log("Couldn't parse json for " + Path.GetFileName(file));
                return;
            }
            List<string> missingKeys = new List<string>() { "The following keys are mandatory for a job file but were missing:" };
            foreach (string key in expectedJobEntries)
            {
                if (!json.ContainsKey(key))
                {
                    missingKeys.Add(key);
                }
            }
            if (missingKeys.Count > 1)
            {
                DataDebugHelper.PrintDataError("Missing mandatory keys for job " + Path.GetFileNameWithoutExtension(file), missingKeys.ToArray());
                return;
            }

            CustomJobData jobData = new CustomJobData();
            jobData.ID = (string)json["ID"];
            jobData.name = (string)json["Name"];
            jobData.location = Location.FromID((string)json["Location"]);
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

        private static void ParseEndingData(string file)
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
            List<string> missingKeys = new List<string>() { "The following keys are mandatory for an ending file but were missing:" };
            foreach (string key in expectedEndingEntries)
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
            string ID = (string)data["ID"];
            string name = (string)data["Name"];
            string preamble = (string)data["Preamble"];
            Location location = data.ContainsKey("Location") ? Location.FromID((string)data["Location"]) : Location.none;
            string chara = data.ContainsKey("Character") ? (string)data["Character"] : "";

            string[] requiredMemories = data.ContainsKey("RequiredMemories") ? ((JArray)(data.GetValueSafe("RequiredMemories"))).ToObject<string[]>() : new string[0];

            string[] requiredJobsStrings = data.ContainsKey("RequiredJobs") ? ((JArray)(data.GetValueSafe("RequiredJobs"))).ToObject<string[]>() : new string[0];
            Job[] requiredJobs = new Job[requiredJobsStrings.Length];
            for (int i = 0;  i < requiredJobsStrings.Length; i++)
            {
                requiredJobs[i] = Job.FromID(requiredJobsStrings[i]);
            }

            string[] extraJobsStrings = data.ContainsKey("OtherJobs") ? ((JArray)(data.GetValueSafe("OtherJobs"))).ToObject<string[]>() : new string[0];
            Job[] extraJobs = new Job[extraJobsStrings.Length];
            for (int i = 0; i < extraJobsStrings.Length; i++)
            {
                extraJobs[i] = Job.FromID(extraJobsStrings[i]);
            }

            string[] skillsStrings = data.ContainsKey("Skills") ? ((JArray)(data.GetValueSafe("Skills"))).ToObject<string[]>() : new string[0];
            Skill[] skills = new Skill[skillsStrings.Length];
            for (int i = 0; i < skillsStrings.Length; i++)
            {
                skills[i] = Skill.FromID(skillsStrings[i]);
            }

            if (Ending.FromID(ID) != null)
            {
                ModInstance.log("Ending with ID " + ID + " already exists, skipping creation");
                DataDebugHelper.PrintDataError("Ending with ID " + ID + " already exists", "If you want to change the ending, please change the ID in the json file to something else. Editing existing endings is not supported at the moment.");
                return;
            }

            ModInstance.log("Creating ending with ID " + ID);
            Ending ending = new Ending(ID, name, preamble, requiredMemories, requiredJobs, extraJobs, skills, chara, location);

            CustomBackground.updateBackgroundNames("ending_" + ID.ToLower(), name);

            ModInstance.log("Parsed and created ending");
        }
    }
}
