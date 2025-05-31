
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExoLoader
{
    public class ScriptExtensionsParser
    {
        public static void ParseFile(string file)
        {
            ModInstance.instance.Log("Parsing script extensions");
            string fullJson = File.ReadAllText(file);
            if (fullJson == null || fullJson.Length == 0)
            {
                ModInstance.instance.Log("Couldn't read text for " + Path.GetFileNameWithoutExtension(file));
                return;
            }

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullJson);
            if (data == null || data.Count == 0)
            {
                ModInstance.instance.Log("Couldn't parse json for " + Path.GetFileName(file));
                return;
            }

            string ID = (string)data["ID"];
            string name = (string)data["Name"];
            
            switch ((string)data["Type"])
            {
                // MemChange is for cases like tammyConfidence, which is a normal memory flag, but has some additional display logic (results bubbles, displayed button requirements, etc.)
                case "MemChange":
                    {
                        new CustomMemChange(ID, name);
                        break;
                    }
                default:
                    {
                        ModInstance.instance.Log("Unknown type " + (string)data["Type"] + " for script extension " + ID);
                        return;
                    }
            }
            
        }
    }
}
