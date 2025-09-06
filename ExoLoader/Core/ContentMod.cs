using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExoLoader
{
    public class ContentMod
    {
        public string id;
        public string name;
        public string description;
        public string version;
        public List<string> introCredits;
        public List<string> gameCredits;

        public static Dictionary<string, ContentMod> allMods = new();

        public ContentMod(string id)
        {
            this.id = id;
            this.name = id;
        }

        public void ParseJson(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                name = data.ContainsKey("name") ? data["name"]?.ToString() : id;
                description = data.ContainsKey("description") ? data["description"]?.ToString() : null;
                version = data.ContainsKey("version") ? data["version"]?.ToString() : null;

                if (data.ContainsKey("introCredits") && data["introCredits"] is JArray creditsArray)
                {
                    introCredits = creditsArray.ToObject<List<string>>();
                }

                if (data.ContainsKey("gameCredits") && data["gameCredits"] is JArray gameCreditsArray)
                {
                    gameCredits = gameCreditsArray.ToObject<List<string>>();
                }
            }
            catch (Exception e)
            {
                ModLoadingStatus.LogError($"Error while parsing content mod JSON: {e.Message}");
            }
        }
    }
}
