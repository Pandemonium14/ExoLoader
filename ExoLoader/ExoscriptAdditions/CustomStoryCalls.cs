using System;
using System.Linq;

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

    public static bool charaexist(string charaId)
    {
        Chara chara = Chara.FromID(charaId);

        return chara != null;
    }
}
