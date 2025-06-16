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
                    if (!Princess.HasMemory(memory))
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

        // When month changes, update all custom characters with the latest valid data
        [HarmonyPatch(typeof(PrincessMonth), nameof(PrincessMonth.SetMonth))]
        [HarmonyPrefix]
        public static bool PrincessMonthSetMonthPrefix(int value, bool force = false, bool skipUpdateMapManager = false, bool justLoadedMap = false)
        {
            try
            {
                foreach (Chara chara in Chara.allCharas)
                {
                    if (chara is CustomChara customChara)
                    {
                        CharaFieldValues fieldValues = GetCharaFieldValues(customChara, month: value);
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
                ModInstance.log("Error in PrincessMonthSetMonthPrefix: " + e.Message);
            }

            return true;
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

                    ModInstance.log($"Custom Chara {customChara.charaID} pronouns: shown={shownPronouns}, actual={actualPronouns}");

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
    }
}
