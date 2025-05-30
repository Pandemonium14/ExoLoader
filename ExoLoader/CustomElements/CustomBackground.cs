using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExoLoader
{
    // We may support adding custom background names in the future (for the gallery)
    public class CustomBackground
    {
        public static Dictionary<string, CustomBackground> allBackgrounds = [];
        public static Dictionary<string, Sprite> loadedBackgrounds = new Dictionary<string, Sprite>();


        public string file;
        public string id;
        public string name;

        public static void Add(string id, string file, string name = null)
        {
            if (allBackgrounds.ContainsKey(id))
            {
                ModInstance.log($"Background with id {id} already exists, not adding again.");
                return;
            }

            CustomBackground background = new CustomBackground
            {
                id = id,
                name = name ?? string.Join(" ", id.RemoveStart("pinup_").Split('_').Select(part => part.ToUpperInvariant())).Trim(),
                file = file
            };

            allBackgrounds.Add(id, background);
            ModInstance.log($"Added custom background with id {id}, name {background.name}, file {background.file}");
        }

        public static void updateBackgroundNames(string key, string name)
        {
            string[] possibleKeys = [
                key,
                key + "_f",
                key + "_m",
                key + "_nb"
            ];

            foreach (string possibleKey in possibleKeys)
            {
                if (allBackgrounds.TryGetValue(possibleKey, out CustomBackground background))
                {
                    background.name = name;
                }
            }
        }

        public static void addLocales()
        {
            // Go through all backgrounds and add their localized names
            foreach (var background in allBackgrounds.Values)
            {
                string galleryBgName = "gallery_bg_" + background.id.ToLower();

                TextLocalized tl = new TextLocalized(galleryBgName);
                tl.AddLocale(Locale.EN, background.name);
            }
        }
    }
}
