using System;
using HarmonyLib;

namespace ExoLoader
{
    [HarmonyPatch]
    public class CharaDataOverridePatches
    {
        private static bool IsValidOverride(CharaDataOverride overrideData, int month = -1)
        {
            if (overrideData == null)
            {
                return false;
            }

            int effectiveMonth = month;

            if (month == -1 && Savegame.instance != null)
            {
                // If month is not specified, use the current week from the savegame
                effectiveMonth = Savegame.instance.week;
            }

            // Check if the override is valid based on the current month and required memories
            if (effectiveMonth < overrideData.startDate)
            {
                return false;
            }

            if (overrideData.requiredMemories != null)
            {
                foreach (var memory in overrideData.requiredMemories)
                {
                    // memory can start with "!" to indicate it should not be present
                    if (memory.StartsWith("!"))
                    {
                        if (Princess.HasMemory(memory.Substring(1)))
                        {
                            return false;
                        }
                    }
                    else if (!Princess.HasMemory(memory))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string GetLatestOverrideOfType(CustomChara chara, CharaDataOverrideField field)
        {
            if (chara == null || chara.data == null || chara.data.overrides == null || !chara.data.overrides.ContainsKey(field) || chara.data.overrides[field] == null)
            {
                return null;
            }

            CharaDataOverride latestOverride = null;
            foreach (var d in chara.data.overrides[field])
            {
                if (IsValidOverride(d))
                {
                    latestOverride = d;
                }
            }

            return latestOverride?.value;
        }

        public class CharaFieldValues
        {
            public string name;
            public string nickname;
            public string basicInfo;
            public string moreInfo;
            public string dialogueColor;
            public string augment;
            public string defaultBg;
        }

        private static CharaFieldValues GetCharaFieldValues(CustomChara chara, int month = -1)
        {
            if (chara == null || chara.data == null)
            {
                return null;
            }

            string name = chara.data.name;
            string nickname = chara.data.nickname;
            string basicInfo = chara.data.basicInfo;
            string moreInfo = chara.data.moreInfo;
            string dialogueColor = chara.data.dialogueColor;
            string augment = chara.data.augment;
            string defaultBg = chara.data.defaultBg;

            foreach (var field in chara.data.overrides.Keys)
            {
                foreach (CharaDataOverride data in chara.data.overrides[field])
                {
                    if (IsValidOverride(data, month))
                    {
                        switch (field)
                        {
                            case CharaDataOverrideField.name:
                                name = data.value;
                                break;
                            case CharaDataOverrideField.nickname:
                                nickname = data.value;
                                break;
                            case CharaDataOverrideField.basicInfo:
                                basicInfo = data.value;
                                break;
                            case CharaDataOverrideField.moreInfo:
                                moreInfo = data.value;
                                break;
                            case CharaDataOverrideField.dialogueColor:
                                dialogueColor = data.value;
                                break;
                            case CharaDataOverrideField.augment:
                                augment = data.value;
                                break;
                            case CharaDataOverrideField.defaultBg:
                                defaultBg = data.value;
                                break;
                        }
                    }
                }
            }

            return new CharaFieldValues
            {
                name = name,
                nickname = nickname,
                basicInfo = basicInfo,
                moreInfo = moreInfo,
                dialogueColor = dialogueColor,
                augment = augment,
                defaultBg = defaultBg
            };
        }

        [HarmonyPatch(typeof(BillboardManager), nameof(BillboardManager.FillMapspots))]
        [HarmonyPostfix]
        public static void UpdateCharasOnFillPatch()
        {
            UpdateCustomCharas(Princess.monthOfGame);
        }

        private static void UpdateCustomCharas(int month)
        {
            try
            {
                foreach (Chara chara in Chara.allCharas)
                {
                    if (chara is CustomChara customChara)
                    {
                        CharaFieldValues fieldValues = GetCharaFieldValues(customChara, month);
                        if (fieldValues != null)
                        {
                            // Update the character's fields based on the latest overrides
                            chara.facts[CharaFact.name] = fieldValues.name;
                            chara.nickname = fieldValues.nickname;
                            chara.facts[CharaFact.basics] = fieldValues.basicInfo;
                            chara.facts[CharaFact.more] = fieldValues.moreInfo;
                            chara.dialogColor = fieldValues.dialogueColor;
                            chara.facts[CharaFact.enhancement] = fieldValues.augment;
                            chara.defaultBackground = fieldValues.defaultBg;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Error in UpdateCustomCharaFields: " + e.Message);
            }
        }

        [HarmonyPatch(typeof(CharasMenu), "UpdateCurrentChara")]
        [HarmonyPostfix]
        public static void CharasMenuUpdateCurrentCharaPostfix(CharasMenu __instance)
        {
            try
            {
                var charaField = typeof(CharasMenu).GetField("chara", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (charaField == null)
                {
                    return;
                }

                Chara chara = (Chara)charaField.GetValue(__instance);
                if (chara == null)
                {
                    return;
                }

                if (chara is CustomChara customChara)
                {
                    NWText charaParagraph = __instance.charaParagraph;
                    string[] parts = charaParagraph.text.Split(["."], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        return;
                    }

                    // last part is the pronouns
                    string shownPronouns = parts[parts.Length - 1].Trim();
                    string actualPronouns = GetLatestOverrideOfType(customChara, CharaDataOverrideField.pronouns);

                    if (actualPronouns == null)
                    {
                        return;
                    }

                    if (actualPronouns.ToLower() == "mirror")
                    {
                        actualPronouns = Princess.GetGenderPronouns();
                    }

                    if (actualPronouns.EndsWith("."))
                    {
                        actualPronouns = actualPronouns.Substring(0, actualPronouns.Length - 1).Trim();
                    }

                    if (actualPronouns != null && actualPronouns != shownPronouns)
                    {
                        // use all parts except the last one and add the actualPronouns at the end
                        string newCharaParagraph = string.Join(". ", parts, 0, parts.Length - 1) + ". " + actualPronouns + ".";

                        __instance.charaParagraph.text = newCharaParagraph;
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Error in CharasMenuUpdateCurrentCharaPostfix: " + e.Message);
            }
        }

        [HarmonyPatch(typeof(Chara), nameof(Chara.SetFillbar))]
        [HarmonyPostfix]
        public static void CharaSetFillbarPostfix(Chara __instance, Fillbar statFillbar, int index)
        {
            try
            {
                if (__instance is CustomChara customChara && customChara.data != null)
                {
                    CharaDataOverrideField field = index switch
                    {
                        0 => CharaDataOverrideField.fillbar1Value,
                        1 => CharaDataOverrideField.fillbar2Value,
                        2 => CharaDataOverrideField.fillbar3Value,
                        _ => throw new ArgumentOutOfRangeException(nameof(index), "Index must be 0, 1, or 2")
                    };
                    string latestOverride = GetLatestOverrideOfType(customChara, field);
                    if (latestOverride != null)
                    {
                        if (latestOverride.ToLower().StartsWith("mem_"))
                        {
                            int num = Princess.GetMemoryInt(latestOverride).Clamp(0, 10);
                            statFillbar.ChangeValue((float)num / 10f);
                        }
                        else if (int.TryParse(latestOverride, out int value))
                        {
                            statFillbar.ChangeValue((float)value / 10f);
                        }
                        else
                        {
                            ModInstance.log($"Invalid fillbar value '{latestOverride}' for {customChara.charaID} at index {index}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Error in CharaSetFillbarPostfix: " + e.Message);
            }
        }

        [HarmonyPatch(typeof(Chara), "isDatingSomeone", MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharaIsDatingSomeonePostfix(Chara __instance, ref bool __result)
        {
            try
            {
                if (__instance is CustomChara chara && chara.data != null)
                {
                    if (chara.data.secretAdmirerType == SecretAdmirerType.never)
                    {
                        __result = true; // mark character as if dating someone, so they never be a secret admirer - just like Sym
                    }
                    else if (chara.data.secretAdmirerType == SecretAdmirerType.polyamorous)
                    {
                        __result = false; // mark character as if not dating anyone, so they can always be a secret admirer
                    }
                    // Otherwise leave the result as is (default game behaviour)
                }
            }
            catch (Exception e)
            {
                ModInstance.log("Error in CharaIsDatingSomeonePostfix: " + e.Message);
            }
        }
    }
}
