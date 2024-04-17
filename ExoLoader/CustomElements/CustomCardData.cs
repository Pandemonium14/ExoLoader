using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExoLoader
{
    public class CustomCardData
    {
        public static Dictionary<string,string> idToFile = new Dictionary<string,string>();


        public string file;
        public string id;
        public string name;
        public CardType type;
        public int level;
        public CardSuit suit;
        public int value;
        public string artist;
        public string artistAt;
        public string artistLink;

        private int kudoCost = 0;

        public List<CardAbilityType> abilityIds = new List<CardAbilityType>();
        public List<int> abilityValues = new List<int>();
        public List<CardSuit> abilitySuits = new List<CardSuit>();

        public void MakeCard()
        {
            ModInstance.log("----> Adding card to dictionary, id = " +  id + ", file = " + file);
            idToFile.Add(id, file);
            new CardData(id, name, type, suit, level, value)
            {
                originalAbilityTypes = abilityIds,
                originalAbilityValues = abilityValues,
                originalAbilitySuits = abilitySuits,
                kudosCost = kudoCost,
                upgradeFromCardID = null,
                howGet = HowGet.none,
                artistName = artist,
                artistSocialAt = artistAt,
                artistSocialUrl = artistLink
            };

            CardData check = CardData.FromID(id);
            if (check == null)
            {
                ModInstance.log("Card wasn't in list after MakeCard call");
            }
        }

    }
}
