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
                ModInstance.instance.Log("Couldn't read text for " + Path.GetFileName(file));
            }
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            if (data == null || data.Count == 0)
            {
                ModInstance.instance.Log("Couldn't parse json for " + Path.GetFileName(file));
            }

            CustomCardData cardData = new CustomCardData();
            cardData.file = file;

            if (data.TryGetValue("ID", out object id))
            {
                cardData.id = (string)id;
            }
            else
            {
                ModInstance.instance.Log("no ID entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read ID entry");

            if (data.TryGetValue("Name", out object name))
            {
                cardData.name = (string)name;
            }
            else
            {
                ModInstance.instance.Log("no Name entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read Name entry");

            if (data.TryGetValue("Level", out object level))
            {
                cardData.level = int.Parse((string)level);
            }
            else
            {
                ModInstance.instance.Log("no Name entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read Name entry");

            if (data.TryGetValue("Type", out object typeName))
            {
                switch (typeName)
                {
                    case "memory":
                        {
                            cardData.type = CardType.memory; 
                            break;
                        }
                    default:
                        {
                            ModInstance.log("Card type " + typeName + " is invalid or not supported yet!");
                            break;
                        }
                }
            }
            else
            {
                ModInstance.instance.Log("no Type entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read Type entry");

            if (data.TryGetValue("Value", out object value))
            {
                cardData.value = ((string)value).ParseInt();
            }
            else
            {
                ModInstance.instance.Log("no Value entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read Value entry");

            if (data.TryGetValue("Suit", out object suitName))
            {
                switch (suitName)
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
                }
            }
            else
            {
                ModInstance.instance.Log("no Suit entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read Suit entry");

            if (data.TryGetValue("ArtistName", out object artistName))
            {
                cardData.artist = (string)artistName;
            }
            else
            {
                ModInstance.instance.Log("no ArtistName entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read ArtistName entry");

            if (data.TryGetValue("ArtistSocialAt", out object artistAt))
            {
                cardData.artistAt = (string)artistAt;
            }
            else
            {
                ModInstance.instance.Log("no ArtistSocialAt entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read ArtistSocialAt entry");

            if (data.TryGetValue("ArtistLink", out object artistLink))
            {
                cardData.artistLink = (string)artistLink;
            }
            else
            {
                ModInstance.instance.Log("no ArtistLink entry for " + Path.GetFileName(file));
            }
            ModInstance.log("Read ArtistLink entry");

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
