using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static System.Net.Mime.MediaTypeNames;

namespace ExoLoader
{
    public class CFileManager
    {
        public static string commonFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomContent", "common");


        public static CharaData ParseCustomData(string folderName)
        {
            string fullJson = File.ReadAllText(Path.Combine(folderName, "data.json"));
            ModInstance.log("Read text");

            if (fullJson == null ||  fullJson.Length == 0)
            {
                ModInstance.instance.Log("Couldn't read text for " + TrimFolderName(folderName));
                return null;
            }

            Dictionary<string, object> parsedJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            ModInstance.log("Converted json");
            if (parsedJson == null || parsedJson.Count == 0)
            {
                ModInstance.instance.Log("Couldn't parse json for " + TrimFolderName(folderName));
                return null;
            }
            CharaData data = new CharaData();

            if (parsedJson.TryGetValue("Data", out object dataValue))
            {
                Dictionary<string, object> dataMap;
                try
                {
                    dataMap = ((JObject)dataValue).ToObject<Dictionary<string, object>>();
                } catch (Exception e)
                {
                    ModInstance.log(e.ToString());
                    ModInstance.log(dataValue.GetType().Name);
                    return null;
                }
                ModInstance.log("Reading Data entry");


                data.id = (string) dataMap.GetValueSafe("ID");
                data.name = (string) dataMap.GetValueSafe("NAME");
                data.nickname = (string) dataMap.GetValueSafe("NICKNAME");

                //ModInstance.log("ID, NAME, and NICKNAME read");

                dataMap.TryGetValue("GENDER", out object g);
                if (((string) g).Equals("X"))
                {
                    data.gender = GenderID.nonbinary;
                }
                else if (((string) g).Equals("F"))
                {
                    data.gender = GenderID.female;
                }
                else if (((string) g).Equals("M"))
                {
                    data.gender = GenderID.male;
                }

                dataMap.TryGetValue("LOVE", out object love);
                if (((string) love).Equals("TRUE"))
                {
                    data.canLove = true;
                }
                else if (love.Equals("FALSE"))
                {
                    data.canLove = false;
                }
                ModInstance.log("Gender and Love read");

                dataMap.TryGetValue("AGE10", out object age10);
                data.ageOffset = int.Parse((string) age10) - 10;
                //ModInstance.log("Age read");

                data.birthday = (string) dataMap.GetValueSafe("BIRTHDAY");
                data.dialogueColor = (string) dataMap.GetValueSafe("DIALOGUECOLOR");
                data.defaultBg = (string) dataMap.GetValueSafe("DEFAULTBG");
                data.basicInfo = (string) dataMap.GetValueSafe("BASICS");
                data.moreInfo = (string) dataMap.GetValueSafe("MORE");
                data.augment = (string) dataMap.GetValueSafe("ENHANCEMENT");

                //ModInstance.log("Up to Sliders read");

                data.slider1left = (string) dataMap.GetValueSafe("FILLBAR1LEFT");
                data.slider1right = (string) dataMap.GetValueSafe("FILLBAR1RIGHT");
                data.slider1values = new int[]
                {
                int.Parse((string) dataMap.GetValueSafe("FILLBAR1CHILD")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR1TEEN")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR1ADULT"))
                };
                data.slider2left = (string) dataMap.GetValueSafe("FILLBAR2LEFT");
                data.slider2right = (string) dataMap.GetValueSafe("FILLBAR2RIGHT");
                data.slider2values = new int[]
                {
                int.Parse((string)  dataMap.GetValueSafe("FILLBAR2CHILD")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR2TEEN")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR2ADULT"))
                };
                data.slider3left = (string) dataMap.GetValueSafe("FILLBAR3LEFT");
                data.slider3right = (string) dataMap.GetValueSafe("FILLBAR3RIGHT");
                data.slider3values = new int[]
                {
                int.Parse((string) dataMap.GetValueSafe("FILLBAR3CHILD")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR3TEEN")),
                int.Parse((string) dataMap.GetValueSafe("FILLBAR3ADULT")) };
                ModInstance.log("Data entry read");
            }
            else
            {
                ModInstance.instance.Log("Data entry not found for " + TrimFolderName(folderName));
                return null;
            }

            if (parsedJson.TryGetValue("OnMap", out object onMap))
            {
                data.onMap = (string)onMap == "TRUE";
            }
            else
            {
                ModInstance.instance.Log("OnMap entry for " + TrimFolderName(folderName));
                return null;
            }
            ModInstance.log("OnMap read");

            if (data.onMap)
            {
                if (parsedJson.TryGetValue("HelioOnly", out object helioOnly))
                {
                    data.helioOnly = (string)helioOnly == "TRUE";
                }
                else
                {
                    ModInstance.instance.Log("No helio only entry for " + TrimFolderName(folderName));
                    return null;
                }
                //ModInstance.log("HelioOnly read");

                if (!data.helioOnly)
                {
                    string[] stringMapSpot = ((JArray)(parsedJson.GetValueSafe("PreHelioMapSpot"))).ToObject<string[]>();
                    if (stringMapSpot == null || stringMapSpot.Length == 0)
                    {
                        ModInstance.instance.Log("No PreHelioMapSpot entry for " + TrimFolderName(folderName));
                        return null;
                    }
                    float[] mapSpot = { float.Parse(stringMapSpot[0]), float.Parse(stringMapSpot[1]), float.Parse(stringMapSpot[2]) };
                    data.stratoMapSpot = mapSpot;

                    string[] stringMapSpotD = ((JArray)(parsedJson.GetValueSafe("DestroyedMapSpot"))).ToObject<string[]>();
                    if (stringMapSpot == null || stringMapSpot.Length == 0)
                    {
                        ModInstance.instance.Log("No DestroyedMapSpot entry for " + TrimFolderName(folderName));
                        return null;
                    }
                    float[] mapSpotD = { float.Parse(stringMapSpotD[0]), float.Parse(stringMapSpotD[1]), float.Parse(stringMapSpotD[2]) };
                    data.destroyedMapSpot = mapSpotD;
                }
                //ModInstance.log("Non-HelioOnly map spots read");

                string[] stringMapSpotHelio = ((JArray)(parsedJson.GetValueSafe("PostHelioMapSpot"))).ToObject<string[]>();
                if (stringMapSpotHelio == null || stringMapSpotHelio.Length == 0)
                {
                    ModInstance.instance.Log("No PostHelioMapSpot entry for " + TrimFolderName(folderName));
                    return null;
                }
                float[] mapSpotHelio = { float.Parse(stringMapSpotHelio[0]), float.Parse(stringMapSpotHelio[1]), float.Parse(stringMapSpotHelio[2]) };
                data.helioMapSpot = mapSpotHelio;

                ModInstance.log("Helio map spot read");
            }

            string[] likes = ((JArray)(parsedJson.GetValueSafe("Likes"))).ToObject<string[]>();
            if (likes == null)
            {
                ModInstance.instance.Log("No Likes entry for " + TrimFolderName(folderName));
                return null;
            }
            data.likes = likes;
            ModInstance.log("Likes read");

            string[] dislikes = ((JArray)(parsedJson.GetValueSafe("Likes"))).ToObject<string[]>();
            if (dislikes == null)
            {
                ModInstance.instance.Log("No Dislikes entry for " + TrimFolderName(folderName));
                return null;
            }
            data.dislikes = dislikes;
            ModInstance.log("Dislikes read");


            string skeleton = (string)parsedJson.GetValueSafe("Skeleton");
            if (skeleton == null || skeleton.Length == 0)
            {
                ModInstance.instance.Log("No Skeleton entry for " + TrimFolderName(folderName));
                return null;
            }
            data.skeleton = skeleton;
            ModInstance.log("Skeleton read");

            int spriteSize = int.Parse((string)parsedJson.GetValueSafe("SpriteSize"));
            data.spriteSize = spriteSize;

            if (parsedJson.TryGetValue("Ages", out object ages))
            {
                data.ages = (string)ages == "TRUE";
            }
            else
            {
                ModInstance.instance.Log("No Ages entry for " + TrimFolderName(folderName));
                return null;
            }
            ModInstance.log("Ages read");

            string[] jobs = ((JArray)(parsedJson.GetValueSafe("Jobs"))).ToObject<string[]>();
            if (jobs != null)
            {
                data.jobs = jobs;
            }
            else
            {
                ModInstance.instance.Log("No Jobs entry for " + Path.GetFileName(folderName));
            }
            ModInstance.log("Jobs read");

            ModInstance.instance.Log("Finished Parsing");
            data.folderName = folderName;
            return data;
        }

        public static string[] GetAllCustomCharaFolders()
        {
            List<string> list = new List<string>();
            foreach (string bundleFolder in GetAllCustomContentFolders())
            {
                foreach (string characterFolder in Directory.GetDirectories(bundleFolder))
                {
                    if (Path.GetFileName(characterFolder) == "Characters")
                    {
                        list.AddRange(Directory.GetDirectories(characterFolder));
                    }
                }
            }
            return list.ToArray();
        }

        public static List<string> GetAllCustomContentFolders(string type) {
            List<string> patchFolders = new List<string>();
            foreach (string contentFolder in GetAllCustomContentFolders())
            {
                foreach (string candidateFolder in Directory.GetDirectories(contentFolder))
                {
                    if (Path.GetFileName(candidateFolder) == type)
                    {
                        patchFolders.Add(candidateFolder);
                    }
                }
            }
            return patchFolders;
        }

        public static string[] GetAllCustomContentFolders()
        {
            return Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomContent"));
        }


        public static Dictionary<string,Sprite> customSprites = new Dictionary<string,Sprite>();

        public static Sprite GetCustomImage(string folderName, string imageName)
        {
            return GetCustomImage(folderName, imageName, 16);
        }

        public static Sprite GetCustomImage(string folderName, string imageName, int targetHeight)
        {
            ModInstance.log("Looking for image " + imageName + " in " + TrimFolderName(folderName));
            if (customSprites.ContainsKey(imageName))
            {
                ModInstance.log("Requested image is already loaded!");
                return customSprites[imageName];
            } else
            {
                string imagePath = Path.Combine(folderName, "Sprites", imageName + ".png");
                if (!File.Exists(imagePath))
                {
                    ModInstance.log("Couldn't find image " + TrimFolderName(imagePath));
                    return null;
                }
                Sprite image = null;
                Texture2D texture = null;
                Byte[] bytes = null;
                try
                {
                    texture = new Texture2D(2,2);
                    bytes = File.ReadAllBytes(imagePath);
                    ImageConversion.LoadImage(texture, bytes);
                    texture.Apply();
                    int height = texture.height;
                    int targetHeightInUnits = targetHeight;
                    float density = height / targetHeightInUnits;
                    image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0), density);
                } catch (Exception e)
                {
                    ModInstance.log("Couldn't make sprite from file " + TrimFolderName(imagePath));
                    ModInstance.log(texture == null ? "The texture is null" : texture.isReadable.ToString());
                    ModInstance.log(bytes.Length.ToString() + "bytes in the image");
                    ModInstance.log(e.ToString());
                }
                customSprites.Add(imageName, image);
                ModInstance.log("Sprite created, " + image.texture.height + " in height");
                return image;
            }
        }

        public static Sprite GetCustomPortrait(string folderName,string imageName)
        {
            ModInstance.log("Looking for portrait image " + imageName + " in folder " +  TrimFolderName(folderName));
            string portraitName = "portrait_" + imageName;
            if (customSprites.ContainsKey(portraitName))
            {
                ModInstance.log("Requested image is already loaded!");
                return customSprites[portraitName];
            }

            string path = Path.Combine(folderName, "Sprites", portraitName + ".png");
            if (!File.Exists(path))
            {
                ModInstance.log("Couldn't find image " + TrimFolderName(path));
                return null;
            }

            
            Texture2D texture = GetTexture(path);
            Sprite image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0));
            customSprites.Add(portraitName, image);
            ModInstance.log("Sprite created, " + image.texture.height + " in height");
            return image;
        }

        public static Texture2D GetTexture(string path)
        {
            Texture2D texture = null;
            Byte[] bytes = null;
            try
            {
                texture = new Texture2D(2, 2);
                bytes = File.ReadAllBytes(path);
                ImageConversion.LoadImage(texture, bytes);
                texture.Apply();
            }
            catch (Exception e)
            {
                ModInstance.log("Couldn't make sprite from file " + TrimFolderName(path));
                ModInstance.log(texture == null ? "The texture is null" : texture.isReadable.ToString());
                ModInstance.log(bytes.Length.ToString() + "bytes in the image");
                ModInstance.log(e.ToString());
            }
            return texture;
        }


        public static Sprite GetCustomCardSprite(string cardID, string originFile)
        {
            if (originFile == null)
            {
                ModInstance.log("Tried getting sprite for non-custom card");
                return null;
            }

            ModInstance.log("Looking for card image " + cardID);
            string spriteName = "card_" + cardID;
            if (customSprites.ContainsKey(spriteName))
            {
                ModInstance.log("Requested image is already loaded!");
                return customSprites[spriteName];
            }

            string path = originFile.Replace(".json", ".png");
            if (!File.Exists(path))
            {
                ModInstance.log("Couldn't find image " + Path.GetFileName(path));
                return null;
            }

            Sprite image = null;
            Texture2D texture = null;
            Byte[] bytes = null;
            try
            {
                texture = new Texture2D(2, 2);
                bytes = File.ReadAllBytes(path);
                ImageConversion.LoadImage(texture, bytes);
                texture.Apply();
                image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0));
            }
            catch (Exception e)
            {
                ModInstance.log("Couldn't make sprite from file " + TrimFolderName(path));
                ModInstance.log(texture == null ? "The texture is null" : texture.isReadable.ToString());
                ModInstance.log(bytes.Length.ToString() + "bytes in the image");
                ModInstance.log(e.ToString());
            }
            customSprites.Add(spriteName, image);
            ModInstance.log("Sprite created, " + image.texture.height + " in height");
            return image;
        }

    public static string TrimFolderName(string folderName)
    {
        return folderName.RemoveStart(AppDomain.CurrentDomain.BaseDirectory);
    }
    }
}
