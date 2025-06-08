using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ExoLoader;

[HarmonyPatch]
public class ExoscriptAdditions
{
    [HarmonyPatch(typeof(StoryCall), nameof(StoryCall.CreateStoryCall))]
    [HarmonyPostfix]
    public static void StoryCallCreateStoryCallPatch(ref StoryCall __result, string methodName, object[] parameterArray, string debugString = null)
    {
        try
        {
            if (__result == null)
            {
                Type[] array = new Type[parameterArray.Length];
                for (int i = 0; i < parameterArray.Length; i++)
                {
                    array[i] = parameterArray[i].GetType();
                }

                MethodInfo method = typeof(CustomStoryCalls).GetMethod(methodName, array) ?? typeof(CustomStoryCalls).GetMethod(methodName);
                if (method != null)
                {
                    ModInstance.log($"Found custom method '{methodName}'");
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
                    __result = new StoryCall
                    {
                        methodName = methodName,
                        parameterArray = parameterArray,
                        debugString = debugString,
                        methodInfo = method
                    };
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ModInstance.log($"Error in StoryCallCreateStoryCallPatch: {ex}");
        }
    }

    // Modify button to appear as "groundhog" when calling custom story calls starting ending with "hog"
    [HarmonyPatch(typeof(Choice), "isGroundhog", MethodType.Getter)]
    [HarmonyPostfix]
    public static void ChoiceIsGroundhogPatch(ref bool __result, Choice __instance)
    {
        if (__result)
        {
            return; // Already a groundhog choice
        }

        try
        {
            foreach (StoryReq requirement in __instance.requirements)
            {
                if (requirement.type == StoryReqType.call)
                {
                    if (requirement.call != null && requirement.call.methodName.EndsWith("hog", StringComparison.OrdinalIgnoreCase))
                    {
                        // If the method name ends with "hog", set the choice as a groundhog choice
                        __result = true;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModInstance.log($"Error in ChoiceIsGroundhogPatch: {ex}");
        }
    }

    // Dating field
    [HarmonyPatch(typeof(Chara))]
    [HarmonyPatch("GetFact", new Type[] { typeof(CharaFact), typeof(bool) })]
    [HarmonyPostfix]
    public static void CharaGetFactPatch(Chara __instance, ref string __result, CharaFact fact, bool force)
    {
        // Memories set as mem_custom_<firstCharaId>_<secondCharaId>_relationship
        try
        {
            if (fact == CharaFact.date)
            {
                string charaID = __instance.charaID;
                string patchMemPrefix = "custom_";
                string patchMemPostfix = "_relationship";

                StringDictionary memories = Princess.memories;
                string additionalText = "";

                foreach (string key in memories.Keys)
                {
                    if (key.StartsWith(patchMemPrefix) && key.EndsWith(patchMemPostfix) && key.Contains(charaID))
                    {
                        // remove prefix and postfix
                        string memory = key.RemoveStart(patchMemPrefix);
                        if (memory.EndsWith(patchMemPostfix))
                        {
                            memory = memory.Substring(0, memory.Length - patchMemPostfix.Length);
                        }

                        string[] parts = memory.Split('_');

                        if (parts.Length > 1 && (
                            parts[0].EqualsIgnoreCase(charaID) ||
                            parts[1].EqualsIgnoreCase(charaID)))
                        {
                            string newCharaID = parts[1] == charaID ? parts[0] : parts[1];
                            Chara chara = Chara.FromID(newCharaID);
                            if (chara != null && chara.hasMet)
                            {
                                var memoryText = memories.Get(key);
                                string formattedMemory = "";

                                // Process based on memory value type
                                if (bool.TryParse(memoryText, out bool boolValue))
                                {
                                    // Boolean case - just use the localized text
                                    formattedMemory = TextLocalized.Localize("charas_datingChara", chara.nickname);
                                }
                                else
                                {
                                    // String case - parse and format
                                    string[] textParts = memoryText.Split('|');

                                    if (textParts.Length == 1)
                                    {
                                        formattedMemory = textParts[0].Replace("{chara}", chara.nickname);
                                    }
                                    else if (textParts.Length > 1)
                                    {
                                        // Two or more elements - choose based on character order in the key
                                        bool isFirstCharaInKey = parts[0].EqualsIgnoreCase(charaID);
                                        int partIndex = isFirstCharaInKey ? 0 : Math.Min(1, textParts.Length - 1);
                                        formattedMemory = textParts[partIndex].Replace("{chara}", chara.nickname);
                                    }
                                }

                                // Add the formatted memory to additional text
                                additionalText += formattedMemory + "\n";
                            }
                            else
                            {
                                ModInstance.log($"Chara '{newCharaID}' not found or has not met. '{chara?.hasMet}', '{chara?.nickname}'");
                            }
                        }
                    }
                }

                if (additionalText.IsNullOrEmptyOrWhitespace())
                {
                    return;
                }

                if (__result.IsNullOrEmptyOrWhitespace())
                {
                    __result = additionalText.RemoveEnding("\n");
                    return;
                }
                else
                {
                    __result += $"\n{additionalText}".RemoveEnding("\n");
                    return;
                }
            }
            return;
        }
        catch (Exception ex)
        {
            ModInstance.log($"Error in CharaGetFactPatch: {ex}");
            return;
        }
    }

    // Correct conditional card in choice buttons
    [HarmonyPatch(typeof(Choice))]
    [HarmonyPatch("GetCards")]
    [HarmonyPostfix]
    public static void ChoiceGetCardsPatch(Choice __instance, ref List<CardData> __result)
    {
        try
        {
            List<CardData> list = new List<CardData>();
            foreach (StorySet set in __instance.sets)
            {
                AddCardConditional(set, ref list);
            }

            __result = list;
            return;
        }
        catch (Exception ex)
        {
            ModInstance.log($"Error in ChoiceGetCardsPatch: {ex}");
            return;
        }
    }

    // Method that adds a card to the list if there is no requirement
    // If there is requirement, it will check if the requirement is met
    // If it is, it will add the card to the list
    // If not and there is an elseSet, run elseSet through this method
    private static void AddCardConditional(StorySet set, ref List<CardData> list)
    {
        if (set.type == StorySetType.card && set.cardData != null && !(set.stringValue == "hidden"))
        {
            if (set.requirement != null)
            {
                if (set.requirement.Execute(new Result()))
                {
                    // Requirement passed, add the card to the list
                    list.Add(set.cardData);
                }
                else if (set.elseSet != null)
                {
                    AddCardConditional(set.elseSet, ref list);
                }
            }
            else
            {
                // No requirements, add the card to the list as normal
                list.Add(set.cardData);
            }
        }
    }

    // Conditional sprite shown on maps
    [HarmonyPatch(typeof(Story))]
    [HarmonyPatch("GetSpriteID")]
    [HarmonyPostfix]
    public static void StoryGetSpriteIDPatch(Story __instance, ref string __result)
    {
        try
        {
            foreach (StorySet set in __instance.entryChoice.sets)
            {
                if (set.type == StorySetType.billboardSprite)
                {
                    __result = ReturnSpriteConditional(set);
                }
            }
        }
        catch (Exception ex)
        {
            ModInstance.log($"Error in StoryGetSpriteIDPatch: {ex}");
            return;
        }
    }

    private static string ReturnSpriteConditional(StorySet set)
    {
        if (set.type == StorySetType.billboardSprite)
        {
            if (set.requirement != null)
            {
                if (set.requirement.Execute(new Result()))
                {
                    return set.stringValue;
                }
                else if (set.elseSet != null)
                {
                    return ReturnSpriteConditional(set.elseSet);
                }
            }
            else
            {
                return set.stringValue;
            }
        }

        return null;
    }

    // Correct conditional flirt icon
    [HarmonyPatch(typeof(Choice))]
    [HarmonyPatch("GetFlirtChara")]
    [HarmonyPostfix]
    public static void ChoiceGetFlirtCharaPatch(Choice __instance, ref Chara __result)
    {
        try
        {
            foreach (StorySet set in __instance.sets)
            {
                GetCharaConditional(set, ref __result);
            }

            return;
        }
        catch (Exception ex)
        {
            ModInstance.log($"Error in ChoiceGetFlirtCharaPatch: {ex}");
            return;
        }
    }

    private static void GetCharaConditional(StorySet set, ref Chara __result)
    {
        if (set.type == StorySetType.memory && set.stringID.StartsWith("flirt_") && set.stringValue == "true")
        {
            if (set.requirement != null)
            {
                Result result = new Result();

                if (set.requirement.Execute(result))
                {
                    __result = Chara.FromID(set.stringID.RemoveStart("flirt_"));
                    return;
                }
                else if (set.elseSet != null)
                {
                    GetCharaConditional(set.elseSet, ref __result);
                }
                else
                {
                    // This means that the requirement failed and we should not show the flir icon
                    __result = null;
                    return;
                }
            }
            else
            {
                __result = Chara.FromID(set.stringID.RemoveStart("flirt_"));
                return;
            }
        }
    }
}
