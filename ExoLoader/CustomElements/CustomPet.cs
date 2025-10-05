using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace ExoLoader
{
    public class CustomPet
    {
        public static Dictionary<string, string> idToFile = new Dictionary<string, string>();
        public static Dictionary<string, CustomPet> customPetsById = new Dictionary<string, CustomPet>();

        public string file;
        public string id;
        public string name;
        public BillboardWaveMode animation;

        // To populate to the card data
        public int level;
        public string upgradeFromCardID;
        public HowGet howGet;
        public string artist;
        public string artistAt;
        public string artistLink;
        public int kudoCost = 0;
        public List<CardAbilityType> abilityIds = new List<CardAbilityType>();
        public List<int> abilityValues = new List<int>();
        public List<CardSuit> abilitySuits = new List<CardSuit>();

        public void MakePet()
        {
            customPetsById.Add(id, this);
            idToFile.Add(id, file);

            CustomCardData card = new CustomCardData
            {
                file = file,
                id = id,
                name = name,
                type = CardType.gear,
                suit = CardSuit.none,
                level = level,
                howGet = howGet,
                value = 1,
                upgradeFromCardID = upgradeFromCardID,
                artist = artist,
                artistAt = artistAt,
                artistLink = artistLink,
                kudoCost = kudoCost,
                abilityIds = abilityIds,
                abilityValues = abilityValues,
                abilitySuits = abilitySuits
            };

            card.MakeCard();
        }
    }
}
