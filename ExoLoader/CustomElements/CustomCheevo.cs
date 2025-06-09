using System.Collections.Generic;

namespace ExoLoader
{
    public class CustomCheevo : Cheevo
    {
        public static List<CustomCheevo> customCheevos = new List<CustomCheevo>();
        public static Dictionary<string, CustomCheevo> customCheevosByID = new Dictionary<string, CustomCheevo>();

        public string customID;
        public string file;

        // Optional field to automatically grant achievement if player has card3 (max rep) for all of these characters
        public List<string> loveAll = [];
        // Optional field to automatically grant achievement if player has all of these achievements
        public List<string> requiredCheevos = [];

        public CustomCheevo(string customID, string cheevoName, string description, bool hidden, string file, List<string> loveAll = null, List<string> requiredCheevos = null)
            : base(CheevoID.none, cheevoName, description, hidden)
        {
            this.customID = customID;
            this.file = file;
            this.loveAll = loveAll;
            this.requiredCheevos = requiredCheevos;

            // Remove from original collections since we're using CheevoID.none
            allCheevos.Remove(this);

            customCheevos.Add(this);
            customCheevosByID[customID] = this;

            Cheevo.allCheevos.Add(this);
        }
    }
}
