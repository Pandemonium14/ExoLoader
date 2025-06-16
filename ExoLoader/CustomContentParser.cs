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

        private static readonly string[] expectedCheevoEntries = new string[]
        {
            "ID",
            "Name",
            "Description"
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
                        case "Characters":
                            {
                                ModInstance.log("Parsing characters folder");
                                // This works a bit differently, because "Characters" folder contains subfolders for each character
                                foreach (string charaFolder in Directory.GetDirectories(folder))
                                {
                                    ModInstance.log("Parsing character folder " + CFileManager.TrimFolderName(charaFolder));
                                    try
                                    {
                                        ParseCharacterData(charaFolder);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is InvalidCastException)
                                        {
                                            DataDebugHelper.PrintDataError("Invalid cast when loading character " + Path.GetFileNameWithoutExtension(charaFolder), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                        }
                                        else
                                        {
                                            DataDebugHelper.PrintDataError("Unexpected error when loading " + Path.GetFileNameWithoutExtension(charaFolder), ex.Message);
                                        }
                                    }
                                }
                                break;
                            }
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
                                    if (file.EndsWith(".png") && !file.Contains("_thumbnail"))
                                    {
                                        string bgName = Path.GetFileName(file).Replace(".png", "").ToLower();
                                        if (!Singleton<AssetManager>.instance.backgroundAndEndingNames.Contains(bgName))
                                        {
                                            ModInstance.log("Found bg " + bgName);
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
                                foreach (string file in Directory.GetFiles(folder))
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
                                        }
                                    }
                                }
                                break;
                            }
                        case "Endings":
                            {
                                ModInstance.log("Parsing ending folders");
                                foreach (string file in Directory.GetFiles(folder))
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
                                        }
                                    }
                                }
                                break;
                            }

                        case "Collectibles":
                            {
                                foreach (string file in Directory.GetFiles(folder))
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
                                        }
                                    }
                                }
                                break;
                            }

                        case "Achievements":
                            {
                                ModInstance.log("Parsing achievements folders");
                                foreach (string file in Directory.GetFiles(folder))
                                {
                                    if (file.EndsWith(".json"))
                                    {
                                        ModInstance.log("Found achievement file " + Path.GetFileName(file) + ", parsing...");
                                        try
                                        {
                                            ParseCheevoData(file);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex is InvalidCastException)
                                            {
                                                DataDebugHelper.PrintDataError("Invalid cast when loading achievement " + Path.GetFileNameWithoutExtension(file), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                                            }
                                            else
                                            {
                                                DataDebugHelper.PrintDataError("Unexpected error when loading achievement " + Path.GetFileNameWithoutExtension(file), ex.Message);
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

        private static void ParseCharacterData(string folder)
        {
            ModInstance.instance.Log("Parsing folder " + CFileManager.TrimFolderName(folder));
            CharaData data;

            try
            {
                data = CFileManager.ParseCustomCharacterData(folder);
            }
            catch (Exception e)
            {
                if (e is InvalidCastException)
                {
                    DataDebugHelper.PrintDataError("Invalid cast when loading character " + Path.GetFileNameWithoutExtension(folder), "This happens when there is missing quotation marks in the json, or if you put text where a number should be. Make sure everything is in order!");
                }
                else
                {
                    DataDebugHelper.PrintDataError("Unexpected error when loading " + Path.GetFileNameWithoutExtension(folder), e.Message);
                }
                return;
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
                        l = [];
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
            List<string> missingKeys = new List<string>() { "The following keys are mandatory for a card file but were missing:" };
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

            string ID = (string)data["ID"];

            if (Ending.FromID(ID) != null)
            {
                Ending existingEnding = Ending.FromID(ID);

                if (data.ContainsKey("Modifications"))
                {
                    ModInstance.log($"Modifying existing ending with ID {ID}");
                    Dictionary<string, object> modifications = ((JObject)data["Modifications"]).ToObject<Dictionary<string, object>>();
                    foreach (var modification in modifications)
                    {
                        switch (modification.Key)
                        {
                            case "Name":
                                existingEnding.endingName = (string)modification.Value;
                                break;
                            case "Preamble":
                                existingEnding.preamble = (string)modification.Value;
                                break;
                            case "Location":
                                existingEnding.location = Location.FromID((string)modification.Value);
                                break;
                            case "Character":
                                string charaId = (string)modification.Value;
                                if (charaId != null && charaId != "")
                                {
                                    existingEnding.charaID = charaId;
                                }
                                else
                                {
                                    existingEnding.charaID = "";
                                }
                                break;
                            case "RequiredMemories":
                                if (modification.Value is JObject memoryModifications)
                                {
                                    List<string> memoriesList = existingEnding.memories != null ? [.. existingEnding.memories] : [];

                                    if (memoryModifications.ContainsKey("Add"))
                                    {
                                        string[] addMemories = ((JArray)memoryModifications["Add"]).ToObject<string[]>();

                                        for (int i = 0; i < addMemories.Length; i++)
                                        {
                                            if (!memoriesList.Contains(addMemories[i]))
                                            {
                                                ModInstance.log("Adding memory " + addMemories[i] + " to ending " + ID);
                                                memoriesList.Add(addMemories[i]);
                                            }
                                        }
                                    }

                                    if (memoryModifications.ContainsKey("Remove"))
                                    {
                                        string[] removeMemories = ((JArray)memoryModifications["Remove"]).ToObject<string[]>();
                                        for (int i = 0; i < removeMemories.Length; i++)
                                        {
                                            if (memoriesList.Contains(removeMemories[i]))
                                            {
                                                ModInstance.log("Removing memory " + removeMemories[i] + " from ending " + ID);
                                                memoriesList.Remove(removeMemories[i]);
                                            }
                                        }
                                    }

                                    existingEnding.memories = [.. memoriesList];
                                }
                                break;
                            case "RequiredJobs":
                                if (modification.Value is JObject jobModifications)
                                {
                                    List<Job> jobsList = existingEnding.requiredJobs != null ? [.. existingEnding.requiredJobs] : [];

                                    if (jobModifications.ContainsKey("Add"))
                                    {
                                        string[] addJobs = ((JArray)jobModifications["Add"]).ToObject<string[]>();


                                        for (int i = 0; i < addJobs.Length; i++)
                                        {
                                            Job job = Job.FromID(addJobs[i]);
                                            if (job != null && !jobsList.Contains(job))
                                            {
                                                ModInstance.log("Adding job " + addJobs[i] + " to ending " + ID);
                                                jobsList.Add(job);
                                            }
                                        }
                                    }

                                    if (jobModifications.ContainsKey("Remove"))
                                    {
                                        string[] removeJobs = ((JArray)jobModifications["Remove"]).ToObject<string[]>();
                                        for (int i = 0; i < removeJobs.Length; i++)
                                        {
                                            Job job = Job.FromID(removeJobs[i]);
                                            if (job != null && jobsList.Contains(job))
                                            {
                                                ModInstance.log("Removing job " + removeJobs[i] + " from ending " + ID);
                                                jobsList.Remove(job);
                                            }
                                        }
                                    }

                                    existingEnding.requiredJobs = [.. jobsList];
                                }
                                break;
                            case "OtherJobs":
                                if (modification.Value is JObject jobModifications2)
                                {
                                    List<Job> jobsList = existingEnding.otherJobs != null ? [.. existingEnding.otherJobs] : [];

                                    if (jobModifications2.ContainsKey("Add"))
                                    {
                                        string[] addJobs = ((JArray)jobModifications2["Add"]).ToObject<string[]>();

                                        for (int i = 0; i < addJobs.Length; i++)
                                        {
                                            Job job = Job.FromID(addJobs[i]);
                                            if (job != null && !jobsList.Contains(job))
                                            {
                                                ModInstance.log("Adding extra job " + addJobs[i] + " to ending " + ID);
                                                jobsList.Add(job);
                                            }
                                        }
                                    }

                                    if (jobModifications2.ContainsKey("Remove"))
                                    {
                                        string[] removeJobs = ((JArray)jobModifications2["Remove"]).ToObject<string[]>();
                                        if (existingEnding.otherJobs == null)
                                        {
                                            break;
                                        }

                                        for (int i = 0; i < removeJobs.Length; i++)
                                        {
                                            Job job = Job.FromID(removeJobs[i]);
                                            if (job != null && jobsList.Contains(job))
                                            {
                                                ModInstance.log("Removing extra job " + removeJobs[i] + " from ending " + ID);
                                                jobsList.Remove(job);
                                            }
                                        }
                                    }

                                    existingEnding.otherJobs = [.. jobsList];
                                }
                                break;
                            case "Skills":
                                if (modification.Value is JObject skillModifications)
                                {
                                    List<Skill> skillsList = existingEnding.skills != null ? [.. existingEnding.skills] : [];

                                    if (skillModifications.ContainsKey("Add"))
                                    {
                                        string[] addSkills = ((JArray)skillModifications["Add"]).ToObject<string[]>();

                                        for (int i = 0; i < addSkills.Length; i++)
                                        {
                                            Skill skill = Skill.FromID(addSkills[i]);
                                            if (skill != null && !skillsList.Contains(skill))
                                            {
                                                ModInstance.log("Adding skill " + addSkills[i] + " to ending " + ID);
                                                skillsList.Add(skill);
                                            }
                                        }
                                    }

                                    if (skillModifications.ContainsKey("Remove"))
                                    {
                                        string[] removeSkills = ((JArray)skillModifications["Remove"]).ToObject<string[]>();
                                        for (int i = 0; i < removeSkills.Length; i++)
                                        {
                                            Skill skill = Skill.FromID(removeSkills[i]);
                                            if (skill != null && skillsList.Contains(skill))
                                            {
                                                ModInstance.log("Removing skill " + removeSkills[i] + " from ending " + ID);
                                                skillsList.Remove(skill);
                                            }
                                        }
                                    }

                                    existingEnding.skills = [.. skillsList];
                                }
                                break;
                        }
                    }
                }
                else
                {
                    ModInstance.log($"Ending with ID {ID} already exists, skipping creation");
                    DataDebugHelper.PrintDataError($"Ending with ID {ID} already exists", "Please make sure that the ID is unique for each ending.");
                }
            }
            else
            {
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

                string name = (string)data["Name"];
                string preamble = (string)data["Preamble"];
                Location location = data.ContainsKey("Location") ? Location.FromID((string)data["Location"]) : Location.none;
                string chara = data.ContainsKey("Character") ? (string)data["Character"] : "";

                string[] requiredMemories = data.ContainsKey("RequiredMemories") ? ((JArray)(data.GetValueSafe("RequiredMemories"))).ToObject<string[]>() : new string[0];

                string[] requiredJobsStrings = data.ContainsKey("RequiredJobs") ? ((JArray)(data.GetValueSafe("RequiredJobs"))).ToObject<string[]>() : new string[0];
                Job[] requiredJobs = new Job[requiredJobsStrings.Length];
                for (int i = 0; i < requiredJobsStrings.Length; i++)
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

                ModInstance.log("Creating ending with ID " + ID);
                Ending ending = new Ending(ID, name, preamble, requiredMemories, requiredJobs, extraJobs, skills, chara, location);

                CustomBackground.updateBackgroundNames("ending_" + ID.ToLower(), name);

                ModInstance.log("Parsed and created ending");
            }
        }

        private static void ParseCheevoData(string file)
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
            List<string> missingKeys = new List<string>() { "The following keys are mandatory for a cheevo file but were missing:" };
            foreach (string key in expectedCheevoEntries)
            {
                if (!data.ContainsKey(key))
                {
                    missingKeys.Add(key);
                }
            }
            if (missingKeys.Count > 1)
            {
                DataDebugHelper.PrintDataError("Missing mandatory keys for cheevo " + Path.GetFileNameWithoutExtension(file), missingKeys.ToArray());
                return;
            }

            if (CustomCheevo.customCheevosByID.ContainsKey((string)data["ID"]))
            {
                ModInstance.log("Cheevo with ID " + (string)data["ID"] + " already exists, skipping creation");
                DataDebugHelper.PrintDataError("Cheevo with ID " + (string)data["ID"] + " already exists", "Please make sure that the ID is unique for each cheevo, maybe another mod has the same ID?");
                return;
            }

            List<string> loveAll = null;
            if (data.ContainsKey("LoveAll"))
            {
                if (data["LoveAll"] is JArray loveAllArray && loveAllArray.Count > 0)
                {
                    loveAll = loveAllArray.ToObject<List<string>>();
                }
            }

            List<string> requiredCheevos = null;
            if (data.ContainsKey("RequiredCheevos"))
            {
                if (data["RequiredCheevos"] is JArray requiredCheevosArray && requiredCheevosArray.Count > 0)
                {
                    requiredCheevos = requiredCheevosArray.ToObject<List<string>>();
                }
            }

            new CustomCheevo(
                ((string)data["ID"]).ToLower(),
                (string)data["Name"],
                (string)data["Description"],
                data.ContainsKey("Hidden") && bool.Parse((string)data["Hidden"]),
                file,
                loveAll,
                requiredCheevos
            );
        }
    }
}
