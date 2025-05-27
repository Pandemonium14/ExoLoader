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
    public class CustomCollectible
    {
        public static Dictionary<string, string> idToFile = new Dictionary<string, string>();
        public static Dictionary<string, CustomCollectible> customCollectiblesById = new Dictionary<string, CustomCollectible>();

        public string file;
        public string id;
        public string name;
        public string namePlural;
        public string cardId;
        public int chance;
        public bool isPlant;

        // To populate to the chara data
        public string[] like;
        public string[] dislike;

        // To populate to the card data
        public HowGet howGet;
        public string artist;
        public string artistAt;
        public string artistLink;
        public int kudoCost = 0;
        public List<CardAbilityType> abilityIds = new List<CardAbilityType>();
        public List<int> abilityValues = new List<int>();
        public List<CardSuit> abilitySuits = new List<CardSuit>();

        public void MakeCollectible()
        {
            customCollectiblesById.Add(id, this);

            ModInstance.log("----> Adding collectible to dictionary, id = " + id + ", file = " + file);
            idToFile.Add(id, file);

            // First create a card
            CustomCardData card = new CustomCardData
            {
                file = file,
                id = cardId,
                name = name,
                type = CardType.collectible,
                suit = CardSuit.none, // Collectibles don't have a suit
                level = 1,
                howGet = howGet,
                value = 1,
                upgradeFromCardID = null,
                artist = artist,
                artistAt = artistAt,
                artistLink = artistLink,
                kudoCost = kudoCost,
                abilityIds = abilityIds,
                abilityValues = abilityValues,
                abilitySuits = abilitySuits
            };

            card.MakeCard();

            CardData cardData = CardData.FromID(cardId);

            if (cardData == null)
            {
                ModInstance.log($"Error adding a card for collectible {id}. CardData is null.");
                return;
            }

            bool hasBattleEffect = abilityIds.Count > 0;

            // Create the collectible
            Collectible collectible = new Collectible(id, name, namePlural, cardData, chance, [], [], isPlant, hasBattleEffect);

            // If we have like/dislikes, find characters by the ids in like/dislike and add item there
            if (like != null && like.Length > 0)
            {
                foreach (string charaId in like)
                {
                    Chara chara = Chara.FromID(charaId);
                    if (chara != null)
                    {
                        if (!chara.likedCards.Contains(cardData))
                        {
                            chara.likedCards.Add(cardData);
                            ModInstance.log($"Added collectible {id} to liked cards of character {charaId}.");
                        }
                        else
                        {
                            ModInstance.log($"Collectible {id} already exists in liked cards of character {charaId}.");
                        }
                    }
                    else
                    {
                        ModInstance.log($"Character with ID {charaId} not found for collectible {id}.");
                    }
                }
            }

            if (dislike != null && dislike.Length > 0)
            {
                foreach (string charaId in dislike)
                {
                    Chara chara = Chara.FromID(charaId);
                    if (chara != null)
                    {
                        if (!chara.dislikedCards.Contains(cardData))
                        {
                            chara.dislikedCards.Add(cardData);
                            ModInstance.log($"Added collectible {id} to disliked cards of character {charaId}.");
                        }
                        else
                        {
                            ModInstance.log($"Collectible {id} already exists in disliked cards of character {charaId}.");
                        }
                    }
                    else
                    {
                        ModInstance.log($"Character with ID {charaId} not found for collectible {id}.");
                    }
                }
            }
        }
    }
}
