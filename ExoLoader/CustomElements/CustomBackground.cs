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
                name = name ?? id,
                file = file
            };

            allBackgrounds.Add(id, background);
            ModInstance.log($"Added custom background with id {id}, name {background.name}, file {background.file}");
        }
    }
}
