using System;
using System.Linq;
using Northway.Utils;

namespace ExoLoader;

class CustomStoryCalls : StoryCalls
{
    public static bool hascheevo(string cheevoStringID)
    {
        // Convert string to CheevoID enum
        if (Enum.TryParse<CheevoID>(cheevoStringID.RemoveStart("cheevo_").ToLower().Trim(), out var cheevoID))
        {
            // Check if the cheevo is unlocked
            bool value = Groundhogs.instance.cheevos.ContainsSafe(cheevoID);

            return value;
        }
        else if (CustomCheevo.customCheevosByID.TryGetValue(cheevoStringID.ToLower(), out CustomCheevo customCheevo))
        {
            bool value = ExoLoaderSave.HasCheevo(customCheevo.customID);

            return value;
        }
        else
        {
            ModInstance.log($"Failed to parse cheevoID from '{cheevoStringID}'");
            return false;
        }
    }

    public static bool hascheevohog(string cheevoStringID)
    {
        if (Princess.HasMemory("hogsdisabled"))
        {
            return false;
        }
        return hascheevo(cheevoStringID);
    }

    public static bool hasending(string endingStringID)
    {
        // add ending_ prefix if not present
        string endingID = endingStringID.StartsWith("ending_") ? endingStringID : "ending_" + endingStringID;

        bool value = Groundhogs.instance.seenBackgrounds.ContainsSafe(endingID) ||
            Groundhogs.instance.seenBackgrounds.ContainsSafe(endingID + "_f") ||
            Groundhogs.instance.seenBackgrounds.ContainsSafe(endingID + "_m") ||
            Groundhogs.instance.seenBackgrounds.ContainsSafe(endingID + "_nb");

        return value;
    }

    public static bool hasendinghog(string endingStringID)
    {
        if (Princess.HasMemory("hogsdisabled"))
        {
            return false;
        }
        return hasending(endingStringID);
    }

    public static int randomnumber(int min, int max)
    {
        if (min >= max)
        {
            (max, min) = (min, max);
        }

        return new Random().Next(min, max);
    }

    public static string randomitem(string itemIdsString)
    {
        string[] itemIds = [.. itemIdsString.Split([';'], StringSplitOptions.RemoveEmptyEntries).ToArray().Select(id => id.Trim())];
        if (itemIds == null || itemIds.Length == 0)
        {
            ModInstance.log("No items provided for random selection.");
            return string.Empty;
        }

        int index = new Random().Next(itemIds.Length);
        return itemIds[index];
    }

    public static bool charaexists(string charaId)
    {
        Chara chara = Chara.FromID(charaId);

        return chara != null;
    }

    public static bool storyexists(string storyId)
    {
        Story story = Story.FromID(storyId);

        return story != null;
    }

    public static bool bgexists(string backgroundId)
    {
        return Singleton<AssetManager>.instance.animationNames.Contains(backgroundId) || Singleton<AssetManager>.instance.backgroundAndEndingNames.Contains(backgroundId);
    }

    public static bool spriteexists(string spriteName)
    {
        if (Singleton<AssetManager>.instance.charaSpriteNames.Contains(spriteName))
        {
            return true;
        }

        // We might need to check for the age stage sprites as well
        string[] parts = spriteName.Split('_');
        if (parts.Length < 2)
        {
            return false; // Invalid sprite name format
        }

        int artStage = Princess.artStage;
        string currentSpriteName = parts[0] + artStage + "_" + string.Join("_", parts.Skip(1));

        return Singleton<AssetManager>.instance.charaSpriteNames.Contains(currentSpriteName);
    }

    public static bool cardexists(string cardId)
    {
        CardData card = CardData.FromID(cardId);

        return card != null;
    }

    public static bool collectibleexists(string cardId)
    {
        CardData card = CardData.FromID(cardId);

        return card != null && card.type == CardType.collectible;
    }

    public static bool gearexists(string cardId)
    {
        CardData card = CardData.FromID(cardId);

        return card != null && card.type == CardType.gear;
    }

    public static bool thingsexist(string thingsString)
    {
        string[] things = [.. thingsString.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())];

        if (things.Length == 0)
        {
            return false;
        }

        foreach (string thing in things)
        {
            string prefix = thing.Split('_')[0];
            string id = thing.Substring(prefix.Length + 1);

            switch (prefix)
            {
                case "card":
                    if (!cardexists(id))
                    {
                        return false;
                    }
                    break;
                case "chara":
                    if (!charaexists(id))
                    {
                        return false;
                    }
                    break;
                case "story":
                    if (!storyexists(id))
                    {
                        return false;
                    }
                    break;
                case "bg":
                    if (!bgexists(id))
                    {
                        return false;
                    }
                    break;
                case "sprite":
                    if (!spriteexists(id))
                    {
                        return false;
                    }
                    break;
                case "collectible":
                    if (!collectibleexists(id))
                    {
                        return false;
                    }
                    break;
                case "gear":
                    if (!gearexists(id))
                    {
                        return false;
                    }
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    // method that would return how many characters player is dating right now
    public static int datingcount()
    {
        return Chara.allCharas.Count(c => c.isDatingYou && !c.isDead);
    }
}
