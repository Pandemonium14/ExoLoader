
using System.Collections.Generic;

namespace ExoLoader
{
    public class CustomMemChange
    {
        public static Dictionary<string, CustomMemChange> changesByID = new Dictionary<string, CustomMemChange>();

        public string ID; // key of the memory, will be like tammyConfidence for mem_tammyConfidence
        public string name; // name for display

        public CustomMemChange(string id, string name)
        {
            ID = id.ToLower();
            this.name = name;

            if (!changesByID.ContainsKey(ID))
            {
                changesByID.Add(ID, this);
                ModInstance.instance.Log("Custom memory change created: " + id + " - " + name);
            }
            else
            {
                ModInstance.instance.Log("Custom memory change with ID " + ID + " already exists. Skipping.");
            }
        }
    }
}
