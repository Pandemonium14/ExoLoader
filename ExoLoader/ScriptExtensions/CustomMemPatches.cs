using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;

namespace ExoLoader
{
    [HarmonyPatch]
    public class CustomMemPatches
    {
        [HarmonyPatch(typeof(NWButtonResults), "IsShowableReq", MethodType.Normal)]
        [HarmonyPostfix]
        public static void IsShowableReqPatch(NWButtonResults __instance, ref bool __result, StoryReq req, Result result)
        {
            try
            {
                if (__result)
                {
                    return;
                }

                if (req.type == StoryReqType.memory && CustomMemChange.changesByID.ContainsKey(req.stringID))
                {
                    __result = true;
                    return;
                }
            }
            catch (System.Exception ex)
            {
                ModInstance.instance.Log("Error in IsShowableReqPatch: " + ex.Message);
                ModInstance.instance.Log("Stack trace: " + ex.StackTrace);
            }
        }

        [HarmonyPatch(typeof(NWButtonResults), "SetRequirement", MethodType.Normal)]
        [HarmonyPostfix]
        public static void SetRequirementPatch(NWButtonResults __instance, ref Image requirementIcon, StoryReq req, Result result)
        {
            try
            {
                if (req.type == StoryReqType.memory && CustomMemChange.changesByID.ContainsKey(req.stringID))
                {
                    ModInstance.instance.Log("Setting custom memory icon for " + req.stringID);
                    CustomMemChange change = CustomMemChange.changesByID[req.stringID];
                    bool flag = !req.Execute(result);
                    string text = "other";
                    string text2 = change.name;

                    string tooltip3 = (req.compare != StoryReqCompare.lessThan) ? TextLocalized.Localize("button_req_equalOrGreater", text2, req.intValue + 1) : TextLocalized.Localize("button_req_lessThan", text2, req.intValue);

                    NWButtonResultsSetIcon(__instance, ref requirementIcon, text, tooltip3, flag ? NWButtonResults.requirementNotMetColor : NWButtonResults.requirementMetColor);
                    if (flag || PlatformUtils.useControllerTooltips)
                    {
                        __instance.tooltipText = tooltip3;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModInstance.instance.Log("Error in SetRequirementPatch: " + ex.Message);
                ModInstance.instance.Log("Stack trace: " + ex.StackTrace);
            }
        }

        public static void NWButtonResultsSetIcon(NWButtonResults __instance, ref Image iconImage, string iconName, string tooltip = null, Color color = default(Color))
        {
            Color iconColor = color == default(Color) ? Color.white : color;

            if (iconName.IsNullOrEmptyOrWhitespace())
            {
                ((Component)(object)iconImage).SetActiveMaybe(value: false);
                return;
            }

            Sprite requirementIcon = AssetManager.GetRequirementIcon(iconName);
            if (requirementIcon == null)
            {
                ((Component)(object)iconImage).SetActiveMaybe(value: false);
                return;
            }

            ((Component)(object)iconImage).SetActiveMaybe();
            iconImage.sprite = requirementIcon;
            if (tooltip != null)
            {
                ((Component)(object)iconImage).GetComponentInChildren<Tooltippable>()?.SetText(tooltip);
            }

            iconImage.SetImageColor(iconColor);
        }

        [HarmonyPatch(typeof(Princess), nameof(Princess.IncrementMemory))]
        [HarmonyPrefix]
        public static void IncrementMemoryPatch(string id, int value = 1)
        {
            if (value != 0 && Princess.result != null && CustomMemChange.changesByID.ContainsKey(id))
            {
                ModInstance.instance.Log("Adding custom memory change for " + id);
                Princess.result.AddSkillChange(new SkillChange(id, value));
            }
        }

        // ResultsSkillBubble.SetContent
        [HarmonyPatch(typeof(ResultsSkillBubble), nameof(ResultsSkillBubble.SetContent))]
        [HarmonyPostfix]
        public static void SetContentPatch(ResultsSkillBubble __instance, SkillChange change)
        {
            if (CustomMemChange.changesByID.ContainsKey(change.stringID))
            {
                ModInstance.instance.Log("Setting custom memory bubble for " + change.stringID);
                __instance.SetActiveMaybe(value: true);
                ResultsSkillBubbleSetIcon(__instance, "other", "other");
                return;
            }
        }

        public static void ResultsSkillBubbleSetIcon(ResultsSkillBubble __instance, string iconName, string bgName, Color color = default(Color))
        {
            Color iconColor = color == default(Color) ? Color.white : color;
            Sprite requirementIcon = AssetManager.GetRequirementIcon(iconName);
            if (requirementIcon == null)
            {
                __instance.SetActiveMaybe(value: false);
                return;
            }

            __instance.bubbleIcon.sprite = requirementIcon;
            Sprite resultBubbleBg = AssetManager.GetResultBubbleBg(bgName);
            if (resultBubbleBg == null)
            {
                __instance.SetActiveMaybe(value: false);
                return;
            }

            __instance.bubbleBg.sprite = resultBubbleBg;
            __instance.bubbleIcon.SetImageColor(iconColor);
        }

        // SkillChange.GetFormattedString
        [HarmonyPatch(typeof(SkillChange), nameof(SkillChange.GetFormattedString))]
        [HarmonyPostfix]
        public static void GetFormattedStringPatch(SkillChange __instance, ref string __result, bool bonusDetails = false, bool simpleBonusDetails = false)
        {
            if (__instance.stringID != null && CustomMemChange.changesByID.ContainsKey(__instance.stringID))
            {
                ModInstance.instance.Log("Formatting custom memory change string for " + __instance.stringID);

                CustomMemChange change = CustomMemChange.changesByID[__instance.stringID];
                int num = __instance.value.Clamp(-100, 100);
                string text = num.ToSignedString() + " " + change.name;

                // This part probably is not needed
                if (__instance.bonusValue != 0 || !__instance.bonusText.IsNullOrEmptyOrWhitespace())
                {
                    if (bonusDetails)
                    {
                        string[] array = __instance.bonusText?.Split(new char[1] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (array == null || array.Length == 0)
                        {
                            text = text + " (" + __instance.bonusValue.ToSignedString() + ")";
                        }
                        else if (array.Length == 1)
                        {
                            text = text + " (" + TextLocalized.Localize("battleBonus_from", __instance.bonusValue.ToSignedString(), array[0]) + ")";
                        }
                        else if (array.Length == 2)
                        {
                            string text6 = array.JoinSafe(" " + TextLocalized.Localize("battleBonus_and") + " ");
                            text = text + " (" + TextLocalized.Localize("battleBonus_from", __instance.bonusValue.ToSignedString(), text6) + ")";
                        }
                        else
                        {
                            string text7 = array.JoinSafe(", ");
                            text = text + " (" + TextLocalized.Localize("battleBonus_from", __instance.bonusValue.ToSignedString(), text7) + ")";
                        }
                    }
                    else
                    {
                        text = text + " (" + __instance.bonusValue.ToSignedString() + ")";
                    }
                }

                __result = text.ReplaceAll("  ", " ");
                return;
            }
        }
    }
}
